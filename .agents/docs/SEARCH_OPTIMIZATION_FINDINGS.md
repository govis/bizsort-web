# Company Search Query Optimization Findings

## Summary
The modern LINQ implementation (`CompanyService.SearchAsync`) suffered from extreme timeouts and latency in production when searching large categories. After exhaustive testing against various query strategies, we identified an "EF Core + SQL Server Backward Scan" bug as the primary bottleneck, exacerbated by complex `OR EXISTS` category lookups.

By deferring the `ORDER BY` clause to in-memory processing, we successfully eliminated the backward index scans, reducing the query execution time from 60+ seconds (in production scenarios) and 14.7 seconds (legacy baseline) down to ~7.7 seconds, all while maintaining perfect parity with the modernized dataset.

## The Bottleneck: `OR EXISTS` and Backward Scans
To correctly evaluate if a company matches a category (either natively or through its multiproducts), the LINQ query generates an `OR EXISTS` structure:
```sql
(
    c.Category = @Category OR 
    EXISTS (SELECT 1 FROM Categories_Unwound WHERE Parent = @Category AND Child = c.Category) OR
    EXISTS (SELECT 1 FROM CompanyProducts WHERE Company = c.Id AND ...)
)
```
When EF Core appends `.OrderByDescending(c => c.Created)` to this query, SQL Server utilizes the `IX_CompanyProfiles_Created` index to scan the table *backwards*. Evaluating complex `OR EXISTS` queries during a backward index scan forces SQL Server to perform row-by-row probe evaluation, which scales exponentially with the table size and results in massive query latency.

## Failed Optimization Strategies

We attempted several strategies to completely eliminate the `OR EXISTS` block on the SQL side. All of them failed to out-perform the baseline due to SQL Server query compiler limitations:

### 1. `UNION` with Materialized Category IDs (`List<int>.Contains()`)
**Approach:** Materialize all child category IDs into memory (e.g., `List<int> { 1, 2, ... 90 }`), query `CompanyProfiles` and `CompanyProducts` separately using `Category IN (...)`, and `UNION` the resulting Company IDs.
**Result:** **15.0+ seconds**. 
**Reason:** Passing 90+ parameterized variables into an `IN` clause across a `UNION` causes the SQL Server optimizer to fail at creating an efficient plan, resulting in massive compilation and execution latency, even after injecting `IX_CompanyProducts_Category` covering indexes.

### 2. `UNION` with `IQueryable` Subqueries
**Approach:** Similar to above, but using EF Core's `IQueryable` to generate `IN (SELECT Child FROM Categories_Unwound WHERE Parent = @cat)`.
**Result:** **15.2+ seconds**.
**Reason:** SQL Server again failed to optimize the combination of `UNION` alongside deep correlated subqueries.

### 3. The Legacy Implementation (`CompanySearchOld`)
**Approach:** The original legacy codebase executed a direct `COUNT()`, then `.Skip(0).Take(20)`. 
**Result:** **14.7+ seconds**.
**Reason:** The legacy implementation was inherently unoptimized. It executed the expensive `OR EXISTS` logic three distinct times (once for Count, once for the Result Page, once for Facets).

## The Optimal Solution: In-Memory Sorting

Since SQL Server struggles to evaluate the `OR EXISTS` logic during backward scans or `UNION` joins, the most optimal solution is to force a forward scan and materialize the results.

1. **Keep the `IQueryable.Contains` Logic:** Allow EF Core to generate the `OR EXISTS` query, which is highly efficient for forward scans.
2. **Remove SQL-side `OrderBy`:** Replace `.OrderByDescending()` with an anonymous projection of IDs and Timestamps:
   ```csharp
   var allMatches = await query.Select(c => new { c.Id, c.Created }).ToArrayAsync();
   ```
3. **Sort In-Memory:** Use LINQ to Objects to sort the array:
   ```csharp
   allMatchingIds = allMatches.OrderByDescending(c => c.Created).Select(c => c.Id).ToArray();
   ```

**Performance Impact:** 
- **Main Query (ID Extraction):** ~6.5 seconds (down from infinite timeouts / 13s)
- **Pagination & Hydration:** ~1.0 second
- **Facets Generation:** ~0.2 seconds (utilizing the materialized `allMatchingIds`)
- **Total Execution Time:** **~7.7 seconds** (A 50% reduction from the 14.7s Legacy baseline).

This fix has been successfully applied to `backend/Data/Company/Profile.cs`.
