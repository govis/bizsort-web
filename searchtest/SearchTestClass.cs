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
            var activeCompanies = dbContext.CompanyProfiles
                .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
                .Where(x => x.a.Status == 2)
                .Select(x => x.c);

            IQueryable<CompanyProfile> query = activeCompanies;

            if (queryInput.Category > 0)
            {
                query = query.Where(c => 
                    (queryInput.TransactionType == 0 || (c.TransactionType & queryInput.TransactionType) > 0)
                    &&
                    (
                        (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                        ||
                        (from cp in dbContext.CompanyProducts
                         join p in dbContext.Products on cp.Product equals p.Id
                         where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                               (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                         select cp).Any(cp => cp.Company == c.Id)
                    )
                );
            }
            else if (queryInput.TransactionType > 0)
            {
                query = query.Where(c => (c.TransactionType & queryInput.TransactionType) > 0);
            }

            query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

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
                allMatchingIds = await query
                    .OrderByDescending(c => c.Created)
                    .Select(c => c.Id)
                    .ToArrayAsync();
            }

            var total = allMatchingIds.Length;
            var pageIds = allMatchingIds
                .Skip(queryInput.StartIndex)
                .Take(queryInput.Length > 0 ? queryInput.Length : 20)
                .ToArray();

            SearchItem[] companies;
            if (queryInput.Location > 0)
            {
                var officeMap = await (from c in dbContext.CompanyProfiles
                                       join co in dbContext.CompanyOfficeLocation(queryInput.Location) on c.Id equals co.Id
                                       where pageIds.Contains(c.Id)
                                       select new { c.Id, co.Office })
                                      .ToArrayAsync();

                companies = pageIds
                    .Select(id => {
                        var o = officeMap.FirstOrDefault(x => x.Id == id);
                        return new SearchItem { Id = id, Office = o != null && o.Office > 0 ? (int?)o.Office : null };
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
                var pfq = await (from c in dbContext.CompanyProfiles
                                 where allMatchingIds.Contains(c.Id)
                                 join pf in dbContext.CompanyFacets on c.Id equals pf.Company
                                 join pfv in dbContext.CompanyFacetValues on pf.FacetValue equals pfv.Id
                                 group pfv by new { pfv.Name, pfv.Id } into cfg
                                 select new BizSrt.Data.Extensions.FacetExtensions.ValueCount { Name = cfg.Key.Name, Value = cfg.Key.Id, Count = cfg.Count() })
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
                .Where(x => x.a.Status == 2)
                .Select(x => x.c);
                
            // NOTE: The "TransactionType Grouping Bug" is intentionally preserved here to match legacy behavior.
            // In this legacy code, the TransactionType check is grouped INSIDE the CategoryCheck, meaning
            // if a company matches via ProductCheck, it bypasses the TransactionType filter entirely.
            // This is fixed in CompanySearchLINQNew, CompanySearchLINQUnion, and the production codebase.
            var cq = from c in activeCompanies
                     where ((c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                     && (queryInput.TransactionType == 0 || (c.TransactionType & (byte)queryInput.TransactionType) > 0)) ||
                     (from cp in dbContext.CompanyProducts
                      join p in dbContext.Products on cp.Product equals p.Id
                      where (p.Type == 0 || (cp.UnlistedType == 1 && p.Status == 2)) &&
                          (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                      select cp).Any(cp => cp.Company == c.Id)
                     select c;

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
