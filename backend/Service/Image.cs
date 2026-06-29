using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data;
using BizSrt.Api.Model;
using BizSrt.Api.Data.Entities;

namespace BizSrt.Api.Service;

public interface IImageService
{
    Task<(byte[]? Content, string ContentType)> GetImageAsync(ImageEntity entity, long id, int width, int height);
    Task<byte[]> GenerateCaptchaAsync(string text);
}

public class ImageService(IServiceScopeFactory serviceScopeFactory) : IImageService
{
    public async Task<(byte[]? Content, string ContentType)> GetImageAsync(ImageEntity entity, long id, int width, int height)
    {
        var key = new BizSrt.Api.Data.Cache.ImageCacheKey(entity, id);
        
        // Suppress exception if record not found
        var image = BizSrt.Api.Data.Cache.LegacyCache.Images[key, BizSrt.Api.Foundation.Cache.ReadOneSuppress.RecordNotFound];
        
        if (image == null) return (null, string.Empty);
        
        // Call Resize which returns raw bytes for now
        var content = image.Resize(width, height, out var type);
        string contentType = type switch
        {
            ImageType.Png => "image/png",
            ImageType.Gif => "image/gif",
            _ => "image/jpeg"
        };
        
        return await Task.FromResult((content, contentType));
    }

    public async Task<byte[]> GenerateCaptchaAsync(string text)
    {
        using var image = new SixLabors.ImageSharp.Image<Rgba32>(200, 50);
        image.Mutate(x => x.BackgroundColor(Color.White));
        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);
        return ms.ToArray();
    }
}

