---
name: "EF Core 10 LINQ to SQL Optimization"
description: "Guidelines and known pitfalls for writing optimal LINQ queries in EF Core 10 against SQL Server. Use this skill whenever porting, writing, or optimizing complex search queries to avoid catastrophic performance issues like backward index scans and execution timeouts."
---

# EF Core 10 LINQ to SQL Optimization Guidelines

When writing or optimizing LINQ queries for the BizSort `.NET 10` application, it is critical to understand how EF Core translates C# into SQL Server syntax. Small structural choices can mean the difference between a 50ms query and a 60-second Execution Timeout.

Always follow these guidelines, especially when querying large tables (e.g., `CompanyProfiles`, `CompanyProducts`, `CompanyMedia`).

## 1. Avoid SQL-Side Sorting on Complex Queries (The Backward-Scan Bug)
**The Pitfall:** When appending `.OrderByDescending(c => c.Created)` to a complex query involving `OR EXISTS` or multiple `INNER JOINs`, SQL Server often assumes the fastest plan is to scan the index (e.g., `IX_CompanyProfiles_Created`) *backwards*. Evaluating complex nested-loop subqueries row-by-row during a backward scan adds significant overhead to the execution plan. While modern SQL Server optimizers can handle this without timing out, it is typically **~2x slower** than deferring the sort.
**The Solution:** Defer the sorting to application memory. Project the necessary sorting keys alongside the entity ID into an anonymous object, fetch them into a C# array using `ToArrayAsync()`, and then use LINQ to Objects to sort and paginate.
```csharp
// BAD (Causes Backward Scan Timeout)
var ids = await query.OrderByDescending(c => c.Created).Select(c => c.Id).ToArrayAsync();

// GOOD (Forces Fast Forward Scan)
var allMatches = await query.Select(c => new { c.Id, c.Created }).ToArrayAsync();
var ids = allMatches.OrderByDescending(c => c.Created).Select(c => c.Id).ToArray();
```

## 2. Beware the `UNION` Trap with Complex `IN` Clauses
**The Pitfall:** You may be tempted to use `.Union()` to avoid `OR EXISTS` subqueries. However, SQL Server's query compiler completely breaks down when trying to generate an execution plan that involves a `UNION` combined with a massive parameterized `IN (@p1... @p90)` clause or a complex `IQueryable` subquery. This will result in 15+ second compilation and execution times.
**The Solution:** Use `OR EXISTS` (via `.Any()`) instead of `UNION` for complex queries. The `OR EXISTS` logic executes flawlessly and natively as a forward table scan.

## 3. Eagerly Materialize Small Dimension Tables — With Caution on Size
**The Pitfall:** When checking hierarchy membership (e.g., `Categories_Unwound`, `Locations_Unwound`), EF Core's `.Any()` generates a correlated `EXISTS` subquery per row. For isolated single-filter queries, this can be slower than fetching the child IDs and using `.Contains()`.
**The Solution (ONLY for very small sets, <10 items):** Execute the dimension query first, materialize it into a `List<int>`, and pass it to `.Contains()`. This forces EF Core to generate a simple parameterized `IN (@p1, @p2)` clause.

> [!WARNING] **EF Core Parameter Padding (CRITICAL):** EF Core 10 pads `List<T>.Contains()` to fixed bucket sizes:
> - 84 items → **90 parameters** (`@catIds1..@catIds90`)
> - 566 items → **600 parameters** (`@locIds1..@locIds600`)
> - 820 items → **900 parameters** (`@allIds1..@allIds900`)
>
> Each padded query causes SQL Server to compile a distinct plan shape, leading to timeouts. **Never materialize dimension tables with >10 items for use with `.Contains()`**. Use an `IQueryable` subquery or `OPENJSON` instead.

```csharp
// ONLY safe for very small lists (< 10 items)
var locIds = await dbContext.Locations_Unwound.Where(lu => lu.Parent == targetLoc).Select(lu => lu.Child).ToListAsync();
locIds.Add(targetLoc);
query = query.Where(c => locIds.Contains(c.Location)); // OK only if list is tiny

// For large hierarchies (>10 children), use IQueryable subquery instead:
var childLocations = dbContext.Locations_Unwound
    .Where(lu => lu.Parent == queryInput.Location)
    .Select(lu => lu.Child);
var locationCompanyIds = dbContext.CompanyOffices
    .Where(co => co.Location == queryInput.Location || childLocations.Contains(co.Location))
    .Select(co => co.Company)
    .Distinct();
// EF Core generates: WHERE Location = @loc OR Location IN (SELECT Child FROM Locations_Unwound WHERE Parent = @loc)
```

## 4. `Distinct()` vs `Any()` (EXISTS)
**The Pitfall:** Calling `.Distinct()` on a full entity after a join forces EF Core to select *all* columns of the entity into a sub-select to compute uniqueness, which destroys performance.
**The Solution:** Use `.Any()` from the perspective of the child table, which translates into a highly efficient `EXISTS` statement and inherently deduplicates the parent row.
```csharp
// BAD
query = (from c in query join co in dbContext.CompanyOffices on c.Id equals co.Company select c).Distinct();

// GOOD
query = query.Where(c => dbContext.CompanyOffices.Any(co => co.Company == c.Id));
```

## 5. Mitigate N+1 on LOB (Large Object) Tables
**The Pitfall:** Do not query massive tables with `varbinary(max)` columns (like `CompanyMedia`) as part of a base search query. Also, never execute an unprojected `FirstOrDefaultAsync()` on these tables just to check for existence, as it will download the massive file payload over the network.
**The Solution:** Always project `.Select(m => m.Id)` when you only need to verify existence or retrieve metadata. If checking for media existence on a list of companies, fetch the Company IDs first, apply `.Take()`, and then evaluate media existence locally on the smaller set.

## 6. Prevent the "Triple-Query Penalty"
**The Pitfall:** When building complex dynamic search pages, you typically need the Total Count, the Result Page, and the Facets. Executing the base EF Core query three times re-evaluates the massive SQL execution plan.
**The Solution:** Project only the primary key (`Select(c => c.Id)`), materialize ALL matching IDs into memory (`ToArrayAsync()`), and pass that array into the subsequent Count, Fetch, and Facet queries using `.Contains()`. EF Core translates this into a highly optimized `INNER JOIN OPENJSON(...)` in SQL Server.

## 7. Combine Orthogonal Filters (But Monitor for Plan Instability)
**The Context:** When a query has two independent, expensive filters (e.g., Category via `OR EXISTS` + Location via a subquery), passing them both to a single `WHERE` clause requires SQL Server to optimize one massive plan. Historically, this caused plan instability and required a "Two-Query Split" (running Category and Location separately and intersecting in C#).
**The Standard:** Modern SQL Server handles the combined query cleanly. The **Combined SQL** approach is significantly faster because it avoids the roundtrip overhead and C# intersection costs. Use this as your default.
```csharp
// GOOD: Combined SQL (Let SQL Server optimize the complete filter)
var query = dbContext.CompanyProfiles
    .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
    .Where(x => x.a.Status == 2)
    .Select(x => x.c);

// Category Filter
query = query.Where(c => c.Category == targetCat || dbContext.Categories_Unwound.Any(cu => cu.Parent == targetCat && cu.Child == c.Category));

// Location Filter
var childLocations = dbContext.Locations_Unwound.Where(lu => lu.Parent == targetLoc).Select(lu => lu.Child);
query = query.Where(c => dbContext.CompanyOffices.Any(co => co.Company == c.Id && (co.Location == targetLoc || childLocations.Contains(co.Location))));

var allMatches = await query.Select(c => new { c.Id, c.Created }).ToArrayAsync();
```
**The Fallback:** If you observe catastrophic plan degradation (e.g., 4+ second execution times) as data grows, the fallback is the **Two-Query Split**. Run the two filters as independent `await` queries sequentially (NOT `Task.WhenAll`!), then intersect the resulting ID arrays using a `HashSet<int>`.

**Prerequisite:** Ensure the join table (e.g., `CompanyOffices`) has an index on the filter column (`Location`) with the relevant FK (`Company`) as an INCLUDE column.
```sql
CREATE INDEX IX_CompanyOffices_Location ON CompanyOffices ([Location]) INCLUDE (Company, [Order], Id);
```

## 8. EF Core Parameter Padding — Never Use `List<T>.Contains()` for Large Sets
**The Problem:** EF Core 10 pads `List<T>.Contains()` to fixed bucket sizes to allow plan caching across similar query shapes. This means:
- 84 items → **90 parameters**
- 566 items → **600 parameters**  
- 820 items → **900 parameters**

Each padded query looks like a *different* query shape to SQL Server (different number of params), causing repeated plan compilations that can take 4–24 seconds each. **This is a silent, catastrophic performance killer.**

**The Rules:**
1. **Never call `.Contains()` on a `List<T>` with more than 10 items** in a DB query.
2. For hierarchy membership (`Categories_Unwound`, `Locations_Unwound`), use an `IQueryable` subquery — it generates `IN (SELECT Child FROM ... WHERE Parent = @p)`, a single compact parameter.
3. For large materialized ID sets (820 match IDs → facets), use `OPENJSON` raw SQL (see Rule 9).

## 9. Use `OPENJSON` for Large ID Set Filtering (Facets, Large Results)
**The Pitfall:** Passing a large result set (e.g., 820 matching company IDs) to another query using `.Contains()` generates 900 padded named parameters and causes plan compilation timeouts.
**The Solution:** Serialize the ID array as a JSON string and use `SqlQueryRaw` with `OPENJSON` as a table-valued function:
```csharp
var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds); // "[820,819,...]"
var pfq = await dbContext.Database
    .SqlQueryRaw<ValueCount>(@"
        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
        FROM CompanyFacets pf
        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
        GROUP BY pfv.Name, pfv.Id", idsJson)
    .ToArrayAsync();
```
**IMPORTANT:** Use `INNER JOIN OPENJSON(...)` (not `WHERE ... IN (SELECT value FROM OPENJSON(...))`). The `FROM (subquery) WHERE IN` pattern wraps the table in a derived table, preventing the optimizer from using the `Company` column index. The direct `INNER JOIN OPENJSON` approach allows a hash join against the index.

**Result:** Facets query dropped from **1,973ms** (900 params) to **330ms** (1 JSON param) = **83% faster**.

## 10. Generating `CROSS APPLY` / `OUTER APPLY` — Porting Legacy EF6 `let` Patterns

**The Problem:** Legacy EF6 queries heavily used the `let` keyword with correlated `FirstOrDefault()` calls:
```csharp
// Legacy EF6 — translated cleanly to a single OUTER APPLY
let media = dbContext.CompanyMedia.FirstOrDefault(m => m.Company == c.Id && m.Type == 1)
where media != null && media.Metadata.Length > 0
```
EF Core 10 **cannot** translate this pattern correctly. It regresses it into **multiple redundant scalar subqueries** — one `SELECT TOP(1)` for the null-check and another for the property access — causing catastrophic nested-loop overhead.

**The Solution:** Rewrite using a second `from` clause with `.Take(1)`. EF Core 10 reliably translates this to a single `APPLY`:

**`OUTER APPLY`** (keeps the parent row even when the subquery is empty — equivalent to `LEFT JOIN`):
```csharp
var query =
    from c in dbContext.CompanyProfiles
    from media in dbContext.CompanyMedia
        .Where(m => m.Company == c.Id && m.Type == 1)
        .Take(1)
        .DefaultIfEmpty()   // ← .DefaultIfEmpty() = OUTER APPLY
    where media != null && media.Metadata.Length > 0
    select c;
```

**`CROSS APPLY`** (drops the parent row when the subquery is empty — equivalent to `INNER JOIN`):
```csharp
var query =
    from c in dbContext.CompanyProfiles
    from media in dbContext.CompanyMedia
        .Where(m => m.Company == c.Id && m.Type == 1)
        .Take(1)            // ← No .DefaultIfEmpty() = CROSS APPLY
    where media.Metadata.Length > 0
    select c;
```

**Rule of thumb:** Any legacy code using `let x = dbContext.Table.FirstOrDefault(...)` should be rewritten to the `from x in table.Where(...).Take(1)` pattern. This guarantees a single, clean `APPLY` in SQL Server instead of the triple-subquery regression.

> [!WARNING]
> **Never use `GroupBy().Select(g => g.OrderBy().First())`** — EF Core translates this to a correlated `SELECT TOP(1)` scalar subquery **per group row** (N+1 at the SQL level), not a `ROW_NUMBER()` window function as you might expect. For small page-size result sets (≤20 rows), fetch all candidate rows and do the min-selection in C# memory instead.
