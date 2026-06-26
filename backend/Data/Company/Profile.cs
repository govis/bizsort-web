using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Model.Company;
using BizSrt.Api.Model;
using BizSrt.Api.Model.List;
using BizSrt.Api.Data.Cache.Company;

namespace BizSrt.Api.Data.Company;

public interface ICompanyService
{
    Task<Profile?> ViewAsync(int id);
    Task<SliceOutput<SearchItem>> GetFeaturedAsync(DirectorySliceInput<int> sliceInput);
    Task<QueryOutput<SearchItem>> SearchAsync(SearchInput queryInput);
    Task<IEnumerable<BizSrt.Api.Model.Company.Preview>> ToPreviewAsync(SearchItem[] companies);
    Task<SliceOutput<EntityId<int>>> GetCommunitiesAsync(int companyId, SliceInput sliceInput);
    Task<SliceOutput<SearchItem>> GetAffiliationsAsync(int companyId, SliceInput sliceInput);
    Task<QueryOutput<BizSrt.Api.Model.EntityId<long>>> GetProductsAsync(int companyId, QueryInput queryInput);
    Task<QueryOutput<BizSrt.Api.Model.EntityId<long>>> GetProjectsAsync(int companyId, QueryInput queryInput);
    Task<QueryOutput<BizSrt.Api.Model.EntityId<long>>> GetJobsAsync(int companyId, short department, QueryInput queryInput);
    Task<IEnumerable<BizSrt.Api.Model.Promotion.Preview>> GetPromotionsAsync(int companyId);
    Task<BizSrt.Api.Model.Account?> GetInfoAsync(int id);
    Task<BizSrt.Api.Model.Product.Profile?> GetProductProfileAsync(int companyId, long productId);
    Task<BizSrt.Api.Model.Job.Profile?> GetJobProfileAsync(int companyId, long jobId);
    Task<BizSrt.Api.Model.Project.Profile?> GetProjectProfileAsync(int companyId, long projectId);
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

        var offices = company.Offices.OrderBy(o => o.Order).Select(o => new BizSrt.Api.Model.Company.Office
        {
            Id = o.Id,
            Name = o.Name ?? "",
            Phone = o.Phone ?? "",
            Phone1 = o.Phone1,
            Fax = o.Fax,
            Location = new BizSrt.Api.Model.Location 
            { 
                Address = $"{o.StreetNumber} {o.Address1}, {o.PostalCode}".Trim().Trim(','),
                GeoLocation = o.GeoLocation is NetTopologySuite.Geometries.Point p 
                    ? new BizSrt.Api.Model.Geolocation { Lat = p.Y, Lng = p.X } 
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
            Category = company.Category > 0 ? new BizSrt.Api.Model.Category { Id = company.Category, Name = categoryName ?? "" } : null,
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

    public async Task<QueryOutput<SearchItem>> SearchAsync(SearchInput queryInput)
    {
        var query = dbContext.CompanyProfiles
            .Join(dbContext.Accounts, c => c.Id, a => a.Id, (c, a) => new { c, a })
            .Where(x => x.a.Status == 2);

        if (queryInput.Category > 0)
        {
            query = query.Where(x => x.c.Category == queryInput.Category);
        }

        if (!string.IsNullOrWhiteSpace(queryInput.SearchQuery))
        {
            query = query.Where(x => x.c.Name.Contains(queryInput.SearchQuery));
        }

        var total = await query.CountAsync();
        var companies = await query
            .OrderByDescending(x => x.c.Created)
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 20)
            .Select(x => new SearchItem { Id = x.c.Id })
            .ToArrayAsync();

        return new QueryOutput<SearchItem>
        {
            StartIndex = queryInput.StartIndex,
            Series = companies,
            TotalCount = total
        };
    }

    public async Task<IEnumerable<BizSrt.Api.Model.Company.Preview>> ToPreviewAsync(SearchItem[] companies)
    {
        var ids = companies.Select(c => c.Id).Distinct().ToArray();
        var cachedProfiles = BizSrt.Api.Data.Cache.LegacyCache.CompanyProfiles[ids];

        var result = new List<BizSrt.Api.Model.Company.Preview>();

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

    public async Task<QueryOutput<BizSrt.Api.Model.EntityId<long>>> GetProductsAsync(int companyId, QueryInput queryInput)
    {
        var query = dbContext.CompanyProducts
            .Join(dbContext.Products, cp => cp.Product, p => p.Id, (cp, p) => new { cp, p })
            .Where(x => x.cp.Company == companyId && x.p.Status == 2);

        var total = await query.CountAsync();
        var products = await query
            .OrderByDescending(x => x.p.Created)
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 20)
            .Select(x => new BizSrt.Api.Model.EntityId<long> { Id = x.cp.Product })
            .ToArrayAsync();

        return new QueryOutput<BizSrt.Api.Model.EntityId<long>>
        {
            StartIndex = queryInput.StartIndex,
            Series = products,
            TotalCount = total
        };
    }

    public async Task<QueryOutput<BizSrt.Api.Model.EntityId<long>>> GetProjectsAsync(int companyId, QueryInput queryInput)
    {
        var query = dbContext.CompanyProjects
            .Join(dbContext.Projects, cp => cp.Project, p => p.Id, (cp, p) => new { cp, p })
            .Where(x => x.cp.Company == companyId && x.p.Status == 2);

        var total = await query.CountAsync();
        var projects = await query
            .OrderByDescending(x => x.p.Created)
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 20)
            .Select(x => new BizSrt.Api.Model.EntityId<long> { Id = x.cp.Project })
            .ToArrayAsync();

        return new QueryOutput<BizSrt.Api.Model.EntityId<long>>
        {
            StartIndex = queryInput.StartIndex,
            Series = projects,
            TotalCount = total
        };
    }

    public async Task<QueryOutput<BizSrt.Api.Model.EntityId<long>>> GetJobsAsync(int companyId, short department, QueryInput queryInput)
    {
        var query = dbContext.Jobs
            .Join(dbContext.Products, j => j.Id, p => p.Id, (j, p) => new { j, p })
            .Where(x => x.j.Company == companyId && x.p.Status == 2);

        if (department > 0)
        {
            query = query.Where(x => x.j.Department == department);
        }

        var total = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(x => x.p.Created)
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 20)
            .Select(x => new BizSrt.Api.Model.EntityId<long> { Id = x.j.Id })
            .ToArrayAsync();

        return new QueryOutput<BizSrt.Api.Model.EntityId<long>>
        {
            StartIndex = queryInput.StartIndex,
            Series = jobs,
            TotalCount = total
        };
    }

    public async Task<IEnumerable<BizSrt.Api.Model.Promotion.Preview>> GetPromotionsAsync(int companyId)
    {
        var promotions = await dbContext.Promotions
            .Include(p => p.CommunityNavigation)
            .Where(p => p.Company == companyId && p.Active)
            .AsNoTracking()
            .Select(p => new BizSrt.Api.Model.Promotion.Preview
            {
                Id = p.Id,
                Name = p.CommunityNavigation.Name,
                Text = p.CommunityNavigation.Text
            })
            .ToListAsync();

        return promotions;
    }

    public async Task<BizSrt.Api.Model.Account?> GetInfoAsync(int id)
    {
        var company = await dbContext.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (company is null) return null;

        return new BizSrt.Api.Model.Account
        {
            Id = company.Id,
            Name = company.Name,
            AccountType = BizSrt.Api.Model.AccountType.Company
        };
    }

    public async Task<BizSrt.Api.Model.Product.Profile?> GetProductProfileAsync(int companyId, long productId)
    {
        var cp = await dbContext.CompanyProducts
            .Include(x => x.ProductNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Company == companyId && x.Product == productId);

        if (cp is null) return null;

        return new BizSrt.Api.Model.Product.Profile
        {
            Id = cp.Product,
            Title = cp.ProductNavigation.Title ?? "",
            RichText = cp.ProductNavigation.RichText,
            Text = cp.ProductNavigation.Text,
            WebUrl = cp.ProductNavigation.WebUrl,
            Status = (BizSrt.Api.Model.Status)cp.ProductNavigation.Status,
            Updated = cp.ProductNavigation.Updated
        };
    }

    public async Task<BizSrt.Api.Model.Job.Profile?> GetJobProfileAsync(int companyId, long jobId)
    {
        var job = await dbContext.Jobs
            .Include(x => x.ProductNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Company == companyId && x.Id == jobId);

        if (job is null) return null;

        return new BizSrt.Api.Model.Job.Profile
        {
            Id = job.Id,
            Title = job.ProductNavigation.Title ?? "",
            RichText = job.ProductNavigation.RichText,
            Text = job.ProductNavigation.Text,
            StartDate = job.StartDate,
            Duration = job.Duration,
            WebUrl = job.ProductNavigation.WebUrl,
            Status = (BizSrt.Api.Model.Status)job.ProductNavigation.Status,
            Updated = job.ProductNavigation.Updated
        };
    }

    public async Task<BizSrt.Api.Model.Project.Profile?> GetProjectProfileAsync(int companyId, long projectId)
    {
        var cp = await dbContext.CompanyProjects
            .Include(x => x.ProjectNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Company == companyId && x.Project == projectId);

        if (cp is null) return null;

        return new BizSrt.Api.Model.Project.Profile
        {
            Id = cp.Project,
            Title = cp.ProjectNavigation.Title,
            RichText = cp.ProjectNavigation.RichText ?? "",
            Text = cp.ProjectNavigation.Text ?? "",
            TenderType = cp.ProjectNavigation.TenderType,
            Status = (BizSrt.Api.Model.Status)cp.ProjectNavigation.Status,
            Updated = cp.ProjectNavigation.Updated
        };
    }
}
