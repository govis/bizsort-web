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
    Task<byte[]?> GetImageAsync(ImageEntity entity, long id, int width, int height);
    Task<byte[]> GenerateCaptchaAsync(string text);
}

public class ImageService(IServiceScopeFactory serviceScopeFactory) : IImageService
{
    public async Task<byte[]?> GetImageAsync(ImageEntity entity, long id, int width, int height)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        IMedia? media = entity switch
        {
            ImageEntity.Company => await dbContext.CompanyMedia.FirstOrDefaultAsync(m => m.Id == (int)id),
            ImageEntity.Product => await dbContext.ProductMedia.FirstOrDefaultAsync(m => m.Id == id),
            ImageEntity.Project => await dbContext.ProjectMedia.FirstOrDefaultAsync(m => m.Id == id),
            ImageEntity.Community => await dbContext.CommunityMedia.FirstOrDefaultAsync(m => m.Id == (int)id),
            _ => null
        };

        if (media?.Content is null) return null;

        using var image = SixLabors.ImageSharp.Image.Load(media.Content);
        
        if (width > 0 && height > 0)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));
        }

        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);
        return ms.ToArray();
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

