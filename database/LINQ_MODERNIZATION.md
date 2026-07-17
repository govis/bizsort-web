# BizSort LINQ & Database Modernization Guide

This document consolidates guidelines, patterns, and known pitfalls for writing and porting EF Core 10 LINQ queries and database access logic in the modern BizSort architecture.

👉 **For caching and legacy architecture rules, please see [LEGACY_MIGRATION.md](../.agents/LEGACY_MIGRATION.md).**

---

## 1. Architecture: Building Blocks & Extension Methods

When porting complex LINQ queries (especially those using `FacetSet` filtering, location TVFs, or hierarchical category unwinding):

- **Use Reusable Extension Methods**: Do not inline complex `Where` or `Join` clauses repeatedly in service methods.
- **Implement `.GetFiltered()`**: Following the legacy codebase architecture, implement `public static IQueryable<T> GetFiltered(this IQueryable<T> query, AppDbContext dbContext, QueryInput queryInput)` extension methods (e.g., in `BizSrt.Data.Extensions.QueryExtensions`).
- **Composable Queries**: Use these extension methods as composable "building blocks" that take the base `IQueryable<T>` (e.g., `dbContext.CompanyProfiles`) and append the required filters before returning the modified query.

---

## 2. Stored Procedure Fallbacks

When porting `Search` methods, ensure you maintain parity with the legacy dual-path architecture:
- If a user provides a `SearchQuery` (text search) or `SearchNear` (geographic coordinate search) parameter, **bypass the complex LINQ queries entirely** and execute the underlying database stored procedures (e.g., `CompanySearch`, `ProductSearch`) using `dbContext.Database.GetDbConnection().CreateCommand()`.
- The complex EF Core LINQ structures should only be used when browsing by categorical filters without free-text or radius search.

---

## 3. Query Optimization: The Two-Query Split (Category + Location)

**The Pitfall**: Combining two independent, expensive filters (e.g., Category via `OR EXISTS` and Location via hierarchy expansion) into a single SQL statement forces SQL Server to optimize one massive plan. The interaction between filters frequently causes the plan to degrade catastrophically, resulting in execution timeouts.

**The Solution**: Run the two filters as two independent queries sequentially, then intersect the resulting ID sets in C# memory using a `HashSet<int>`.
- The Category query returns matching Company IDs.
- The Location query returns companies with offices in the location tree.
- Both execute efficiently because they have independent, clean SQL Server execution plans.

> [!WARNING]
> Do NOT use `Task.WhenAll` to run them in parallel on the same `AppDbContext`. EF Core contexts are not thread-safe.

---

## 4. EF Core Parameter Padding (CRITICAL)

**The Pitfall**: EF Core 10 caches query plans by converting `List<T>.Contains()` into a fixed number of SQL parameters (padding). For example, passing a list of 566 location IDs generates **600 named SQL parameters** (`@p1..@p600`). This completely breaks SQL Server's query compiler, leading to 10-30 second timeouts as it tries to compile a plan for the padded shape.

**The Rules**:
1. **Never use `.Contains()` with an in-memory `List<T>` larger than 10 items** in a database query.
2. For hierarchy expansion (e.g., `Categories_Unwound`, `Locations_Unwound`), use an unmaterialized `IQueryable` subquery. EF Core translates this to a clean `IN (SELECT Child FROM ... WHERE Parent = @p)` with a single parameter.

---

## 5. OPENJSON for Large ID Sets (Avoiding the Triple-Query Penalty)

**The Pitfall**: Search operations typically require 3 queries: total count, a page of results, and facet aggregations. Running the complex base query three times is too slow. But passing the thousands of matched IDs into the facet query using `.Contains()` triggers the catastrophic parameter padding bug mentioned above (e.g., 900 SQL parameters).

**The Solution**: Execute the base query once, projecting only the `Id` to satisfy covering indexes, and materialize the array into memory. Then serialize the IDs to JSON and use `OPENJSON` for subsequent queries (like Facets):

```csharp
var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
var pfq = await dbContext.Database
    .SqlQueryRaw<ValueCount>(@"
        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
        FROM CompanyFacets pf
        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
        GROUP BY pfv.Name, pfv.Id", idsJson)
    .ToArrayAsync();
```
*Note: Always use `INNER JOIN OPENJSON`. Using `WHERE IN (SELECT value FROM OPENJSON)` wraps the clause in a derived table and prevents index seeks.*

---

## 6. Eliminating Backward Scans (Sorting In-Memory)

**The Pitfall**: Appending `.OrderByDescending(c => c.Created)` (and `.Take(N)`) to a complex `OR EXISTS` query causes SQL Server to execute a **backward index scan**. It scans the `Created` index backwards and evaluates the expensive subqueries row-by-row until it finds enough matching records. This causes query times to jump from 4 seconds to >30 seconds in production.

**The Solution**: Defer the sort to C# memory.
1. Allow EF Core to generate the complex `OR EXISTS` filters (which it processes extremely fast using a forward table scan).
2. Project both the ID and the Sort Key into memory without `OrderBy` or `Take`.
3. Perform the sort on the materialized array.

```csharp
var allMatches = await query.Select(c => new { c.Id, c.Created }).ToArrayAsync();
var sortedPageIds = allMatches.OrderByDescending(c => c.Created).Select(c => c.Id).Skip(0).Take(20).ToArray();
```

---

## 7. Generating `CROSS APPLY` / `OUTER APPLY` (Legacy `let` Patterns)

**The Pitfall**: Legacy EF6 queries used the `let` keyword with correlated `FirstOrDefault()` calls (e.g. `let media = dbContext.CompanyMedia.FirstOrDefault(m => m.Company == c.Id)`). EF Core 10 cannot translate this efficiently and regresses it into multiple redundant scalar subqueries (`SELECT TOP(1)`), causing massive N+1 overhead at the SQL level.

**The Solution**: Rewrite the legacy `let` to use a second `from` clause with `.Take(1)`. This guarantees EF Core 10 generates a single `APPLY`.

- **OUTER APPLY** (keeps row if empty, like a LEFT JOIN): Add `.DefaultIfEmpty()`
  ```csharp
  from c in dbContext.CompanyProfiles
  from media in dbContext.CompanyMedia.Where(m => m.Company == c.Id).Take(1).DefaultIfEmpty()
  ```
- **CROSS APPLY** (drops row if empty, like an INNER JOIN): Omit `.DefaultIfEmpty()`
  ```csharp
  from c in dbContext.CompanyProfiles
  from media in dbContext.CompanyMedia.Where(m => m.Company == c.Id).Take(1)
  ```

---

## 8. Avoiding N+1 on LOB (Large Object) Columns

**The Pitfall**: Tables like `CompanyMedia` contain `varbinary(max)` columns. If you only need to check for existence or grab the media ID, querying the full entity (e.g., `FirstOrDefaultAsync()`) forces EF Core to download the massive multi-megabyte payloads over the network.

**The Solution**: Always project `.Select(m => m.Id)` if you don't need the file content. 

Additionally, avoid joining large media tables directly in base search queries. Fetch the final 20 company IDs first, and *then* run a separate query to fetch their media.

---

## 9. Avoid `Distinct()` on Full Entities

**The Pitfall**: Calling `.Distinct()` on a full entity after a join forces EF Core to select *all* columns into a sub-select to compute uniqueness, which destroys performance.

**The Solution**: Use `.Any()` from the perspective of the child table, which translates into a highly efficient `EXISTS` statement and inherently deduplicates the parent row.
```csharp
// BAD
query = (from c in query join co in dbContext.CompanyOffices on c.Id equals co.Company select c).Distinct();

// GOOD
query = query.Where(c => dbContext.CompanyOffices.Any(co => co.Company == c.Id));
```

---

## 10. Cache Construction: Batch-Load Navigation Properties

**The Pitfall**: When building cache models (like `CachedCompanyProfile`), lazy-loading related collections (`Offices`, `Products`) individually per company creates an N+1 cascade (e.g., 20 companies * 3 queries = 60 DB hits).

**The Solution**: Batch-load all related collections in the cache's multi-fetch constructor. Group them in memory, and assign them directly to the cached objects.

```csharp
var allOffices = dbContext.CompanyOffices
    .Where(co => accountIds.Contains(co.Company))
    .Select(co => new { co.Company, co.Id, co.City }) // Project to anonymous type first
    .AsNoTracking().ToList() // Now safe to materialize
    .GroupBy(co => co.Company)
    .ToDictionary(g => g.Key, g => g.ToArray());
```
> [!CAUTION]
> Do NOT call `AsNoTracking()` directly on scalar/primitive projections (like `Select(cp => cp.Product)`). EF Core requires an entity or anonymous object type for `AsNoTracking`. Project first, then track.
