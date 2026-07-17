# Performance Optimization Test: Company Search

This document details the optimizations attempted for `LINQNew` (the modernized EF Core 10 implementation) and `LINQSQL` (the baseline Legacy Stored Procedure), breaking down the execution time and generated SQL for each major query segment.

## Benchmark Summary

| Implementation | Cold Plan Time | Warm Plan Time | Notes |
|---|---|---|---|
| **LINQNew** | ~4.4s – 5.5s | ~2.7s – 4.4s | Highly optimized Two-Query Split pattern. |
| **LINQSQL (SP)**| ~11.0s | ~6.8s | The legacy `CompanySearch` stored procedure. Used as the baseline. |

*Note: "Cold plan" means the SQL Server plan cache was empty, but the DB connection was warmed up. "Warm plan" means the execution plan was cached in SQL Server and EF Core.*

---

## 1. Category Filter Query (The "What")

This query extracts the raw pool of company IDs that match the requested Category (including child categories and multiproducts).

**Best Element:** The `OR EXISTS` subquery structure. It compiles cleanly into a forward table scan and allows SQL Server to optimize the hierarchy lookup without parameter bloat.
**Worst Element (Failed Attempt):** Materializing category children into a `List<short> {1, 2... 84}` and using `.Contains()`. EF Core padded this list to 90 SQL parameters, which caused SQL Server to fail to compile an execution plan and time out (**>24 seconds**).

### Generated SQL (LINQNew):
**Time:** ~1,400ms (Warm) / ~3,100ms (Cold)
```sql
SELECT [c].[Id], [c].[Created]
FROM [CompanyProfiles] AS [c]
INNER JOIN [Accounts] AS [a] ON [c].[Id] = [a].[Id]
WHERE [a].[Status] = CAST(2 AS tinyint) AND [c].[TransactionType] & @queryInput_TransactionType > CAST(0 AS smallint) AND ([c].[Category] = @_8__locals1_queryInput_Category OR EXISTS (
    SELECT 1
    FROM [Categories_Unwound] AS [c0]
    WHERE [c0].[Parent] = @_8__locals1_queryInput_Category AND [c0].[Child] = [c].[Category]) OR EXISTS (
    SELECT 1
    FROM [CompanyProducts] AS [c1]
    INNER JOIN [Products] AS [p] ON [c1].[Product] = [p].[Id]
    WHERE ([p].[Type] = CAST(0 AS smallint) OR ([c1].[UnlistedType] = CAST(1 AS tinyint) AND [p].[Status] = CAST(2 AS tinyint))) AND ([c1].[Category] = @_8__locals1_queryInput_Category OR EXISTS (
        SELECT 1
        FROM [Categories_Unwound] AS [c2]
        WHERE [c2].[Parent] = @_8__locals1_queryInput_Category AND [c2].[Child] = [c1].[Category])) AND [c1].[Company] = [c].[Id]))
```

---

## 2. Location Filter Query (The "Where")

This query extracts all companies that have an office within the requested Location hierarchy. It is executed *independently* from the Category query (The "Two-Query Split"), and their results are intersected in C# memory.

**Best Element:** Using an `IQueryable` subquery for `Locations_Unwound` combined with the `IX_CompanyOffices_Location` index. It evaluates instantly.
**Worst Element (Failed Attempt):** Materializing the 566 child locations into a `List<int>` and using `.Contains()`. EF Core padded this to **600 parameters** (`@locIds1..@locIds600`), causing plan compilation to fail and time out.

### Generated SQL (LINQNew):
**Time:** ~515ms (Warm) / ~760ms (Cold)
```sql
SELECT DISTINCT [c].[Company]
FROM [CompanyOffices] AS [c]
WHERE [c].[Location] = @_8__locals2_queryInput_Location OR [c].[Location] IN (
    SELECT [l].[Child]
    FROM [Locations_Unwound] AS [l]
    WHERE [l].[Parent] = @_8__locals2_queryInput_Location
)
```

---

## 3. Office Map (Selection & Sorting)

Once the 20 companies for the current page are identified (after the C# memory intersection and sorting), we must fetch their display offices.

**Best Element:** Fetching all candidate offices for the 20 companies using a simple `IN` clause (20 parameters) and doing the lowest-Order selection in C# memory.
**Worst Element (Failed Attempt):** Using `GroupBy(c => c.Company).Select(g => g.OrderBy(x => x.Order).First())`. EF Core translated this into a correlated `SELECT TOP(1)` subquery *per group row*, re-evaluating the location subquery for every single company, resulting in a **4,105ms** query.

### Generated SQL (LINQNew):
**Time:** ~25ms (Warm) / ~60ms (Cold)
```sql
SELECT [c].[Company], [c].[Order], [c].[Id]
FROM [CompanyOffices] AS [c]
WHERE [c].[Company] IN (@pageIdsArray1, @pageIdsArray2, ..., @pageIdsArray20)
ORDER BY [c].[Order]
```

---

## 4. Facets Aggregation

Aggregates the facet counts for all companies that matched both the Category and Location filters (e.g., 820 IDs).

**Best Element:** Serializing the 820 matching IDs into a JSON string and passing it to an `INNER JOIN OPENJSON(@json)` raw SQL query.
**Worst Element (Failed Attempt 1):** Passing the 820 IDs to `.Contains()`. EF Core generated **900 parameters**, causing execution timeouts.
**Worst Element (Failed Attempt 2):** Using a derived table `FROM (SELECT pf.* FROM CompanyFacets WHERE Company IN (SELECT value FROM OPENJSON(...))) AS b`. Wrapping the clause in a derived table prevented the query optimizer from pushing the predicate into the index, resulting in a **1,973ms** execution time instead of **330ms**.

### Generated SQL (LINQNew Raw SQL):
**Time:** ~485ms (Warm) / ~330ms (Cold)
```sql
SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
FROM CompanyFacets pf
INNER JOIN OPENJSON(@p0) ids ON pf.Company = ids.value
INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
GROUP BY pfv.Name, pfv.Id
```
*(Where `@p0` is a JSON array string `"[11329,11222,11220...]"`)*

---

## Baseline: LINQSQL (Legacy Stored Procedure)

The legacy implementation relies on the `CompanySearch` stored procedure. No structural optimizations were made to the SP itself during this test; it serves as the baseline to prove that the modernized EF Core 10 LINQ implementation can match or beat its performance without relying on archaic `@TableVariables` and raw SQL concatenation.

**Time:** ~6,847ms (Warm) / ~11,057ms (Cold)

**Key Finding:** The SP executes the expensive Category/Location intersection three separate times (Count, Page Fetch, Facets) using temporary tables. The `LINQNew` implementation beats the SP's execution time (4.4s vs 6.8s) because it executes the complex intersection exactly once, materializes the ID array into memory, and re-uses that array (via `OPENJSON`) for the subsequent Facet and Pagination queries.
