using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Data.Entities;
using BizSrt.Data.Extensions;
using BizSrt.Model;
using BizSrt.Model.List;
using BizSrt.Model.Offering;
using System.Data;
using System.Linq;

namespace BizSrt.Api.Data.Company;

public interface ICompanyOfferingService
{
    Task<SearchOutput<SearchItem>> SearchAsync(BizSrt.Model.Offering.SearchInput queryInput);
    Task<SliceOutput<SearchItem>> GetFeaturedAsync(DirectorySliceInput<long> sliceInput);
    Task<SliceOutput<SearchItem>> GetFeaturedAsync(int company, SliceInput sliceInput);
    Task<Preview[]> ToPreviewAsync(SearchItem[] offerings, Dictionary<string, object> options);
}

public class CompanyOfferingService : ICompanyOfferingService
{
    private readonly AppDbContext dbContext;

    public CompanyOfferingService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<SliceOutput<SearchItem>> GetFeaturedAsync(DirectorySliceInput<long> sliceInput)
    {
        var offerings = new List<long>();
        long offering;
        var cached = BizSrt.Api.Data.Cache.LegacyCache.FeaturedOfferings[new Tuple<short, int>(sliceInput.Category, sliceInput.Location), sliceInput.Index == 0 && sliceInput.Length > 1];
        var index = sliceInput.Index;
        if (sliceInput.Skip == null || sliceInput.Skip.Length < cached.Length)
        {
            while (offerings.Count < sliceInput.Length && index < cached.Length)
            {
                offering = cached[index];
                if (sliceInput.Skip == null || !sliceInput.Skip.Contains(offering))
                    offerings.Add(offering);
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
        return Task.FromResult(new SliceOutput<SearchItem>(offerings.Select(p => new SearchItem { Id = p }).ToArray(), index));
    }

    public Task<SliceOutput<SearchItem>> GetFeaturedAsync(int company, SliceInput sliceInput)
    {
        var offerings = new List<long>();
        var cached = BizSrt.Api.Data.Cache.LegacyCache.CompanyProfiles[company]?.Offerings ?? Array.Empty<long>();
        var index = sliceInput.Index;
        while (offerings.Count < sliceInput.Length && index < cached.Length)
        {
            offerings.Add(cached[index]);
            if (++index >= cached.Length)
            {
                if (cached.Length <= sliceInput.Length)
                {
                    index = -1;
                    break;
                }
                else
                    index = 0;
            }
        }
        return Task.FromResult(new SliceOutput<SearchItem>(offerings.Select(p => new SearchItem { Id = p }).ToArray(), index));
    }

    public Task<Preview[]> ToPreviewAsync(SearchItem[] offerings, Dictionary<string, object> options)
    {
        if (offerings != null && offerings.Length > 0)
        {
            var offeringIds = offerings.Select(p => p.Id).ToArray();
            var cachedOfferings = BizSrt.Api.Data.Cache.LegacyCache.CompanyOfferings[offeringIds, false];
            
            if (options?.ContainsKey("company") == true)
            {
                var companies = BizSrt.Api.Data.Cache.LegacyCache.CompanyProfiles[cachedOfferings.Select(p => p.CompanyId).Distinct().ToArray()]
                    .ToDictionary(c => c.Id, c => new BizSrt.Model.Account 
                    { 
                        AccountType = BizSrt.Model.AccountType.Company,
                        Id = c.Id, 
                        Name = c.Name, 
                        Image = new Image<int> { Entity = ImageEntity.Company, ImageId = c.ImageId, MaxImageSize = c.ImageSize } 
                    });

                return Task.FromResult(cachedOfferings.Select(p => 
                {
                    var prvw = p.ToPreview();
                    if (companies.TryGetValue(p.CompanyId, out var companyAccount))
                    {
                        prvw.Company = companyAccount;
                    }
                    return prvw;
                }).ToArray());
            }
            else
            {
                return Task.FromResult(cachedOfferings.Select(p => p.ToPreview()).ToArray());
            }
        }
        else
            throw new InvalidOperationException("Invalid input");
    }

    public async Task<SearchOutput<SearchItem>> SearchAsync(BizSrt.Model.Offering.SearchInput queryInput)
    {
        if (!string.IsNullOrWhiteSpace(queryInput.SearchQuery) || queryInput.SearchNear != null)
        {
            return await ExecuteOfferingSearchSpAsync(queryInput);
        }

        var activeOfferings = dbContext.Offerings.Where(p => p.Type != 0 && p.Status == (byte)BizSrt.Model.Offering.Status.Active);

        IQueryable<Offering> query = activeOfferings;

        if (queryInput.Category > 0)
        {
            var targetCat = (short)queryInput.Category;
            var categoryIdsQuery = dbContext.Categories_Unwound
                .Where(cu => cu.Parent == targetCat)
                .Select(cu => cu.Child);

            query = from p in query
                    join cp in dbContext.CompanyOfferings on p.Id equals cp.Offering
                    where cp.UnlistedType == (byte)BizSrt.Model.Offering.UnlistedType.Listed &&
                          (cp.Category == targetCat || categoryIdsQuery.Contains(cp.Category)) &&
                          (queryInput.OfferingType == 0 || (p.Type & queryInput.OfferingType) > 0)
                    select p;
        }
        else
        {
            throw new InvalidOperationException("Invalid input for offering search without category or text search.");
        }

        query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

        IQueryable<long>? locationOfferingIds = null;
        if (queryInput.Location > 0)
        {
            var childLocations = dbContext.Locations_Unwound
                .Where(lu => lu.Parent == queryInput.Location)
                .Select(lu => lu.Child);

            locationOfferingIds = dbContext.CompanyOfferings
                .Where(cp => dbContext.CompanyOffices
                    .Where(co => co.Location == queryInput.Location || childLocations.Contains(co.Location))
                    .Any(co => co.Company == cp.Company))
                .Select(cp => cp.Offering)
                .Distinct();
        }

        long[] allMatchingIds;
        if (locationOfferingIds != null)
        {
            // Two-query split: category and location run as two independent SQL queries.
            // Intersect in C# memory using HashSet to prevent catastrophic plan degradation.
            var categoryMatches = await query.Select(p => new { p.Id, p.Created }).ToArrayAsync();
            var locationMatches = await locationOfferingIds.ToArrayAsync();
            var locationSet = new HashSet<long>(locationMatches);
            
            allMatchingIds = categoryMatches
                .Where(p => locationSet.Contains(p.Id))
                .OrderByDescending(p => p.Created)
                .Select(p => p.Id)
                .ToArray();
        }
        else
        {
            allMatchingIds = await query
                .OrderByDescending(p => p.Created)
                .Select(p => p.Id)
                .ToArrayAsync();
        }

        var total = allMatchingIds.Length;
        var pageIds = allMatchingIds
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 20)
            .ToArray();

        var offerings = pageIds.Select(id => new SearchItem { Id = id }).ToArray();

        BizSrt.Model.Semantic.FacetName[]? facets = null;
        if (queryInput.InclFacets != null)
        {
            var idsJson = System.Text.Json.JsonSerializer.Serialize(allMatchingIds);
            var pfq = await dbContext.Database
                .SqlQueryRaw<BizSrt.Data.Extensions.FacetExtensions.ValueCount>(@"
                    SELECT pfv.Name, pfv.Id AS Value, COUNT(*) AS Count
                    FROM CompanyOfferingFacets pf
                    INNER JOIN OPENJSON({0}) ids ON pf.Offering = ids.value
                    INNER JOIN CompanyOfferingFacetValues pfv ON pf.FacetValue = pfv.Id
                    GROUP BY pfv.Name, pfv.Id", idsJson)
                .ToArrayAsync();

            facets = BizSrt.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
        }

        return new SearchOutput<SearchItem>
        {
            StartIndex = queryInput.StartIndex,
            Series = offerings,
            TotalCount = total,
            Facets = facets
        };
    }

    private async Task<SearchOutput<SearchItem>> ExecuteOfferingSearchSpAsync(BizSrt.Model.Offering.SearchInput queryInput)
    {
        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;

        if (wasClosed)
            await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "OfferingSearch";
            command.CommandType = CommandType.StoredProcedure;

            var pOfferingType = command.CreateParameter();
            pOfferingType.ParameterName = "@OfferingType";
            pOfferingType.Value = (short)queryInput.OfferingType;
            command.Parameters.Add(pOfferingType);

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
            pLength.DbType = DbType.Int32;
            pLength.Direction = ParameterDirection.InputOutput;
            pLength.Value = queryInput.Length > 0 ? queryInput.Length : 20;
            command.Parameters.Add(pLength);

            var offerings = new List<BizSrt.Model.Offering.SearchItem>();
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    offerings.Add(new BizSrt.Model.Offering.SearchItem
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),
                        Distance = queryInput.SearchNear != null ? (float)reader.GetDouble(reader.GetOrdinal("Distance")) : 0f
                    });
                }
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = offerings.Select(p => new SearchItem { Id = p.Id }).ToArray(),
                TotalCount = pLength.Value != DBNull.Value ? Convert.ToInt32(pLength.Value) : 0
            };
        }
        finally
        {
            if (wasClosed)
                await connection.CloseAsync();
        }
    }
}
