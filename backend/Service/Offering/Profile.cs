using Microsoft.AspNetCore.Mvc;
using BizSrt.Api.Data.Company;
using BizSrt.Model.Offering;
using System.Text.Json;

namespace BizSrt.Api.Service.Offering;

public static class OfferingEndpoints
{
    public static void MapOfferingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/offering").WithTags("Offering");

        group.MapGet("/profile/search", async ([FromQuery] string queryInput, ICompanyOfferingService offeringService) =>
        {
            var input = JsonSerializer.Deserialize<SearchInput>(queryInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SearchInput();
            var result = await offeringService.SearchAsync(input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getFeatured", async ([FromQuery] string sliceInput, ICompanyOfferingService offeringService) =>
        {
            var input = JsonSerializer.Deserialize<BizSrt.Model.List.DirectorySliceInput<long>>(sliceInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (input == null) return Results.BadRequest();
            var result = await offeringService.GetFeaturedAsync(input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/toPreview", async ([FromQuery] string offerings, [FromQuery] string? options, ICompanyOfferingService offeringService) =>
        {
            var inputOfferings = JsonSerializer.Deserialize<SearchItem[]>(offerings, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (inputOfferings == null) return Results.BadRequest();
            
            var optionsDict = !string.IsNullOrEmpty(options) 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(options, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                : new Dictionary<string, object>();
                
            var result = await offeringService.ToPreviewAsync(inputOfferings, optionsDict);
            return Results.Ok(result);
        });
    }
}
