# LINQ & SQL Performance Optimization Findings

This document captures the chronological attempts, experiments, and failures we encountered while optimizing the `CompanySearch` LINQ queries. It serves as a historical record of what was tried, what failed, and why.

---

## 1. Category Filter Query

**Goal:** Extract company IDs that match the requested Category (including child categories and multiproducts).

### What We Tried:
1. **The Legacy `OR EXISTS` Subquery (Winner)**
   - **SQL Generated:** `OR EXISTS (SELECT 1 FROM Categories_Unwound...)`
   - **Result:** ~2.3s – 3.8s depending on cache. This structure compiles cleanly into a forward table scan and allows SQL Server to optimize the hierarchy lookup without parameter bloat.
2. **Materializing Child Categories to `List<short>`**
   - **Approach:** Fetched the 83 child categories into memory, appended the parent (84 total), and used `catIds.Contains(c.Category)`.
   - **Result:** **FAIL (24,250ms Timeout)**. EF Core padded the 84 items into exactly 90 named SQL parameters (`@__catIds_0` to `@__catIds_89`). The duplicate padded variables caused SQL Server's execution plan compiler to choke and time out.
3. **`UNION` with Materialized Category IDs**
   - **Approach:** Queried `CompanyProfiles` and `CompanyProducts` separately using `Category IN (...)` and attempted to `UNION` the resulting Company IDs to avoid the `OR EXISTS` block entirely.
   - **Result:** **FAIL (15.0+ seconds)**. Passing 90+ parameterized variables into an `IN` clause across a `UNION` caused the SQL Server optimizer to fail at creating an efficient plan, resulting in massive execution latency.
4. **`UNION` with `IQueryable` Subqueries**
   - **Approach:** Similar to above, but using EF Core's `IQueryable` to generate `IN (SELECT Child FROM Categories_Unwound...)` across a `UNION`.
   - **Result:** **FAIL (15.2+ seconds)**. SQL Server completely failed to optimize the combination of a `UNION` alongside deep correlated subqueries.

---

## 2. Location Filter Query

**Goal:** Extract companies that have an office within the requested Location hierarchy.

### What We Tried:
1. **The Legacy TVF `CompanyOfficeLocation`**
   - **Approach:** Mimicked the legacy stored procedure using the Table-Valued Function.
   - **Result:** **FAIL (Massive IO)**. Resulted in 24,776 logical reads on `Locations_Unwound`. The TVF generated a correlated `EXISTS` subquery that executed once per row in `CompanyOffices`.
2. **Materializing Child Locations to `List<int>` (Named Parameter Padding)**
   - **Approach:** Fetched the 566 child locations into memory and used `locIds.Contains(co.Location)` via EF Core parameterization.
   - **Result:** **FAIL (Timeout)**. EF Core padded the 566 items into exactly **600 named parameters** (`@locIds1..@locIds600`). This completely blew up the plan compiler.
3. **Literal List (Hardcoded `IN` Clause)**
   - **Approach:** Forced EF Core to generate a literal 566-value `IN (1, 2, 3, ..., 566)` string to bypass the parameter padding bloat.
   - **Result:** **FAIL (Timeout)**. While it avoided parameter bloat, embedding a massive literal `IN` clause alongside the complex `OR EXISTS (Categories)` subqueries in a single monolithic query created an optimization nightmare for SQL Server. It was actually worse than the TVF approach.
4. **`IQueryable` Subquery (Winner)**
   - **Approach:** `WHERE Location = @loc OR Location IN (SELECT Child FROM Locations_Unwound WHERE Parent = @loc)`
   - **Result:** Fast (~600ms). Used exactly 1 parameter and executed entirely on the SQL Server side.
5. **Added Index: `IX_CompanyOffices_Location`**
   - **Result:** Dropped the location filter from a full table scan (211 logical reads) to targeted seeks (32 reads).

---

## 3. Office Map (Selection & Sorting)

**Goal:** After paginating down to 20 candidate companies, fetch the "best" (lowest-Order) office for each to display in the UI.

### What We Tried:
1. **EF Core `GroupBy` + `First`**
   - **Approach:** `GroupBy(c => c.Company).Select(g => g.OrderBy(x => x.Order).First())`
   - **Result:** **FAIL (4,105ms)**. EF Core generated a correlated `SELECT TOP(1)` subquery per company group, and re-evaluated the expensive Location filter inside every single subquery.
2. **Raw SQL with `ROW_NUMBER()` (Location-First)**
   - **Approach:** `ROW_NUMBER() OVER (PARTITION BY Company ORDER BY Order)`
   - **Result:** **FAIL (1,052ms - 1,634ms)**. SQL Server scanned 5,149 location-matched offices *first* before filtering down to the 20 requested companies.
3. **Raw SQL with `ROW_NUMBER()` (Company-First)**
   - **Approach:** Put `Company IN (@pageIds)` as the leading predicate.
   - **Result:** ~34ms. Extremely fast, but required complex and fragile C# string concatenation to build the query.
4. **EF Core `Contains` + In-Memory Sort (Winner)**
   - **Approach:** Fetch all offices for the 20 `pageIds` using `Company IN (@p1..@p20)`, then pick the lowest-Order office in C# memory.
   - **Result:** Fast (~60ms). No grouping or sorting done in SQL. 20 parameters is perfectly safe and doesn't trigger parameter-padding bloat.

---

## 4. Facets Aggregation

**Goal:** Aggregate facet counts for all companies that matched both Category and Location.

### What We Tried:
1. **Re-running the Base Query**
   - **Approach:** Run the entire `OR EXISTS` base query again just to count facets.
   - **Result:** **FAIL**. The "Triple-Query Penalty" caused total execution time to double.
2. **`Contains` on Materialized IDs**
   - **Approach:** Passed the 820 matching IDs into `allMatchingIds.Contains(c.Id)`.
   - **Result:** **FAIL (~16s / Timeout)**. EF Core padded the 820 items to exactly **900 named parameters**, causing severe plan instability and timeouts.
3. **Raw SQL (`FromSqlRaw`): Derived Table `OPENJSON`**
   - **Approach:** `FROM (SELECT pf.* FROM CompanyFacets WHERE Company IN (SELECT value FROM OPENJSON(@json))) AS b INNER JOIN ...`
   - **Result:** **FAIL (1,973ms)**. Wrapping the table in a derived subquery prevented the SQL query optimizer from pushing the `IN` predicate down into an index seek on `CompanyFacets.Company`.
4. **Raw SQL (`FromSqlRaw`): `INNER JOIN OPENJSON` (Winner)**
   - **Approach:** `INNER JOIN OPENJSON(@json) ids ON pf.Company = ids.value`
   - **Result:** Fast (~330ms). 1 JSON string parameter avoids padding issues, and the direct `INNER JOIN` allows a highly optimal index seek.

---

## 5. Architectural Experiments

### In-Memory Sorting (The Backward Scan Mitigation)
- **Approach:** When EF Core appended `.OrderByDescending(c => c.Created)` to the complex `OR EXISTS` category query, SQL Server utilized the `IX_CompanyProfiles_Created` index to scan the table *backwards*. Evaluating complex `OR EXISTS` subqueries row-by-row during a backward index scan forced SQL Server into a catastrophic execution plan (60+ seconds). We removed the SQL-side `.OrderByDescending()` entirely, projecting only the `Id` and `Created` timestamp into memory via a forward scan, and then applied the sort in C#.
- **Result:** **SUCCESS**. This dropped the category query time from a 60+ second timeout down to ~2.5s (warm cache) by guaranteeing SQL Server performed a forward table scan. Sorting 10,000 items in C# memory takes <5ms.

### Query Splitting (The "Two-Query Split")
- **Approach:** The original query combined Category (`OR EXISTS`) and Location (`IN`) logic into one massive SQL statement, which caused the optimizer to break down. We split this into a **Category-only query** and a **Location-only query**, and intersected their resulting IDs (`HashSet<int>`) in C# memory.
- **Result:** **SUCCESS**. This dropped execution time by ~40% because each independent query received a clean, simple execution plan from SQL Server without interference.

### Parallel Query Execution
- **Approach:** We attempted to run the Category filter and Location filter in parallel using `Task.WhenAll` to save time.
- **Result:** **FAIL (`InvalidOperationException`)**. EF Core `DbContext` is explicitly not thread-safe. "A second operation was started on this context instance before a previous operation completed." We reverted to running the Two-Query split sequentially using standard `await`.
