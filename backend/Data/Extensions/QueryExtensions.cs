using System.Linq;
using BizSrt.Api.Data;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Model;
using BizSrt.Api.Model.List;

namespace BizSrt.Api.Data.Extensions;

public static class QueryExtensions
{
    public static IQueryable<Product> GetFiltered(this IQueryable<Product> query, AppDbContext dbContext, QueryInput queryInput)
    {
        if (queryInput != null && !string.IsNullOrWhiteSpace(queryInput.SearchQuery))
        {
            query = from p in query
                    join pt in dbContext.ProductTextSearch(queryInput.SearchQuery) on p.Id equals pt.Id
                    select p;
        }

        // TODO: Port Facet logic when modernized Facet models are available
        return query;
    }

    public static IQueryable<Project> GetFiltered(this IQueryable<Project> query, AppDbContext dbContext, QueryInput queryInput)
    {
        if (queryInput != null && !string.IsNullOrWhiteSpace(queryInput.SearchQuery))
        {
            query = from p in query
                    join pt in dbContext.ProjectTextSearch(queryInput.SearchQuery) on p.Id equals pt.Id
                    select p;
        }

        // TODO: Port Facet logic when modernized Facet models are available
        return query;
    }
}
