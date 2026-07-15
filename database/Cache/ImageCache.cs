using System;
using System.Linq;
using BizSrt.Foundation.Cache;
using BizSrt.Model;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Processing;

namespace BizSrt.Data.Cache
{
    public struct ImageCacheKey : IEquatable<ImageCacheKey>
    {
        public ImageEntity Entity { get; }
        public long Id { get; }

        public ImageCacheKey(ImageEntity entity, long id)
        {
            Entity = entity;
            Id = id;
        }

        public bool Equals(ImageCacheKey other) => Entity == other.Entity && Id == other.Id;
        public override bool Equals(object? obj) => obj is ImageCacheKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Entity, Id);
    }

    public class CachedImage : IExpirationItem
    {
        public byte[] Content { get; protected set; }
        public ImageType Type { get; protected set; }
        public ushort Width { get; protected set; }
        public ushort Height { get; protected set; }

        public int HitCount { get; set; }
        public int LastHit { get; set; }

        internal CachedImage(byte[] content, ImageType type, ushort width, ushort height)
        {
            Content = content;
            Type = type;
            Width = width;
            Height = height;
        }

        public byte[]? Resize(int width, int height, out ImageType imageType)
        {
            imageType = Type;
            if (Content == null || Content.Length == 0) return Content;

            try
            {
                using var image = SixLabors.ImageSharp.Image.Load(Content);
                if (image.Width <= width && image.Height <= height)
                {
                    return Content;
                }

                image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(width, height),
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                }));

                using var ms = new System.IO.MemoryStream();
                image.Save(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                return ms.ToArray();
            }
            catch
            {
                return Content; // Fallback to original
            }
        }
    }

    public class ImagesCache : ReadOneExpirationCache<ImageCacheKey, CachedImage>
    {
        public ImagesCache()
            : base((ImageCacheKey key) =>
            {
                using var dbContext = LegacyCache.GetDbContext();
                CachedImage? image = null;

                switch (key.Entity)
                {
                    case ImageEntity.Company:
                        if (key.Id > 0)
                        {
                            var cm = dbContext.CompanyMedia.AsNoTracking().FirstOrDefault(m => m.Id == (int)key.Id);
                            if (cm != null && cm.Content != null)
                            {
                                // Skipping metadata parsing for now, returning dummy Jpeg type
                                image = new CachedImage(cm.Content, ImageType.Jpeg, 0, 0);
                            }
                        }
                        break;
                }

                return image!; // ReadOneCache expects the value, if null it might throw if not configured properly, but legacy handle it
            }, 1000)
        {
        }
    }
}
