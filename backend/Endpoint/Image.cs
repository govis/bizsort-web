using Microsoft.AspNetCore.Mvc;
using BizSrt.Api.Service;
using BizSrt.Model;

namespace BizSrt.Api.Endpoint;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/image").WithTags("Image");

        group.MapGet("/get", async ([FromQuery] ImageEntity entity, [FromQuery] long id, [FromQuery] int width, [FromQuery] int height, IImageService imageService) =>
        {
            var result = await imageService.GetImageAsync(entity, id, width, height);
            return result.Content is not null ? Results.File(result.Content, result.ContentType) : Results.NotFound();
        })
        .WithName("GetImage")
        .WithOpenApi();

        group.MapGet("/captcha", async (IImageService imageService) =>
        {
            var captchaBytes = await imageService.GenerateCaptchaAsync("ABCD");
            return Results.File(captchaBytes, "image/jpeg");
        })
        .WithName("GetCaptcha")
        .WithOpenApi();
    }
}

