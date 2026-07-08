using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Data.Extensions;
using BizSrt.Api.Model;
using BizSrt.Api.Model.List;
using BizSrt.Api.Model.Product;
using System.Data;
using System.Linq;

namespace BizSrt.Api.Data.Company;

public interface ICompanyProductService
{
    Task<SearchOutput<SearchItem>> SearchAsync(BizSrt.Api.Model.Product.SearchInput queryInput);
    Task<SliceOutput<SearchItem>> GetFeaturedAsync(DirectorySliceInput<long> sliceInput);
    Task<SliceOutput<SearchItem>> GetFeaturedAsync(int company, SliceInput sliceInput);
    Task<Preview[]> ToPreviewAsync(SearchItem[] products, Dictionary<string, object> options);
}

public class CompanyProductService : ICompanyProductService
{
    private readonly AppDbContext dbContext;

    public CompanyProductService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<SliceOutput<SearchItem>> GetFeaturedAsync(DirectorySliceInput<long> sliceInput)
    {
        var products = new List<long>();
        long product;
        var cached = BizSrt.Api.Data.Cache.LegacyCache.FeaturedProducts[new Tuple<short, int>(sliceInput.Category, sliceInput.Location), sliceInput.Index == 0 && sliceInput.Length > 1];
        var index = sliceInput.Index;
        if (sliceInput.Skip == null || sliceInput.Skip.Length < cached.Length)
        {
            while (products.Count < sliceInput.Length && index < cached.Length)
            {
                product = cached[index];
                if (sliceInput.Skip == null || !sliceInput.Skip.Contains(product))
                    products.Add(product);
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
        return Task.FromResult(new SliceOutput<SearchItem>(products.Select(p => new SearchItem { Id = p }).ToArray(), index));
    }

    public Task<SliceOutput<SearchItem>> GetFeaturedAsync(int company, SliceInput sliceInput)
    {
        var products = new List<long>();
        var cached = BizSrt.Api.Data.Cache.LegacyCache.CompanyProfiles[company]?.Products ?? Array.Empty<long>();
        var index = sliceInput.Index;
        while (products.Count < sliceInput.Length && index < cached.Length)
        {
            products.Add(cached[index]);
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
        return Task.FromResult(new SliceOutput<SearchItem>(products.Select(p => new SearchItem { Id = p }).ToArray(), index));
    }

    public Task<Preview[]> ToPreviewAsync(SearchItem[] products, Dictionary<string, object> options)
    {
        if (products != null && products.Length > 0)
        {
            var productIds = products.Select(p => p.Id).ToArray();
            var cachedProducts = BizSrt.Api.Data.Cache.LegacyCache.CompanyProducts[productIds, false];
            
            if (options?.ContainsKey("company") == true)
            {
                var companies = BizSrt.Api.Data.Cache.LegacyCache.CompanyProfiles[cachedProducts.Select(p => p.CompanyId).Distinct().ToArray()]
                    .ToDictionary(c => c.Id, c => new BizSrt.Api.Model.Account 
                    { 
                        AccountType = BizSrt.Api.Model.AccountType.Company,
                        Id = c.Id, 
                        Name = c.Name, 
                        Image = new Image<int> { Entity = ImageEntity.Company, ImageId = c.ImageId, MaxImageSize = c.ImageSize } 
                    });

                return Task.FromResult(cachedProducts.Select(p => 
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
                return Task.FromResult(cachedProducts.Select(p => p.ToPreview()).ToArray());
            }
        }
        else
            throw new InvalidOperationException("Invalid input");
    }

    public async Task<SearchOutput<SearchItem>> SearchAsync(BizSrt.Api.Model.Product.SearchInput queryInput)
    {
        if (!string.IsNullOrWhiteSpace(queryInput.SearchQuery) || queryInput.SearchNear != null)
        {
            return await ExecuteProductSearchSpAsync(queryInput);
        }

        var activeProducts = dbContext.Products.Where(p => p.Status == 2);

        IQueryable<Product> query = activeProducts;

        if (queryInput.Category > 0)
        {
            var cpq = queryInput.Location == 0 
                ? dbContext.CompanyProducts 
                : dbContext.CompanyProducts.Where(cp => dbContext.CompanyOfficeLocation(queryInput.Location).Any(co => co.Id == cp.Company));

            query = from p in query
                    join cp in cpq on p.Id equals cp.Product
                    where cp.UnlistedType == 1 &&
                          (cp.Category == queryInput.Category || dbContext.Categories_Unwound.Any(cu => cu.Parent == queryInput.Category && cu.Child == cp.Category)) &&
                          (queryInput.ProductType == 0 || (p.Type & queryInput.ProductType) > 0)
                    select p;
        }
        else
        {
            throw new InvalidOperationException("Invalid input for product search without category or text search.");
        }

        query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

        // Materialize all matching IDs in a single round-trip to avoid running the expensive
        // correlated subquery 3 times (count + facets + page).
        // Project only the Id so SQL Server can use covering indexes.
        var allMatchingIds = await query
            .OrderByDescending(p => p.Created)
            .Select(p => p.Id)
            .ToArrayAsync();

        var total = allMatchingIds.Length;
        var pageIds = allMatchingIds
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 20)
            .ToArray();

        var products = pageIds.Select(id => new SearchItem { Id = id }).ToArray();

        BizSrt.Api.Model.Semantic.FacetName[]? facets = null;
        if (queryInput.InclFacets != null)
        {
            // Facet aggregation over the materialized ID set — no re-scan of the base query
            var pfq = await (from p in dbContext.Products
                             where allMatchingIds.Contains(p.Id)
                             join pf in dbContext.CompanyProductFacets on p.Id equals pf.Product
                             join pfv in dbContext.CompanyProductFacetValues on pf.FacetValue equals pfv.Id
                             group pfv by new { pfv.Name, pfv.Id } into pfg
                             select new BizSrt.Api.Data.Extensions.FacetExtensions.ValueCount { Name = pfg.Key.Name, Value = pfg.Key.Id, Count = pfg.Count() })
                            .ToArrayAsync();

            facets = BizSrt.Api.Data.Extensions.FacetExtensions.GetFacets(pfq, queryInput.InclFacets, total);
        }

        return new SearchOutput<SearchItem>
        {
            StartIndex = queryInput.StartIndex,
            Series = products,
            TotalCount = total,
            Facets = facets
        };
    }

    private async Task<SearchOutput<SearchItem>> ExecuteProductSearchSpAsync(BizSrt.Api.Model.Product.SearchInput queryInput)
    {
        var connection = dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;

        if (wasClosed)
            await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "ProductSearch";
            command.CommandType = CommandType.StoredProcedure;

            var pProductType = command.CreateParameter();
            pProductType.ParameterName = "@ProductType";
            pProductType.Value = (short)queryInput.ProductType;
            command.Parameters.Add(pProductType);

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

            var products = new List<BizSrt.Api.Model.Product.SearchItem>();
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new BizSrt.Api.Model.Product.SearchItem
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    Distance = queryInput.SearchNear != null ? (float)reader.GetDouble(reader.GetOrdinal("Distance")) : 0f
                });
            }

            return new SearchOutput<SearchItem>
            {
                StartIndex = queryInput.StartIndex,
                Series = products.Select(p => new SearchItem { Id = p.Id }).ToArray(),
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
