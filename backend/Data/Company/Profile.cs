using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Data.Entities;
using BizSrt.Model.Company;
using BizSrt.Model;
using BizSrt.Model.List;
using BizSrt.Api.Data.Cache.Company;
using BizSrt.Data.Extensions;

namespace BizSrt.Api.Data.Company;

public interface ICompanyService
{
    Task<Profile?> ViewAsync(int id);
    Task<SliceOutput<SearchItem>> GetFeaturedAsync(DirectorySliceInput<int> sliceInput);
    Task<SearchOutput<SearchItem>> SearchAsync(SearchInput queryInput);
    Task<IEnumerable<BizSrt.Model.Company.Preview>> ToPreviewAsync(SearchItem[] companies);
    Task<SliceOutput<EntityId<int>>> GetCommunitiesAsync(int companyId, SliceInput sliceInput);
    Task<SliceOutput<SearchItem>> GetAffiliationsAsync(int companyId, SliceInput sliceInput);
    Task<QueryOutput<BizSrt.Model.EntityId<long>>> GetProductsAsync(int companyId, QueryInput queryInput);
    Task<QueryOutput<BizSrt.Model.EntityId<long>>> GetProjectsAsync(int companyId, QueryInput queryInput);
    Task<QueryOutput<BizSrt.Model.EntityId<long>>> GetJobsAsync(int companyId, short department, QueryInput queryInput);
    Task<IEnumerable<BizSrt.Model.Promotion.Preview>> GetPromotionsAsync(int companyId);
    Task<BizSrt.Model.Account?> GetInfoAsync(int id);
    Task<BizSrt.Model.Product.Profile?> GetProductProfileAsync(int companyId, long productId);
    Task<BizSrt.Model.Job.Profile?> GetJobProfileAsync(int companyId, long jobId);
    Task<BizSrt.Model.Project.Profile?> GetProjectProfileAsync(int companyId, long projectId);
}

public class CompanyService(AppDbContext dbContext) : ICompanyService
{
    public async Task<Profile?> ViewAsync(int id)
    {
        var company = await dbContext.CompanyProfiles
            .Include(c => c.Offices)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (company is null) return null;

        string? categoryName = null;
        if (company.Category > 0)
        {
            categoryName = await dbContext.Categories
                .Where(c => c.Id == company.Category)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        var offices = company.Offices.OrderBy(o => o.Order).Select(o => new BizSrt.Model.Company.Office
        {
            Id = o.Id,
            Name = o.Name ?? "",
            Phone = o.Phone ?? "",
            Phone1 = o.Phone1,
            Fax = o.Fax,
            Location = new BizSrt.Model.Location 
            { 
                Address = $"{o.StreetNumber} {o.Address1}, {o.PostalCode}".Trim().Trim(','),
                GeoLocation = o.GeoLocation is NetTopologySuite.Geometries.Point p 
                    ? new BizSrt.Model.Geolocation { Lat = p.Y, Lng = p.X } 
                    : null
            }
        }).ToArray();

        var profile = new Profile
        {
            Id = company.Id,
            Name = company.Name,
            Email = (company.Options & 1) > 0 ? company.Email : null,
            WebSite = company.WebSite,
            RichText = company.RichText is not null ? System.Text.Encoding.UTF8.GetString(company.RichText) : null,
            Text = company.Text,
            Category = company.Category > 0 ? new BizSrt.Model.Category { Id = company.Category, Name = categoryName ?? "" } : null,
            HeadOffice = offices.Length > 0 ? offices[0] : null,
            Offices = offices,
            Offerings = new Page_Offerings { View = ProductsView.NoProducts, HideOfferings = false },
            HasAffiliations = await dbContext.CompanyAffiliations.AnyAsync(a => a.From == id || (a.To == id && !a.Pending)),
            HasCommunities = await dbContext.CompanyCommunities.AnyAsync(cc => cc.Company == id)
        };
        
        return profile;
    }

    public Task<SliceOutput<SearchItem>> GetFeaturedAsync(DirectorySliceInput<int> sliceInput)
    {
        // Apply default Location (1 = Canada) if not specified by the frontend search widget
        if (sliceInput.Location == 0) sliceInput.Location = 1;

        var companies = new List<int>(); 
        int company;
        var cached = BizSrt.Api.Data.Cache.LegacyCache.FeaturedCompanies[new Tuple<short, int>(sliceInput.Category, sliceInput.Location), sliceInput.Index == 0 && sliceInput.Length > 1];
        var index = sliceInput.Index;
        
        if (sliceInput.Skip == null || sliceInput.Skip.Length < cached.Length)
        {
            while (companies.Count < sliceInput.Length && index < cached.Length)
            {
                company = cached[index];
                if (sliceInput.Skip == null || !sliceInput.Skip.Contains(company))
                    companies.Add(company);
                if (++index >= cached.Length)
                {
                    if (cached.Length <= sliceInput.Length)
                    {
                        index = -1;
                        break;
                    }
                    else
                    {
                        index = 0;
                        sliceInput.Skip = null;
                    }
                }
            }
        }
        
        return Task.FromResult(new SliceOutput<SearchItem>(companies.Select(b => new SearchItem { Id = b }).ToArray(), index));
    }

    public async Task<SearchOutput<SearchItem>> SearchAsync(SearchInput queryInput)
    {
        if (!string.IsNullOrWhiteSpace(queryInput.SearchQuery) || queryInput.SearchNear != null)
        {
            return await ExecuteCompanySearchSpAsync(queryInput);
        }

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
                                          where (p.Type == 0 || (cp.UnlistedType == (byte)BizSrt.Model.Product.UnlistedType.Listed && p.Status == (byte)BizSrt.Model.Product.Status.Active)) &&
                                                (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                                          select cp);

            query = query.Where(c => (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                || productCategoryMatches.Any(cp => cp.Company == c.Id));
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
            .Take(queryInput.Length > 0 ? queryInput.Length : 100)
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


    // =====================================================================================
    // REFERENCE IMPLEMENTATION ONLY
    // =====================================================================================
    // This is the pre-optimization baseline implementation of CompanySearch (matches legacy logic).
    // It is kept purely for historical reference and performance comparison purposes.
    // DO NOT modify, optimize, or use this method in production code.
    // For the actual implementation, see SearchAsync() above.
    // =====================================================================================
    public async Task<SearchOutput<SearchItem>> Reference_SearchAsync(SearchInput queryInput)
    {
        if (!string.IsNullOrWhiteSpace(queryInput.SearchQuery) || queryInput.SearchNear != null)
        {
            return await ExecuteCompanySearchSpAsync(queryInput);
        }

        var activeCompanies = dbContext.CompanyProfiles
            .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
            .Where(x => x.a.Status == (byte)BizSrt.Model.Status.Active && (queryInput.TransactionType == 0 || (x.c.TransactionType & queryInput.TransactionType) > 0))
            .Select(x => x.c);

        IQueryable<CompanyProfile> query = activeCompanies;

        if (queryInput.Category > 0)
        {
            query = query.Where(c => 
                (c.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == c.Category))
                ||
                (from cp in dbContext.CompanyProducts
                 join p in dbContext.Products on cp.Product equals p.Id
                 where (p.Type == 0 || (cp.UnlistedType == (byte)BizSrt.Model.Product.UnlistedType.Listed && p.Status == (byte)BizSrt.Model.Product.Status.Active)) &&
                       (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category))
                 select cp).Any(cp => cp.Company == c.Id)
            );
        }

        query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

        IQueryable<SearchItem> resultQuery;

        if (queryInput.Location > 0)
        {
            var coq = from c in query
                      join co in dbContext.CompanyOfficeLocation(queryInput.Location) on c.Id equals co.Id
                      select new { Company = c, co.Office };

            resultQuery = coq
                .OrderByDescending(x => x.Company.Created)
                .Select(x => new SearchItem { Id = x.Company.Id, Office = x.Office > 0 ? (int?)x.Office : null });
            
            query = coq.Select(x => x.Company);
        }
        else
        {
            resultQuery = query
                .OrderByDescending(c => c.Created)
                .Select(c => new SearchItem { Id = c.Id });
        }

        var total = await query.CountAsync();

        BizSrt.Model.Semantic.FacetName[]? facets = null;
        if (queryInput.InclFacets != null)
        {
            var pfq = await (from c in query
                             join pf in dbContext.CompanyFacets on c.Id equals pf.Company
                             join pfv in dbContext.CompanyFacetValues on pf.FacetValue equals pfv.Id
                             group pfv by new { pfv.Name, pfv.Id } into pfg
                             select new BizSrt.Data.Extensions.FacetExtensions.ValueCount { Name = pfg.Key.Name, Value = pfg.Key.Id, Count = pfg.Count() })
                            .ToArrayAsync();

            facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
        }

        var companies = await resultQuery
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 100)
            .ToArrayAsync();

        return new SearchOutput<SearchItem>
        {
            StartIndex = queryInput.StartIndex,
            Series = companies,
            TotalCount = total,
            Facets = facets
        };
    }

    private async Task<SearchOutput<SearchItem>> ExecuteCompanySearchSpAsync(SearchInput queryInput)
    {
        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;

        if (wasClosed)
            await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "CompanySearch";
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
            pLength.Value = queryInput.Length > 0 ? queryInput.Length : 100;
            command.Parameters.Add(pLength);

            var companies = new List<SearchItem>();
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                companies.Add(new SearchItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Office = reader.GetInt32(reader.GetOrdinal("Office")) > 0 ? reader.GetInt32(reader.GetOrdinal("Office")) : null,
                    Distance = queryInput.SearchNear != null ? (float)reader.GetDouble(reader.GetOrdinal("Distance")) : 0f
                });
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

    public async Task<IEnumerable<BizSrt.Model.Company.Preview>> ToPreviewAsync(SearchItem[] companies)
    {
        var ids = companies.Select(c => c.Id).Distinct().ToArray();
        var cachedProfiles = BizSrt.Api.Data.Cache.LegacyCache.CompanyProfiles[ids];

        var result = new List<BizSrt.Model.Company.Preview>();

        foreach (var req in companies)
        {
            var cp = cachedProfiles.FirstOrDefault(p => p != null && p.Id == req.Id);
            if (cp is null) continue;

            result.Add(cp.ToPreview(req.Office ?? 0));
        }

        return await Task.FromResult(result);
    }

    public async Task<SliceOutput<EntityId<int>>> GetCommunitiesAsync(int companyId, SliceInput sliceInput)
    {
        var query = dbContext.CompanyCommunities
            .Where(cc => cc.Company == companyId);

        var total = await query.CountAsync();
        var communities = await query
            .Skip(sliceInput.Index)
            .Take(sliceInput.Length)
            .Select(cc => new EntityId<int> { Id = cc.Community })
            .ToArrayAsync();

        return new SliceOutput<EntityId<int>>(communities, sliceInput.Index + communities.Length < total ? sliceInput.Index + communities.Length : -1);
    }

    public async Task<SliceOutput<SearchItem>> GetAffiliationsAsync(int companyId, SliceInput sliceInput)
    {
        var query = dbContext.CompanyAffiliations
            .Where(a => (a.From == companyId || (a.To == companyId && !a.Pending)) && !a.Declined);

        var total = await query.CountAsync();
        var affiliations = await query
            .OrderByDescending(a => a.Date)
            .Skip(sliceInput.Index)
            .Take(sliceInput.Length)
            .Select(a => new SearchItem { Id = a.From == companyId ? a.To : a.From })
            .ToArrayAsync();

        return new SliceOutput<SearchItem>(affiliations, sliceInput.Index + affiliations.Length < total ? sliceInput.Index + affiliations.Length : -1);
    }

    public async Task<QueryOutput<BizSrt.Model.EntityId<long>>> GetProductsAsync(int companyId, QueryInput queryInput)
    {
        var pq = dbContext.Products.GetFiltered(dbContext, queryInput);

        var query = dbContext.CompanyProducts
            .Join(pq, cp => cp.Product, p => p.Id, (cp, p) => new { cp, p })
            .Where(x => x.cp.Company == companyId && x.cp.UnlistedType == (byte)BizSrt.Model.Product.UnlistedType.Listed && x.p.Status == (byte)BizSrt.Model.Product.Status.Active);

        var total = await query.CountAsync();
        var products = await query
            .OrderByDescending(x => x.p.Created)
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 100)
            .Select(x => new BizSrt.Model.EntityId<long> { Id = x.cp.Product })
            .ToArrayAsync();

        return new QueryOutput<BizSrt.Model.EntityId<long>>
        {
            StartIndex = queryInput.StartIndex,
            Series = products,
            TotalCount = total
        };
    }

    public async Task<QueryOutput<BizSrt.Model.EntityId<long>>> GetProjectsAsync(int companyId, QueryInput queryInput)
    {
        var pq = dbContext.Projects.GetFiltered(dbContext, queryInput);

        var query = dbContext.CompanyProjects
            .Join(pq, cp => cp.Project, p => p.Id, (cp, p) => new { cp, p })
            .Where(x => x.cp.Company == companyId && x.cp.UnlistedType == (byte)BizSrt.Model.Product.UnlistedType.Listed && x.p.Status == (byte)BizSrt.Model.Product.Status.Active);

        var total = await query.CountAsync();
        var projects = await query
            .OrderByDescending(x => x.p.Created)
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 100)
            .Select(x => new BizSrt.Model.EntityId<long> { Id = x.cp.Project })
            .ToArrayAsync();

        return new QueryOutput<BizSrt.Model.EntityId<long>>
        {
            StartIndex = queryInput.StartIndex,
            Series = projects,
            TotalCount = total
        };
    }

    public async Task<QueryOutput<BizSrt.Model.EntityId<long>>> GetJobsAsync(int companyId, short department, QueryInput queryInput)
    {
        var pq = dbContext.Products.GetFiltered(dbContext, queryInput);

        var query = dbContext.Jobs
            .Join(pq, j => j.Id, p => p.Id, (j, p) => new { j, p })
            .Where(x => x.j.Company == companyId && x.p.Status == (byte)BizSrt.Model.Product.Status.Active);

        if (department > 0)
        {
            query = query.Where(x => x.j.Department == department);
        }

        var total = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(x => x.p.Created)
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 100)
            .Select(x => new BizSrt.Model.EntityId<long> { Id = x.j.Id })
            .ToArrayAsync();

        return new QueryOutput<BizSrt.Model.EntityId<long>>
        {
            StartIndex = queryInput.StartIndex,
            Series = jobs,
            TotalCount = total
        };
    }

    public async Task<IEnumerable<BizSrt.Model.Promotion.Preview>> GetPromotionsAsync(int companyId)
    {
        var promotions = await dbContext.Promotions
            .Include(p => p.CommunityNavigation)
            .Where(p => p.Company == companyId && p.Active)
            .AsNoTracking()
            .Select(p => new BizSrt.Model.Promotion.Preview
            {
                Id = p.Id,
                Name = p.CommunityNavigation.Name,
                Text = p.CommunityNavigation.Text
            })
            .ToListAsync();

        return promotions;
    }

    public async Task<BizSrt.Model.Account?> GetInfoAsync(int id)
    {
        var company = await dbContext.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (company is null) return null;

        return new BizSrt.Model.Account
        {
            Id = company.Id,
            Name = company.Name,
            AccountType = BizSrt.Model.AccountType.Company
        };
    }

    public async Task<BizSrt.Model.Product.Profile?> GetProductProfileAsync(int companyId, long productId)
    {
        var cp = await dbContext.CompanyProducts
            .Include(x => x.ProductNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Company == companyId && x.Product == productId);

        if (cp is null) return null;

        return new BizSrt.Model.Product.Profile
        {
            Id = cp.Product,
            Title = cp.ProductNavigation.Title ?? "",
            RichText = cp.ProductNavigation.RichText,
            Text = cp.ProductNavigation.Text,
            WebUrl = cp.ProductNavigation.WebUrl,
            Status = (BizSrt.Model.Product.Status)cp.ProductNavigation.Status,
            Updated = cp.ProductNavigation.Updated
        };
    }

    public async Task<BizSrt.Model.Job.Profile?> GetJobProfileAsync(int companyId, long jobId)
    {
        var job = await dbContext.Jobs
            .Include(x => x.ProductNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Company == companyId && x.Id == jobId);

        if (job is null) return null;

        return new BizSrt.Model.Job.Profile
        {
            Id = job.Id,
            Title = job.ProductNavigation.Title ?? "",
            RichText = job.ProductNavigation.RichText,
            Text = job.ProductNavigation.Text,
            StartDate = job.StartDate,
            Duration = job.Duration,
            WebUrl = job.ProductNavigation.WebUrl,
            Status = (BizSrt.Model.Product.Status)job.ProductNavigation.Status,
            Updated = job.ProductNavigation.Updated
        };
    }

    public async Task<BizSrt.Model.Project.Profile?> GetProjectProfileAsync(int companyId, long projectId)
    {
        var cp = await dbContext.CompanyProjects
            .Include(x => x.ProjectNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Company == companyId && x.Project == projectId);

        if (cp is null) return null;

        return new BizSrt.Model.Project.Profile
        {
            Id = cp.Project,
            Title = cp.ProjectNavigation.Title,
            RichText = cp.ProjectNavigation.RichText ?? "",
            Text = cp.ProjectNavigation.Text ?? "",
            TenderType = cp.ProjectNavigation.TenderType,
            Status = (BizSrt.Model.Product.Status)cp.ProjectNavigation.Status,
            Updated = cp.ProjectNavigation.Updated
        };
    }
}
