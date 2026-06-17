using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data;
using BizSrt.Api.Services;
using BizSrt.Api.Models.Company;
using BizSrt.Api.Models.Legacy.List;
using System.Text.Json;

namespace BizSrt.Api.Endpoints;

public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/company").WithTags("Company");

        group.MapGet("/profile/view", async ([FromQuery] int company, [FromQuery] string? options, ICompanyService companyService) =>
        {
            var profile = await companyService.GetCompanyProfileAsync(company);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        })
        .WithName("ViewCompanyProfile")
        .WithOpenApi();

        group.MapGet("/profile/getFeatured", async ([FromQuery] string sliceInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<SliceInput>(sliceInput) ?? new SliceInput();
            var result = await companyService.GetFeaturedCompaniesAsync(input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/search", async ([FromQuery] string queryInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<SearchInput>(queryInput) ?? new SearchInput();
            var result = await companyService.SearchCompaniesAsync(input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getCommunities", async ([FromQuery] int company, [FromQuery] string sliceInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<SliceInput>(sliceInput) ?? new SliceInput();
            var result = await companyService.GetCompanyCommunitiesAsync(company, input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getAffiliations", async ([FromQuery] int company, [FromQuery] string sliceInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<SliceInput>(sliceInput) ?? new SliceInput();
            var result = await companyService.GetCompanyAffiliationsAsync(company, input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getProducts", async ([FromQuery] int company, [FromQuery] string queryInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<QueryInput>(queryInput) ?? new QueryInput();
            var result = await companyService.GetCompanyProductsAsync(company, input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getProjects", async ([FromQuery] int company, [FromQuery] string queryInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<QueryInput>(queryInput) ?? new QueryInput();
            var result = await companyService.GetCompanyProjectsAsync(company, input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getJobs", async ([FromQuery] int company, [FromQuery] short department, [FromQuery] string queryInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<QueryInput>(queryInput) ?? new QueryInput();
            var result = await companyService.GetCompanyJobsAsync(company, department, input);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getPromotions", async ([FromQuery] int company, ICompanyService companyService) =>
        {
            var result = await companyService.GetCompanyPromotionsAsync(company);
            return Results.Ok(result);
        });

        group.MapGet("/profile/getInfo", async ([FromQuery] int company, ICompanyService companyService) =>
        {
            var info = await companyService.GetCompanyInfoAsync(company);
            return info is not null ? Results.Ok(info) : Results.NotFound();
        });

        group.MapGet("/profile/newProfiles", async ([FromQuery] string queryInput, ICompanyService companyService) =>
        {
            var input = JsonSerializer.Deserialize<SearchInput>(queryInput) ?? new SearchInput();
            var result = await companyService.SearchCompaniesAsync(input); 
            return Results.Ok(result);
        });

        group.MapGet("/product/getFeatured", async ([FromQuery] int company, [FromQuery] string sliceInput, ICompanyService companyService) =>
        {
             var input = JsonSerializer.Deserialize<SliceInput>(sliceInput) ?? new SliceInput();
             var result = await companyService.GetCompanyProductsAsync(company, new QueryInput { StartIndex = input.Index, Length = input.Length });
             return Results.Ok(new SliceOutput<BizSrt.Api.Models.Legacy.EntityId<long>>(result.Series, result.StartIndex + result.Series.Length < result.TotalCount ? result.StartIndex + result.Series.Length : -1));
        });

        group.MapGet("/product/view", async ([FromQuery] int company, [FromQuery] long product, [FromQuery] string? options, ICompanyService companyService) =>
        {
             var profile = await companyService.GetProductProfileAsync(company, product);
             return profile is not null ? Results.Ok(profile) : Results.NotFound();
        });

        group.MapGet("/job/view", async ([FromQuery] int company, [FromQuery] long job, [FromQuery] string? options, ICompanyService companyService) =>
        {
             var profile = await companyService.GetJobProfileAsync(company, job);
             return profile is not null ? Results.Ok(profile) : Results.NotFound();
        });

        group.MapGet("/project/view", async ([FromQuery] int company, [FromQuery] long project, [FromQuery] string? options, ICompanyService companyService) =>
        {
             var profile = await companyService.GetProjectProfileAsync(company, project);
             return profile is not null ? Results.Ok(profile) : Results.NotFound();
        });
    }
}
