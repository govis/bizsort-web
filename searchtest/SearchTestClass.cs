using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Data.Entities;
using BizSrt.Model.Company;
using BizSrt.Model.List;
using BizSrt.Data.Extensions;
using System.Collections.Generic;

namespace BizSrt.SearchTest
{
    // Used with SqlQueryRaw for the ROW_NUMBER() office map query.
    public class OfficeMapResult
    {
        public int Company { get; set; }
        public int Office { get; set; }
    }

    public class SearchParityTest
    {
        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQUnion(AppDbContext dbContext, BizSrt.Model.Company.SearchInput queryInput)
        {

            var activeCompanies = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active)
                .Select(x => x.c);

            IQueryable<CompanyProfile> query = activeCompanies;

            if (queryInput.Category > 0)
            {
                var companyCategoryMatches = dbContext.CompanyProfiles
                    .Where(c => c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    .Select(c => c.Id);

                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == (byte)BizSrt.Model.Product.UnlistedType.Listed && p.Status == (byte)BizSrt.Model.Product.Status.Active)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp.Company);

                var matchingCompanyIds = companyCategoryMatches.Union(productCategoryMatches);

                query = query.Where(c => 
                    (queryInput.TransactionType == 0 || (c.TransactionType & queryInput.TransactionType) > 0)
                    &&
                    matchingCompanyIds.Contains(c.Id)
                );
            }
            else if (queryInput.TransactionType > 0)
            {
                query = query.Where(c => (c.TransactionType & queryInput.TransactionType) > 0);
            }

            int[] allMatchingIds;
            if (queryInput.Location > 0)
            {
                var coq = from c in query
                          where dbContext.CompanyOfficeLocation(queryInput.Location).Any(co => co.Id == c.Id)
                          orderby c.Created descending
                          select c.Id;
                allMatchingIds = await coq.ToArrayAsync();
            }
            else
            {
                allMatchingIds = await query.OrderByDescending(c => c.Created).Select(c => c.Id).ToArrayAsync();
            }

            var total = allMatchingIds.Length;
            var pageIds = allMatchingIds.Skip(queryInput.StartIndex).Take(queryInput.Length > 0 ? queryInput.Length : 20).ToArray();
            
            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                var officeMap = await (from c in dbContext.CompanyProfiles
                                       join co in dbContext.CompanyOfficeLocation(queryInput.Location) on c.Id equals co.Id
                                       where pageIds.Contains(c.Id)
                                       select new { c.Id, co.Office }).ToArrayAsync();

                companies = pageIds.Select(id => {
                    var o = officeMap.FirstOrDefault(x => x.Id == id);
                    return new SearchItem { Id = id, Office = o != null && o.Office > 0 ? (int?)o.Office : null };
                }).ToArray();
            }
            else
            {
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            return new SearchOutput<SearchItem> { StartIndex = queryInput.StartIndex, Series = companies, TotalCount = total, Facets = null };
        }

        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQNew(AppDbContext dbContext, SearchInput queryInput)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            if (queryInput.Category > 0)
            {
                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp);

                query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    || productCategoryMatches.Any(cp => cp.Company == c.Id));
            }
            else if (queryInput.TransactionType > 0)
            {
                query = query.Where(c => (c.TransactionType & queryInput.TransactionType) > 0);
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

            IQueryable<int>? locationCompanyIds = null;
            if (queryInput.Location > 0)
            {
                var childLocations = dbContext.Locations_Unwound
                    .Where(lu => lu.Parent == queryInput.Location)
                    .Select(lu => lu.Child);

                locationCompanyIds = dbContext.CompanyOffices
                    .Where(co => co.Location == queryInput.Location || childLocations.Contains(co.Location))
                    .Select(co => co.Company)
                    .Distinct();
            }

            int[] allMatchingIds;

            if (locationCompanyIds != null)
            {
                sw.Restart();
                var categoryMatches = await query
                    .Select(c => new { c.Id, c.Created })
                    .ToArrayAsync();
                var catTime = sw.ElapsedMilliseconds;

                sw.Restart();
                var locationMatches = await locationCompanyIds.ToArrayAsync();
                var locTime = sw.ElapsedMilliseconds;

                Console.WriteLine($"    ├─ Category query:  {catTime}ms  ({categoryMatches.Length} rows)");
                Console.WriteLine($"    ├─ Location query:  {locTime}ms  ({locationMatches.Length} rows)");

                var locationSet = new HashSet<int>(locationMatches);
                allMatchingIds = categoryMatches
                    .Where(c => locationSet.Contains(c.Id))
                    .OrderByDescending(c => c.Created)
                    .Select(c => c.Id)
                    .ToArray();
            }
            else
            {
                sw.Restart();
                var allMatches = await query.Select(c => new { c.Id, c.Created }).ToArrayAsync();
                var catTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Category query:  {catTime}ms  ({allMatches.Length} rows)");
                allMatchingIds = allMatches.OrderByDescending(c => c.Created).Select(c => c.Id).ToArray();
            }

            var total = allMatchingIds.Length;
            Console.WriteLine($"    ├─ Intersection:    {total} matching IDs");
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (locationCompanyIds != null)
            {
                sw.Restart();
                var pageIdsArray = pageIds;
                var allOfficesForPage = await dbContext.CompanyOffices
                    .Where(co => pageIdsArray.Contains(co.Company))
                    .OrderBy(co => co.Order)
                    .Select(co => new { co.Company, co.Order, co.Id })
                    .ToArrayAsync();
                var officeTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Office map:      {officeTime}ms  ({allOfficesForPage.Length} rows)");

                var bestOfficeMap = allOfficesForPage
                    .GroupBy(o => o.Company)
                    .ToDictionary(g => g.Key, g => g.First());

                companies = pageIds
                    .Select(id =>
                    {
                        bestOfficeMap.TryGetValue(id, out var o);
                        return new SearchItem
                        {
                            Id = id,
                            Office = o != null && o.Order != 0 ? (int?)o.Id : null
                        };
                    })
                    .ToArray();
            }
            else
            {
                Console.WriteLine($"    ├─ Office map:      skipped (no location)");
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            BizSrt.Model.Semantic.FacetName[]? facets = null;
            if (queryInput.InclFacets != null)
            {
                sw.Restart();
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();
                var facetTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    └─ Facets query:    {facetTime}ms  ({pfq.Length} facet values)");

                facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = companies,
                TotalCount = total,
                Facets = facets
            };
        }

        /// <summary>
        /// Variant A: Combined SQL — no two-query split.
        /// Category + Location combined in a single SQL query using WHERE EXISTS.
        /// Tests whether the rebuilt SQL Server handles the combined plan better.
        /// </summary>
        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQCombined(AppDbContext dbContext, SearchInput queryInput)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            if (queryInput.Category > 0)
            {
                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp);

                query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    || productCategoryMatches.Any(cp => cp.Company == c.Id));
            }
            else if (queryInput.TransactionType > 0)
            {
                query = query.Where(c => (c.TransactionType & queryInput.TransactionType) > 0);
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

            // Combined: add location filter directly to the IQueryable (no split)
            if (queryInput.Location > 0)
            {
                var childLocations = dbContext.Locations_Unwound
                    .Where(lu => lu.Parent == queryInput.Location)
                    .Select(lu => lu.Child);

                query = query.Where(c => dbContext.CompanyOffices
                    .Any(co => co.Company == c.Id &&
                        (co.Location == queryInput.Location || childLocations.Contains(co.Location))));
            }

            sw.Restart();
            var allMatches = await query
                .Select(c => new { c.Id, c.Created })
                .ToArrayAsync();
            var combinedTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"    ├─ Combined query:  {combinedTime}ms  ({allMatches.Length} rows)");

            var allMatchingIds = allMatches
                .OrderByDescending(c => c.Created)
                .Select(c => c.Id)
                .ToArray();

            var total = allMatchingIds.Length;
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                sw.Restart();
                var pageIdsArray = pageIds;
                var allOfficesForPage = await dbContext.CompanyOffices
                    .Where(co => pageIdsArray.Contains(co.Company))
                    .OrderBy(co => co.Order)
                    .Select(co => new { co.Company, co.Order, co.Id })
                    .ToArrayAsync();
                var officeTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Office map:      {officeTime}ms  ({allOfficesForPage.Length} rows)");

                var bestOfficeMap = allOfficesForPage
                    .GroupBy(o => o.Company)
                    .ToDictionary(g => g.Key, g => g.First());

                companies = pageIds
                    .Select(id =>
                    {
                        bestOfficeMap.TryGetValue(id, out var o);
                        return new SearchItem
                        {
                            Id = id,
                            Office = o != null && o.Order != 0 ? (int?)o.Id : null
                        };
                    })
                    .ToArray();
            }
            else
            {
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            BizSrt.Model.Semantic.FacetName[]? facets = null;
            if (queryInput.InclFacets != null)
            {
                sw.Restart();
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();
                var facetTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    └─ Facets query:    {facetTime}ms  ({pfq.Length} facet values)");

                facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = companies,
                TotalCount = total,
                Facets = facets
            };
        }

        /// <summary>
        /// Variant B: OPENJSON for Location — materialize category IDs first,
        /// then pass them via OPENJSON to a raw SQL location-intersected query.
        /// Tests whether OPENJSON is faster than the IQueryable subquery for location.
        /// </summary>
        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQOpenJsonLocation(AppDbContext dbContext, SearchInput queryInput)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            if (queryInput.Category > 0)
            {
                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp);

                query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    || productCategoryMatches.Any(cp => cp.Company == c.Id));
            }
            else if (queryInput.TransactionType > 0)
            {
                query = query.Where(c => (c.TransactionType & queryInput.TransactionType) > 0);
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

            // Step 1: category query (same as LINQNew)
            sw.Restart();
            var categoryMatches = await query
                .Select(c => new { c.Id, c.Created })
                .ToArrayAsync();
            var catTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"    ├─ Category query:  {catTime}ms  ({categoryMatches.Length} rows)");

            int[] allMatchingIds;

            if (queryInput.Location > 0)
            {
                // Step 2: Use OPENJSON to pass category IDs + do location intersection in SQL
                sw.Restart();
                var catIdsJson = System.Text.Json.JsonSerializer.Serialize(categoryMatches.Select(c => c.Id).ToArray());
                var intersected = await dbContext.Database
                    .SqlQueryRaw<int>(@"
                        SELECT DISTINCT CAST(ids.[value] AS int) AS [Value]
                        FROM OPENJSON({0}) ids
                        INNER JOIN CompanyOffices co ON co.Company = CAST(ids.[value] AS int)
                        WHERE co.[Location] = {1}
                           OR co.[Location] IN (SELECT lu.Child FROM Locations_Unwound lu WHERE lu.Parent = {1})",
                        catIdsJson, queryInput.Location)
                    .ToArrayAsync();
                var locTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Location OPENJSON: {locTime}ms  ({intersected.Length} rows)");

                // We need Created for ordering — look it up from categoryMatches
                var intersectedSet = new HashSet<int>(intersected);
                allMatchingIds = categoryMatches
                    .Where(c => intersectedSet.Contains(c.Id))
                    .OrderByDescending(c => c.Created)
                    .Select(c => c.Id)
                    .ToArray();
            }
            else
            {
                allMatchingIds = categoryMatches
                    .OrderByDescending(c => c.Created)
                    .Select(c => c.Id)
                    .ToArray();
            }

            var total = allMatchingIds.Length;
            Console.WriteLine($"    ├─ Intersection:    {total} matching IDs");
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                sw.Restart();
                var pageIdsArray = pageIds;
                var allOfficesForPage = await dbContext.CompanyOffices
                    .Where(co => pageIdsArray.Contains(co.Company))
                    .OrderBy(co => co.Order)
                    .Select(co => new { co.Company, co.Order, co.Id })
                    .ToArrayAsync();
                var officeTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Office map:      {officeTime}ms  ({allOfficesForPage.Length} rows)");

                var bestOfficeMap = allOfficesForPage
                    .GroupBy(o => o.Company)
                    .ToDictionary(g => g.Key, g => g.First());

                companies = pageIds
                    .Select(id =>
                    {
                        bestOfficeMap.TryGetValue(id, out var o);
                        return new SearchItem
                        {
                            Id = id,
                            Office = o != null && o.Order != 0 ? (int?)o.Id : null
                        };
                    })
                    .ToArray();
            }
            else
            {
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            BizSrt.Model.Semantic.FacetName[]? facets = null;
            if (queryInput.InclFacets != null)
            {
                sw.Restart();
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();
                var facetTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    └─ Facets query:    {facetTime}ms  ({pfq.Length} facet values)");

                facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = companies,
                TotalCount = total,
                Facets = facets
            };
        }

        /// <summary>
        /// Variant C: EXISTS-based Location — use .Any() instead of .Distinct() for location dedup.
        /// The current LINQNew uses DISTINCT on CompanyOffices. This variant tests whether
        /// EXISTS (correlated subquery per category row) is faster than the DISTINCT approach.
        /// Still uses the two-query split.
        /// </summary>
        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQExistsLocation(AppDbContext dbContext, SearchInput queryInput)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            if (queryInput.Category > 0)
            {
                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp);

                query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    || productCategoryMatches.Any(cp => cp.Company == c.Id));
            }
            else if (queryInput.TransactionType > 0)
            {
                query = query.Where(c => (c.TransactionType & queryInput.TransactionType) > 0);
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

            // Step 1: category query (same as LINQNew)
            sw.Restart();
            var categoryMatches = await query
                .Select(c => new { c.Id, c.Created })
                .ToArrayAsync();
            var catTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"    ├─ Category query:  {catTime}ms  ({categoryMatches.Length} rows)");

            int[] allMatchingIds;

            if (queryInput.Location > 0)
            {
                // EXISTS-based: for each category-matched company, check if it has an office in the location
                // This avoids the DISTINCT aggregate on CompanyOffices
                var childLocations = dbContext.Locations_Unwound
                    .Where(lu => lu.Parent == queryInput.Location)
                    .Select(lu => lu.Child);

                sw.Restart();
                // Use a raw SQL approach: pass category IDs via OPENJSON, then filter with EXISTS on offices
                var catIdsJson = System.Text.Json.JsonSerializer.Serialize(categoryMatches.Select(c => c.Id).ToArray());
                var intersected = await dbContext.Database
                    .SqlQueryRaw<int>(@"
                        SELECT CAST(ids.[value] AS int) AS [Value]
                        FROM OPENJSON({0}) ids
                        WHERE EXISTS (
                            SELECT 1 FROM CompanyOffices co
                            WHERE co.Company = CAST(ids.[value] AS int)
                              AND (co.[Location] = {1}
                                OR co.[Location] IN (SELECT lu.Child FROM Locations_Unwound lu WHERE lu.Parent = {1}))
                        )",
                        catIdsJson, queryInput.Location)
                    .ToArrayAsync();
                var locTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Location EXISTS: {locTime}ms  ({intersected.Length} rows)");

                var intersectedSet = new HashSet<int>(intersected);
                allMatchingIds = categoryMatches
                    .Where(c => intersectedSet.Contains(c.Id))
                    .OrderByDescending(c => c.Created)
                    .Select(c => c.Id)
                    .ToArray();
            }
            else
            {
                allMatchingIds = categoryMatches
                    .OrderByDescending(c => c.Created)
                    .Select(c => c.Id)
                    .ToArray();
            }

            var total = allMatchingIds.Length;
            Console.WriteLine($"    ├─ Intersection:    {total} matching IDs");
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                sw.Restart();
                var pageIdsArray = pageIds;
                var allOfficesForPage = await dbContext.CompanyOffices
                    .Where(co => pageIdsArray.Contains(co.Company))
                    .OrderBy(co => co.Order)
                    .Select(co => new { co.Company, co.Order, co.Id })
                    .ToArrayAsync();
                var officeTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Office map:      {officeTime}ms  ({allOfficesForPage.Length} rows)");

                var bestOfficeMap = allOfficesForPage
                    .GroupBy(o => o.Company)
                    .ToDictionary(g => g.Key, g => g.First());

                companies = pageIds
                    .Select(id =>
                    {
                        bestOfficeMap.TryGetValue(id, out var o);
                        return new SearchItem
                        {
                            Id = id,
                            Office = o != null && o.Order != 0 ? (int?)o.Id : null
                        };
                    })
                    .ToArray();
            }
            else
            {
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            BizSrt.Model.Semantic.FacetName[]? facets = null;
            if (queryInput.InclFacets != null)
            {
                sw.Restart();
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();
                var facetTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    └─ Facets query:    {facetTime}ms  ({pfq.Length} facet values)");

                facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = companies,
                TotalCount = total,
                Facets = facets
            };
        }

        /// <summary>
        /// Variant D: SQL-side ORDER BY — re-tests the backward scan bug.
        /// Previously this caused 60+ second timeouts because SQL Server scanned 
        /// IX_CompanyProfiles_Created backwards while evaluating OR EXISTS subqueries row-by-row.
        /// Tests whether the rebuilt SQL Server handles this plan better.
        /// Uses Combined SQL (Variant A base) + SQL-side OrderByDescending.
        /// </summary>
        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQSqlOrderBy(AppDbContext dbContext, SearchInput queryInput)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            if (queryInput.Category > 0)
            {
                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp);

                query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    || productCategoryMatches.Any(cp => cp.Company == c.Id));
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

            if (queryInput.Location > 0)
            {
                var childLocations = dbContext.Locations_Unwound
                    .Where(lu => lu.Parent == queryInput.Location)
                    .Select(lu => lu.Child);

                query = query.Where(c => dbContext.CompanyOffices
                    .Any(co => co.Company == c.Id &&
                        (co.Location == queryInput.Location || childLocations.Contains(co.Location))));
            }

            // THIS IS THE KEY DIFFERENCE: ORDER BY in SQL instead of C# memory
            sw.Restart();
            var allMatchingIds = await query
                .OrderByDescending(c => c.Created)
                .Select(c => c.Id)
                .ToArrayAsync();
            var queryTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"    ├─ Query+Sort SQL:  {queryTime}ms  ({allMatchingIds.Length} rows)");

            var total = allMatchingIds.Length;
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                sw.Restart();
                var pageIdsArray = pageIds;
                var allOfficesForPage = await dbContext.CompanyOffices
                    .Where(co => pageIdsArray.Contains(co.Company))
                    .OrderBy(co => co.Order)
                    .Select(co => new { co.Company, co.Order, co.Id })
                    .ToArrayAsync();
                var officeTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Office map:      {officeTime}ms  ({allOfficesForPage.Length} rows)");

                var bestOfficeMap = allOfficesForPage
                    .GroupBy(o => o.Company)
                    .ToDictionary(g => g.Key, g => g.First());

                companies = pageIds
                    .Select(id =>
                    {
                        bestOfficeMap.TryGetValue(id, out var o);
                        return new SearchItem
                        {
                            Id = id,
                            Office = o != null && o.Order != 0 ? (int?)o.Id : null
                        };
                    })
                    .ToArray();
            }
            else
            {
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            BizSrt.Model.Semantic.FacetName[]? facets = null;
            if (queryInput.InclFacets != null)
            {
                sw.Restart();
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();
                var facetTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    └─ Facets query:    {facetTime}ms  ({pfq.Length} facet values)");

                facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = companies,
                TotalCount = total,
                Facets = facets
            };
        }

        /// <summary>
        /// Variant E: GroupBy + First Office Map.
        /// Re-tests the EF Core GroupBy().Select(g => g.OrderBy().First()) approach
        /// that previously generated correlated SELECT TOP(1) subqueries (4,105ms).
        /// Uses Combined SQL (Variant A base) but with GroupBy office selection.
        /// </summary>
        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQGroupByOffice(AppDbContext dbContext, SearchInput queryInput)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            if (queryInput.Category > 0)
            {
                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp);

                query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    || productCategoryMatches.Any(cp => cp.Company == c.Id));
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

            if (queryInput.Location > 0)
            {
                var childLocations = dbContext.Locations_Unwound
                    .Where(lu => lu.Parent == queryInput.Location)
                    .Select(lu => lu.Child);

                query = query.Where(c => dbContext.CompanyOffices
                    .Any(co => co.Company == c.Id &&
                        (co.Location == queryInput.Location || childLocations.Contains(co.Location))));
            }

            sw.Restart();
            var allMatches = await query
                .Select(c => new { c.Id, c.Created })
                .ToArrayAsync();
            var queryTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"    ├─ Combined query:  {queryTime}ms  ({allMatches.Length} rows)");

            var allMatchingIds = allMatches
                .OrderByDescending(c => c.Created)
                .Select(c => c.Id)
                .ToArray();

            var total = allMatchingIds.Length;
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                // THIS IS THE KEY DIFFERENCE: EF Core GroupBy + First in SQL
                sw.Restart();
                var pageIdsArray = pageIds;
                var officeMap = await dbContext.CompanyOffices
                    .Where(co => pageIdsArray.Contains(co.Company))
                    .GroupBy(co => co.Company)
                    .Select(g => new { Company = g.Key, Office = g.OrderBy(x => x.Order).First() })
                    .ToArrayAsync();
                var officeTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Office GroupBy:  {officeTime}ms  ({officeMap.Length} rows)");

                var bestOfficeMap = officeMap.ToDictionary(o => o.Company, o => o.Office);

                companies = pageIds
                    .Select(id =>
                    {
                        bestOfficeMap.TryGetValue(id, out var o);
                        return new SearchItem
                        {
                            Id = id,
                            Office = o != null && o.Order != 0 ? (int?)o.Id : null
                        };
                    })
                    .ToArray();
            }
            else
            {
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            BizSrt.Model.Semantic.FacetName[]? facets = null;
            if (queryInput.InclFacets != null)
            {
                sw.Restart();
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();
                var facetTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    └─ Facets query:    {facetTime}ms  ({pfq.Length} facet values)");

                facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = companies,
                TotalCount = total,
                Facets = facets
            };
        }

        /// <summary>
        /// Variant F: ROW_NUMBER() Office Map.
        /// Re-tests raw SQL ROW_NUMBER() OVER (PARTITION BY Company ORDER BY [Order])
        /// with Company IN (@pageIds) as the leading predicate.
        /// Previously was fast (34ms) but required fragile SQL string building.
        /// </summary>
        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQRowNumberOffice(AppDbContext dbContext, SearchInput queryInput)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            if (queryInput.Category > 0)
            {
                var productCategoryMatches = (from cp in dbContext.CompanyProducts
                                              join p in dbContext.Products on cp.Product equals p.Id
                                              where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                                                    (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                              select cp);

                query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                    || productCategoryMatches.Any(cp => cp.Company == c.Id));
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

            if (queryInput.Location > 0)
            {
                var childLocations = dbContext.Locations_Unwound
                    .Where(lu => lu.Parent == queryInput.Location)
                    .Select(lu => lu.Child);

                query = query.Where(c => dbContext.CompanyOffices
                    .Any(co => co.Company == c.Id &&
                        (co.Location == queryInput.Location || childLocations.Contains(co.Location))));
            }

            sw.Restart();
            var allMatches = await query
                .Select(c => new { c.Id, c.Created })
                .ToArrayAsync();
            var queryTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"    ├─ Combined query:  {queryTime}ms  ({allMatches.Length} rows)");

            var allMatchingIds = allMatches
                .OrderByDescending(c => c.Created)
                .Select(c => c.Id)
                .ToArray();

            var total = allMatchingIds.Length;
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                // THIS IS THE KEY DIFFERENCE: ROW_NUMBER() via raw SQL with OPENJSON
                sw.Restart();
                var pageIdsJson = System.Text.Json.JsonSerializer.Serialize(pageIds);
                var officeMap = await dbContext.Database
                    .SqlQueryRaw<OfficeMapResult>(@"
                        SELECT Company, Id AS Office FROM (
                            SELECT co.Company, co.Id, ROW_NUMBER() OVER (PARTITION BY co.Company ORDER BY co.[Order]) AS rn
                            FROM CompanyOffices co
                            INNER JOIN OPENJSON({0}) ids ON co.Company = CAST(ids.[value] AS int)
                        ) ranked WHERE rn = 1", pageIdsJson)
                    .ToArrayAsync();
                var officeTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    ├─ Office ROW_NUM: {officeTime}ms  ({officeMap.Length} rows)");

                var bestOfficeMap = officeMap.ToDictionary(o => o.Company, o => o.Office);

                companies = pageIds
                    .Select(id =>
                    {
                        bestOfficeMap.TryGetValue(id, out var o);
                        return new SearchItem
                        {
                            Id = id,
                            Office = o > 0 ? (int?)o : null
                        };
                    })
                    .ToArray();
            }
            else
            {
                companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();
            }

            BizSrt.Model.Semantic.FacetName[]? facets = null;
            if (queryInput.InclFacets != null)
            {
                sw.Restart();
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();
                var facetTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"    └─ Facets query:    {facetTime}ms  ({pfq.Length} facet values)");

                facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = companies,
                TotalCount = total,
                Facets = facets
            };
        }

        public static async Task<SearchOutput<SearchItem>> CompanySearchSQL(AppDbContext dbContext, SearchInput queryInput)
        {
            var connection = dbContext.Database.GetDbConnection();
            var wasClosed = connection.State == System.Data.ConnectionState.Closed;

            if (wasClosed)
                await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "CompanySearchParityTest";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                var pTransactionType = command.CreateParameter();
                pTransactionType.ParameterName = "@TransactionType";
                pTransactionType.Value = (short)queryInput.TransactionType;
                command.Parameters.Add(pTransactionType);

                var pCategory = command.CreateParameter();
                pCategory.ParameterName = "@Category";
                pCategory.Value = queryInput.Category;
                command.Parameters.Add(pCategory);

                var pQuery = command.CreateParameter();
                pQuery.ParameterName = "@Query";
                pQuery.Value = queryInput.SearchQuery ?? (object)DBNull.Value;
                command.Parameters.Add(pQuery);

                var pLocation = command.CreateParameter();
                pLocation.ParameterName = "@Location";
                pLocation.Value = queryInput.Location;
                command.Parameters.Add(pLocation);

                if (queryInput.Location == 0 && queryInput.SearchNear != null)
                {
                    var pLat = command.CreateParameter();
                    pLat.ParameterName = "@Lattitude";
                    pLat.Value = queryInput.SearchNear.Lat;
                    command.Parameters.Add(pLat);

                    var pLng = command.CreateParameter();
                    pLng.ParameterName = "@Longitude";
                    pLng.Value = queryInput.SearchNear.Lng;
                    command.Parameters.Add(pLng);

                    var pDist = command.CreateParameter();
                    pDist.ParameterName = "@Distance";
                    pDist.Value = 100;
                    command.Parameters.Add(pDist);
                }

                if (queryInput.StartIndex > 0)
                {
                    var pStart = command.CreateParameter();
                    pStart.ParameterName = "@StartIndex";
                    pStart.Value = queryInput.StartIndex;
                    command.Parameters.Add(pStart);
                }

                var pLength = command.CreateParameter();
                pLength.ParameterName = "@Length";
                pLength.DbType = System.Data.DbType.Int32;
                pLength.Direction = System.Data.ParameterDirection.InputOutput;
                pLength.Value = queryInput.Length > 0 ? queryInput.Length : 20;
                command.Parameters.Add(pLength);

                var companies = new List<SearchItem>();
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        companies.Add(new SearchItem
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Office = reader.GetInt32(reader.GetOrdinal("Office")) > 0 ? reader.GetInt32(reader.GetOrdinal("Office")) : null,
                            Distance = queryInput.SearchNear != null ? (float)reader.GetDouble(reader.GetOrdinal("Distance")) : 0f
                        });
                    }
                }

                return new SearchOutput<SearchItem>
                {
                    StartIndex = queryInput.StartIndex,
                    Series = companies.ToArray(),
                    TotalCount = pLength.Value != DBNull.Value ? Convert.ToInt32(pLength.Value) : 0
                };
            }
            finally
            {
                if (wasClosed)
                    await connection.CloseAsync();
            }
        }

        public static async Task<SearchOutput<SearchItem>> CompanySearchLINQOld(AppDbContext dbContext, SearchInput queryInput)
        {
            var activeCompanies = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);
                
            IQueryable<CompanyProfile> cq = activeCompanies;

            if (queryInput.Category > 0)
            {
                cq = from c in cq
                     where (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category)) ||
                     (from cp in dbContext.CompanyProducts
                      join p in dbContext.Products on cp.Product equals p.Id
                      where (p.Type == 0 || (cp.UnlistedType == (byte)BizSrt.Model.Product.UnlistedType.Listed && p.Status == (byte)BizSrt.Model.Product.Status.Active)) &&
                          (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                      select cp).Any(cp => cp.Company == c.Id)
                     select c;
            }

            var output = new SearchOutput<SearchItem>();

            if (queryInput.Location > 0)
            {
                var coq = from c in cq
                          join co in dbContext.CompanyOfficeLocation(queryInput.Location) on c.Id equals co.Id
                          select new { Company = c, co.Office };

                var total = await coq.CountAsync();
                var paged = await coq.OrderByDescending(si => si.Company.Created)
                                     .Skip(queryInput.StartIndex)
                                     .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                                     .ToArrayAsync();

                output.TotalCount = total;
                output.Series = paged.Select(si => new SearchItem { Id = si.Company.Id, Office = si.Office > 0 ? (int?)si.Office : null }).ToArray();
                cq = coq.Select(co => co.Company);
            }
            else
            {
                var total = await cq.CountAsync();
                var paged = await cq.OrderByDescending(c => c.Created)
                                    .Skip(queryInput.StartIndex)
                                    .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                                    .ToArrayAsync();

                output.TotalCount = total;
                output.Series = paged.Select(c => new SearchItem { Id = c.Id }).ToArray();
            }

            if (queryInput.StartIndex == 0 && queryInput.InclFacets != null)
            {
                var bfq = await (from c in cq
                          join cf in dbContext.CompanyFacets on c.Id equals cf.Company
                          join cfv in dbContext.CompanyFacetValues on cf.FacetValue equals cfv.Id
                          group cfv by new { cfv.Name, cfv.Id } into cfg
                          select new BizSrt.Data.Extensions.FacetExtensions.ValueCount { Name = cfg.Key.Name, Value = cfg.Key.Id, Count = cfg.Count() }).ToArrayAsync();

                output.Facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(bfq, queryInput.InclFacets, output.TotalCount);
            }

            return output;
        }
    }
}
