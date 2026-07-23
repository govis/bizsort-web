# Performance Optimization Test: Company Search

**Last Updated:** 2026-07-23 (Post SQL Server Rebuild)
**Test Parameters:** Category=163, Location=1, TransactionType=3, Page=20
**SQL Server:** Rebuilt fresh installation

---

## Executive Summary

The rebuilt SQL Server dramatically improved all query times across the board. The previous bottleneck (cold plan compilation taking 3–11s) has been eliminated. All LINQ variants now execute in **under 750ms cold** and **under 200ms warm**. Several optimizations that were previously catastrophically slow are now viable.

**Key Finding:** The **Combined SQL (Variant A)** approach — which was previously rejected because it caused unstable, high-overhead plans — now **outperforms** the Two-Query Split pattern. The **SQL-side ORDER BY backward scan bug has been fixed** in the rebuilt server but is still ~2x slower than C# memory sorting.

---

## Full Benchmark Results (9 implementations, 3 runs each)

### Overall Timings

| # | Implementation | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Status |
|:---:|---|:---:|:---:|:---:|---|
| 1 | **LINQNew (Current — Two-Query Split)** | 748 | 109 | 117 | ✅ Validated |
| 2 | **Variant A: Combined SQL (No Split)** | 137 | 63 | 70 | ⭐ New Best (cold) |
| 3 | **Variant B: OPENJSON Location** | 180 | 88 | 91 | ✅ Validated |
| 4 | **Variant C: EXISTS Location** | 152 | 110 | 149 | ✅ Good but variable |
| 5 | **Variant D: SQL-side ORDER BY** | 228 | 174 | 129 | ⚠️ Fixed but slower |
| 6 | **Variant E: GroupBy+First Office** | 310 | 80 | 97 | ⚠️ Cold penalty |
| 7 | **Variant F: ROW_NUMBER() Office** | 151 | 87 | 88 | ✅ Viable alternative |
| 8 | **LINQOld (TVF + Triple Query)** | 334 | 164 | 180 | ❌ Still slow |
| 9 | **LINQSQL (SP Baseline)** | 131 | 79 | 107 | Baseline (no facets) |

> [!NOTE]
> The SP baseline does **not** compute facets. All LINQ variants compute facets (adding ~8-20ms warm). When adjusted for this, Variant A matches the SP.

---

## Per-Query Breakdown

### 1. LINQNew — Current Best (Two-Query Split)

| Query Phase | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Rows |
|---|:---:|:---:|:---:|:---:|
| Category query (OR EXISTS) | 513 | 70 | 79 | 1,046 |
| Location query (IQueryable DISTINCT) | 50 | 17 | 26 | 5,149 |
| Office map (IN + C# GroupBy) | 46 | 3 | 2 | 20 |
| Facets (OPENJSON) | 102 | 16 | 8 | 106 |
| **Total** | **748** | **109** | **117** | — |

**Architecture:** Two independent SQL queries (Category + Location) → `HashSet<int>` intersection → C# ORDER BY → page slice → office map → OPENJSON facets.

**Generated SQL (Category):**
```sql
SELECT [c].[Id], [c].[Created]
FROM [CompanyProfiles] AS [c]
INNER JOIN [Accounts] AS [a] ON [c].[Id] = [a].[Id]
WHERE [a].[Status] = CAST(2 AS tinyint)
  AND [c].[TransactionType] & @tt > CAST(0 AS smallint)
  AND ([c].[Category] = @cat OR EXISTS (
    SELECT 1 FROM [Categories_Unwound] AS [c0]
    WHERE [c0].[Parent] = @cat AND [c0].[Child] = [c].[Category])
  OR EXISTS (
    SELECT 1 FROM [CompanyProducts] AS [c1]
    INNER JOIN [Products] AS [p] ON [c1].[Product] = [p].[Id]
    WHERE (...) AND [c1].[Company] = [c].[Id]))
```

**Generated SQL (Location):**
```sql
SELECT DISTINCT [c].[Company]
FROM [CompanyOffices] AS [c]
WHERE [c].[Location] = @loc OR [c].[Location] IN (
    SELECT [l].[Child] FROM [Locations_Unwound] AS [l] WHERE [l].[Parent] = @loc)
```

---

### 2. Variant A: Combined SQL (No Two-Query Split) ⭐ NEW WINNER

| Query Phase | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Rows |
|---|:---:|:---:|:---:|:---:|
| Combined query (Cat + Loc in 1 SQL) | 100 | 51 | 60 | 820 |
| Office map (IN + C# GroupBy) | 11 | 4 | 1 | 20 |
| Facets (OPENJSON) | 19 | 6 | 8 | 106 |
| **Total** | **137** | **63** | **70** | — |

**Why it works now:** The rebuilt SQL Server optimizer generates a clean plan for the combined `OR EXISTS` + `WHERE EXISTS(CompanyOffices)` query. Previously, this combination caused catastrophic backward index scans and nested-loop plan degradation (4–11s).

**Generated SQL (Combined):**
```sql
SELECT [c].[Id], [c].[Created]
FROM [CompanyProfiles] AS [c]
INNER JOIN [Accounts] AS [a] ON [c].[Id] = [a].[Id]
WHERE [a].[Status] = CAST(2 AS tinyint)
  AND [c].[TransactionType] & @tt > CAST(0 AS smallint)
  AND ([c].[Category] = @cat OR EXISTS (...Categories_Unwound...)
    OR EXISTS (...CompanyProducts + Products...))
  AND EXISTS (
    SELECT 1 FROM [CompanyOffices] AS [co]
    WHERE [co].[Company] = [c].[Id]
      AND ([co].[Location] = @loc OR [co].[Location] IN (
        SELECT [l].[Child] FROM [Locations_Unwound] AS [l] WHERE [l].[Parent] = @loc)))
```

---

### 3. Variant B: OPENJSON Location

| Query Phase | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Rows |
|---|:---:|:---:|:---:|:---:|
| Category query | 100 | 55 | 55 | 1,046 |
| Location OPENJSON (JOIN CompanyOffices) | 38 | 16 | 12 | 820 |
| Office map | 13 | 2 | 5 | 20 |
| Facets (OPENJSON) | 19 | 12 | 16 | 106 |
| **Total** | **180** | **88** | **91** | — |

**Generated SQL (Location OPENJSON):**
```sql
SELECT DISTINCT CAST(ids.[value] AS int) AS [Value]
FROM OPENJSON(@catIdsJson) ids
INNER JOIN CompanyOffices co ON co.Company = CAST(ids.[value] AS int)
WHERE co.[Location] = @loc
   OR co.[Location] IN (SELECT lu.Child FROM Locations_Unwound lu WHERE lu.Parent = @loc)
```

---

### 4. Variant C: EXISTS Location

| Query Phase | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Rows |
|---|:---:|:---:|:---:|:---:|
| Category query | 81 | 72 | 101 | 1,046 |
| Location EXISTS (OPENJSON + WHERE EXISTS) | 34 | 12 | 21 | 820 |
| Office map | 10 | 8 | 9 | 20 |
| Facets (OPENJSON) | 18 | 15 | 15 | 106 |
| **Total** | **152** | **110** | **149** | — |

**Generated SQL (Location EXISTS):**
```sql
SELECT CAST(ids.[value] AS int) AS [Value]
FROM OPENJSON(@catIdsJson) ids
WHERE EXISTS (
    SELECT 1 FROM CompanyOffices co
    WHERE co.Company = CAST(ids.[value] AS int)
      AND (co.[Location] = @loc
        OR co.[Location] IN (SELECT lu.Child FROM Locations_Unwound lu WHERE lu.Parent = @loc)))
```

---

### 5. Variant D: SQL-side ORDER BY ⚠️ (Previously: 60s timeout → Now: 179ms)

| Query Phase | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Rows |
|---|:---:|:---:|:---:|:---:|
| Query + ORDER BY DESC in SQL | 179 | 139 | 110 | 820 |
| Office map | 16 | 10 | 3 | 20 |
| Facets (OPENJSON) | 26 | 23 | 11 | 106 |
| **Total** | **228** | **174** | **129** | — |

**Previous Result (before rebuild):** 60+ second timeout. SQL Server scanned `IX_CompanyProfiles_Created` backwards while evaluating `OR EXISTS` subqueries row-by-row.

**Current Result:** The backward scan bug is **fixed** — it no longer causes timeouts. However, it's still ~2x slower than deferred C# sorting (179ms vs 100ms for the combined query alone) because SQL Server still adds sorting overhead to the execution plan.

**Verdict:** ⚠️ No longer catastrophic, but **still inferior to C# memory sort**. Keep deferring ORDER BY to application memory.

---

### 6. Variant E: GroupBy+First Office Map ⚠️ (Previously: 4,105ms → Now: 203ms cold / 3ms warm)

| Query Phase | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Rows |
|---|:---:|:---:|:---:|:---:|
| Combined query | 78 | 68 | 83 | 820 |
| Office GroupBy+First (EF Core SQL) | 203 | 3 | 3 | 20 |
| Facets (OPENJSON) | 13 | 7 | 7 | 106 |
| **Total** | **310** | **80** | **97** | — |

**Previous Result (before rebuild):** 4,105ms. EF Core generated a correlated `SELECT TOP(1)` subquery per group, re-evaluating the expensive location filter inside each one.

**Current Result:** Cold plan compilation is expensive (203ms for just 20 rows), but warm performance is excellent (3ms). The key difference is that this variant no longer re-evaluates the location filter — the office map query only uses `Company IN (@p1..@p20)`, making the GroupBy efficient once the plan is cached.

**Verdict:** ⚠️ Viable when warm, but cold penalty is 7x worse than in-memory GroupBy. **Keep the in-memory approach** as the default.

---

### 7. Variant F: ROW_NUMBER() Office Map ✅ (Previously: 34ms → Now: 28ms)

| Query Phase | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) | Rows |
|---|:---:|:---:|:---:|:---:|
| Combined query | 93 | 58 | 60 | 820 |
| Office ROW_NUMBER() (raw SQL + OPENJSON) | 28 | 18 | 12 | 20 |
| Facets (OPENJSON) | 22 | 8 | 13 | 106 |
| **Total** | **151** | **87** | **88** | — |

**Generated SQL (ROW_NUMBER Office Map):**
```sql
SELECT Company, Id AS Office FROM (
    SELECT co.Company, co.Id,
           ROW_NUMBER() OVER (PARTITION BY co.Company ORDER BY co.[Order]) AS rn
    FROM CompanyOffices co
    INNER JOIN OPENJSON(@pageIdsJson) ids ON co.Company = CAST(ids.[value] AS int)
) ranked WHERE rn = 1
```

**Verdict:** ✅ Consistently fast (12-28ms). Slightly slower than the in-memory approach (1-4ms warm) but the difference is negligible. The in-memory approach remains simpler.

---

### 8. LINQOld (TVF + Triple Query Penalty) ❌

| Metric | Cold (ms) | Warm-1 (ms) | Warm-2 (ms) |
|---|:---:|:---:|:---:|
| **Total** | **334** | **164** | **180** |

**Previous Result (before rebuild):** Massive I/O (24,776 logical reads on Locations_Unwound), triple-query penalty.

**Current Result:** Much faster than before but still **2.5x slower than Variant A** due to executing the base query three times (Count + Page + Facets).

**Verdict:** ❌ Still the worst LINQ approach. **Do not use.**

---

## Comparison vs. Previous Results (Before SQL Server Rebuild)

| Metric | Before Rebuild | After Rebuild | Improvement |
|---|:---:|:---:|:---:|
| **LINQNew cold** | ~4,430ms | ~748ms | **83% faster** |
| **LINQNew warm** | ~2,680ms | ~109ms | **96% faster** |
| **SP cold** | ~6,847ms | ~131ms | **98% faster** |
| **SP warm** | ~6,847ms | ~79ms | **99% faster** |
| **Backward scan (Variant D)** | 60,000ms+ timeout | ~228ms | **99.6% faster** |
| **GroupBy office (Variant E)** | 4,105ms | ~310ms | **92% faster** |

---

## Historical Optimizations Cross-Reference

All previously-tried optimizations from [LINQOptimizationFindings.md](file:///C:/Bizsort/bizsort-web/searchtest/LINQOptimizationFindings.md) have been re-validated:

### Category Filter (§1)
| Approach | Before Rebuild | After Rebuild | Status |
|---|:---:|:---:|---|
| OR EXISTS subquery (winner) | 2.3–3.8s | 79ms warm | ✅ Still best |
| Materialized `List<short>` (84→90 params) | 24,250ms timeout | Not retested | ❌ Still banned (padding rule) |
| UNION with materialized IDs | 15s+ | Not retested | ❌ Still banned |
| UNION with IQueryable subqueries | 15.2s+ | Not retested | ❌ Still banned |

### Location Filter (§2)
| Approach | Before Rebuild | After Rebuild | Status |
|---|:---:|:---:|---|
| IQueryable subquery (winner) | ~600ms | 17–50ms | ✅ Still best for split |
| TVF CompanyOfficeLocation | Massive I/O | ~164ms (in LINQOld) | ⚠️ Usable but slow |
| Materialized `List<int>` (566→600 params) | Timeout | Not retested | ❌ Still banned (padding rule) |
| Literal 566-value IN clause | Timeout | Not retested | ❌ Still banned |

### Office Map (§3)
| Approach | Before Rebuild | After Rebuild | Status |
|---|:---:|:---:|---|
| IN + C# memory GroupBy (winner) | ~60ms | 1–4ms warm | ✅ Still best |
| GroupBy+First (EF Core SQL) | 4,105ms | 3ms warm / 203ms cold | ⚠️ Fixed but cold penalty |
| ROW_NUMBER() Company-first | ~34ms | 12–28ms | ✅ Viable alternative |

### Facets (§4)
| Approach | Before Rebuild | After Rebuild | Status |
|---|:---:|:---:|---|
| OPENJSON INNER JOIN (winner) | ~330ms | 6–19ms warm | ✅ Still best |
| Contains 820 IDs (900 params) | 16s+ timeout | Not retested | ❌ Still banned (padding rule) |
| Derived table OPENJSON | 1,973ms | Not retested | ❌ Still banned |

### Architectural (§5)
| Approach | Before Rebuild | After Rebuild | Status |
|---|:---:|:---:|---|
| C# memory ORDER BY | ~2.5s | 51–100ms | ✅ Still best |
| SQL-side ORDER BY (backward scan) | 60s+ timeout | 110–179ms | ⚠️ Fixed but ~2x slower |
| Two-Query Split | ~4.4s total | 109–748ms | ✅ Valid but beaten by Combined |
| Combined SQL (no split) | Unstable plans | 63–137ms | ⭐ **Now the winner** |
| Parallel Task.WhenAll | InvalidOperation | Not retested | ❌ DbContext not thread-safe |

---

## Final Rankings

### Overall Winner: Variant A (Combined SQL)

| Rank | Strategy | Cold | Warm | Queries | Simplicity |
|:---:|---|:---:|:---:|:---:|:---:|
| 1 | **Variant A: Combined SQL** | 137ms | 63–70ms | 3 | ⭐ Simple |
| 2 | Variant F: ROW_NUMBER() Office | 151ms | 87–88ms | 3 | Good |
| 3 | Variant C: EXISTS Location | 152ms | 110–149ms | 4 | Moderate |
| 4 | Variant B: OPENJSON Location | 180ms | 88–91ms | 4 | Moderate |
| 5 | LINQNew (Two-Query Split) | 748ms | 109–117ms | 4 | Moderate |
| 6 | Variant D: SQL-side ORDER BY | 228ms | 129–174ms | 3 | Simple |
| 7 | Variant E: GroupBy+First Office | 310ms | 80–97ms | 3 | Simple |
| 8 | LINQOld (TVF + Triple Query) | 334ms | 164–180ms | 5+ | Complex |

### Recommendations

1. **✅ Adopt Combined SQL (Variant A)** as the production implementation
2. **✅ Keep C# memory ORDER BY** — SQL-side sorting is fixed but still 2x slower
3. **✅ Keep in-memory Office Map** — simplest and fastest warm (1–4ms)
4. **✅ Keep OPENJSON facets** — validated, 6–19ms warm
5. **⚠️ Monitor Combined SQL** — if plan degradation recurs with data growth, fall back to Two-Query Split (Variant C with EXISTS)
6. **❌ Never use `List<T>.Contains()` for >10 items** — parameter padding rule still critical

---

## Files

- [SearchTestClass.cs](file:///C:/Bizsort/bizsort-web/searchtest/SearchTestClass.cs) — All 9 implementations
- [Program.cs](file:///C:/Bizsort/bizsort-web/searchtest/Program.cs) — Benchmark harness
- [LINQOptimizationFindings.md](file:///C:/Bizsort/bizsort-web/searchtest/LINQOptimizationFindings.md) — Historical chronological record of all experiments
