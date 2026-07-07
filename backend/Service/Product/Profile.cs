using Microsoft.AspNetCore.Mvc;
using BizSrt.Api.Data.Company;
using BizSrt.Api.Model.Product;
using System.Text.Json;

namespace BizSrt.Api.Service.Product;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/product").WithTags("Product");

        group.MapGet("/profile/search", async ([FromQuery] string queryInput, ICompanyProductService productService) =>
        {
            var input = JsonSerializer.Deserialize<SearchInput>(queryInput) ?? new SearchInput();
            var result = await productService.SearchAsync(input);
            return Results.Ok(result);
        });
    }
}
