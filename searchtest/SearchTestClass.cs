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
            var query = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
                .Select(x => x.c);

            // Category filter: keep as IQueryable OR EXISTS (Categories_Unwound).
            // EF Core's List<T>.Contains() pads to fixed bucket sizes (90 for 84 items, 600 for 566),
            // generating @catIds1..@catIds90 named params which destroys plan compilation.
            // OR EXISTS on Categories_Unwound is a compact 2-param subquery SQL Server handles cleanly.
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

            // Location filter: keep as IQueryable subquery — never materialize the child IDs.
            // EF Core pads List<int> to fixed bucket sizes (600 slots for 566 items),
            // generating @locIds1..@locIds600, which causes compilation timeouts.
            // WHERE Location = @loc OR Location IN (SELECT Child FROM Locations_Unwound WHERE Parent = @loc)
            // is a compact 1-param subquery SQL Server can handle efficiently.
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
                // Two-query split: category and location run as two independent SQL queries.
                // Each gets a clean execution plan with no cross-filter interference.
                // Intersect in C# memory using HashSet — avoids combined plan degradation.
                // NOTE: DbContext is not thread-safe — must await sequentially.
                var categoryMatches = await query
                    .Select(c => new { c.Id, c.Created })
                    .ToArrayAsync();

                var locationMatches = await locationCompanyIds.ToArrayAsync();

                var locationSet = new HashSet<int>(locationMatches);
                allMatchingIds = categoryMatches
                    .Where(c => locationSet.Contains(c.Id))
                    .OrderByDescending(c => c.Created)
                    .Select(c => c.Id)
                    .ToArray();
            }
            else
            {
                var allMatches = await query.Select(c => new { c.Id, c.Created }).ToArrayAsync();
                allMatchingIds = allMatches.OrderByDescending(c => c.Created).Select(c => c.Id).ToArray();
            }

            var total = allMatchingIds.Length;
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (locationCompanyIds != null)
            {
                // Fetch all offices for the 20 page companies using the Company index (20 seeks).
                // No location filter needed: all page companies already passed the location query,
                // so they are guaranteed to have at least one office in the searched location.
                // Pick the lowest-Order office per company — matches SP/TVF semantics (TOP 1 ORDER BY [Order]).
                var pageIdsArray = pageIds;
                var allOfficesForPage = await dbContext.CompanyOffices
                    .Where(co => pageIdsArray.Contains(co.Company))
                    .OrderBy(co => co.Order)
                    .Select(co => new { co.Company, co.Order, co.Id })
                    .ToArrayAsync();

                // Group in C# memory — O(n) over ~100 rows.
                var bestOfficeMap = allOfficesForPage
                    .GroupBy(o => o.Company)
                    .ToDictionary(g => g.Key, g => g.First()); // already ordered by Order

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
                // allMatchingIds.Contains(c.Id) generates 900 named params (820 IDs padded to nearest bucket).
                // OPENJSON join approach: pass IDs as a single JSON string, join CompanyFacets directly.
                // Unlike the FROM (subquery) pattern, this allows the optimizer to use the Company index.
                var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
                var pfq = await dbContext.Database
                    .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                        SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                        FROM CompanyFacets pf
                        INNER JOIN OPENJSON({0}) ids ON pf.Company = ids.value
                        INNER JOIN CompanyFacetValues pfv ON pf.FacetValue = pfv.Id
                        GROUP BY pfv.Name, pfv.Id", idsJson)
                    .ToArrayAsync();

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
