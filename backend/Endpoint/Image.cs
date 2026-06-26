using Microsoft.AspNetCore.Mvc;
using BizSrt.Api.Service;
using BizSrt.Api.Model;

namespace BizSrt.Api.Endpoint;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/image").WithTags("Image");

        group.MapGet("/get", async ([FromQuery] ImageEntity entity, [FromQuery] long id, [FromQuery] int w, [FromQuery] int h, IImageService imageService) =>
        {
            var imageBytes = await imageService.GetImageAsync(entity, id, w, h);
            return imageBytes is not null ? Results.File(imageBytes, "image/jpeg") : Results.NotFound();
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

