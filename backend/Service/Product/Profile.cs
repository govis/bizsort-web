using Microsoft.AspNetCore.Mvc;
using BizSrt.Data.Company;
using BizSrt.Model.Product;
using System.Text.Json;

namespace BizSrt.Api.Service.Product;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/product").WithTags("Product");

        group.MapGet("/profile/search", async ([FromQuery] string queryInput, ICompanyProductService productService) =>
        {
            var input = JsonSerializer.Deserialize<SearchInput>(queryInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SearchInput();
            var result = await productService.SearchAsync(input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getFeatured", async ([FromQuery] string sliceInput, ICompanyProductService productService) =>
        {
            var input = JsonSerializer.Deserialize<BizSrt.Model.List.DirectorySliceInput<long>>(sliceInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (input == null) return Results.BadRequest();
            var result = await productService.GetFeaturedAsync(input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/toPreview", async ([FromQuery] string products, [FromQuery] string? options, ICompanyProductService productService) =>
        {
            var inputProducts = JsonSerializer.Deserialize<SearchItem[]>(products, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (inputProducts == null) return Results.BadRequest();
            
            var optionsDict = !string.IsNullOrEmpty(options) 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(options, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                : new Dictionary<string, object>();
                
            var result = await productService.ToPreviewAsync(inputProducts, optionsDict);
            return Results.Ok(result);
        });
    }
}
