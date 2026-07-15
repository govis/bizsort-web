using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Model;
using BizSrt.Model.Product;
using BizSrt.Foundation.Cache;
using BizSrt.Foundation;

namespace BizSrt.Api.Data.Cache.Company;

public class CachedCompanyProduct : PartCache, IKey<long>, IExpirationItem
{
    public long Key => Id;
    public int HitCount { get; set; }
    public int LastHit { get; set; }

    public long Id { get; set; }
    public byte UnlistedType { get; set; }
    public short Type { get; set; }
    public int CompanyId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string PreviewText => TextConverter.Varchar(Text, 250);
    public string Title { get; set; } = string.Empty;
    public short Category { get; set; }
    
    public long ImageId { get; set; }
    public ImageSizeType ImageSize { get; set; }
    
    public ImageEntity ImageEntity => (ImageId == 0 && Type == (short)BizSrt.Model.Product.ProductType.ItemType.Service) 
        ? ImageEntity.Service 
        : ImageEntity.Product;

    public Image<long> Image => new Image<long> { Entity = ImageEntity, ImageId = ImageId, MaxImageSize = ImageSize };

    public string WebUrl { get; set; } = string.Empty;
    public byte Status { get; set; }
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
    public DateTime Updated { get; set; }

    public Preview ToPreview(Action<Preview, CachedCompanyProduct>? populate = null)
    {
        var prvw = new Preview 
        { 
            Id = Id,
            Name = Title,
            Type = new BizSrt.Model.Product.ProductType { ItemKey = Type, ItemText = "Product" },
            WebUrl = WebUrl,
            Text = PreviewText,
            Date = Created,
            Image = Image
        };
        prvw.Properties["unlistedType"] = UnlistedType;
        prvw.Properties["status"] = Status;
        
        populate?.Invoke(prvw, this);
        return prvw;
    }
}

public class CompanyProductCache : ReadManyExpirationCache<long, CachedCompanyProduct>
{
    public CompanyProductCache()
        : base(
            (List<long> productIds) =>
            {
                using var dbContext = LegacyCache.GetDbContext();
                
                var query = from p in dbContext.Products
                            where productIds.Contains(p.Id)
                            join cp in dbContext.CompanyProducts on p.Id equals cp.Product
                            from pmId in dbContext.ProductMedia
                                .Where(m => m.Product == p.Id && m.Type == (byte)MediaType.Default_Image)
                                .Select(m => new { m.Id, m.Metadata })
                                .Take(1)
                                .DefaultIfEmpty()
                            select new 
                            { 
                                p.Id, 
                                cp.Company, 
                                cp.UnlistedType, 
                                p.Type, 
                                cp.Category, 
                                p.Text, 
                                p.Title, 
                                p.WebUrl, 
                                p.CreatedBy, 
                                p.Created, 
                                p.Updated, 
                                ImageId = pmId != null ? pmId.Id : 0, 
                                ImageMetadata = pmId != null ? pmId.Metadata : null,
                                p.Status
                            };

                var products = query.AsNoTracking().ToList();

                return products.Select(pt => new CachedCompanyProduct
                {
                    Id = pt.Id,
                    CompanyId = pt.Company,
                    CreatedBy = pt.CreatedBy,
                    UnlistedType = pt.UnlistedType,
                    Type = pt.Type,
                    Text = pt.Text ?? string.Empty,
                    Title = pt.Title ?? string.Empty,
                    Category = pt.Category,
                    ImageId = pt.ImageId,
                    ImageSize = ImageSizeType.View,
                    WebUrl = pt.WebUrl ?? string.Empty,
                    Created = pt.Created,
                    Updated = pt.Updated,
                    Status = pt.Status
                }).ToArray();
            },
            (long productId) =>
            {
                using var dbContext = LegacyCache.GetDbContext();

                var query = from p in dbContext.Products
                            where p.Id == productId
                            join cp in dbContext.CompanyProducts on p.Id equals cp.Product
                            from pmId in dbContext.ProductMedia
                                .Where(m => m.Product == p.Id && m.Type == (byte)MediaType.Default_Image)
                                .Select(m => new { m.Id, m.Metadata })
                                .Take(1)
                                .DefaultIfEmpty()
                            select new 
                            { 
                                p.Id, 
                                cp.Company, 
                                cp.UnlistedType, 
                                p.Type, 
                                cp.Category, 
                                p.Text, 
                                p.Title, 
                                p.WebUrl, 
                                p.CreatedBy, 
                                p.Created, 
                                p.Updated, 
                                ImageId = pmId != null ? pmId.Id : 0, 
                                ImageMetadata = pmId != null ? pmId.Metadata : null,
                                p.Status
                            };

                var pt = query.AsNoTracking().SingleOrDefault();

                if (pt == null) return null;

                return new CachedCompanyProduct
                {
                    Id = pt.Id,
                    CompanyId = pt.Company,
                    CreatedBy = pt.CreatedBy,
                    UnlistedType = pt.UnlistedType,
                    Type = pt.Type,
                    Text = pt.Text ?? string.Empty,
                    Title = pt.Title ?? string.Empty,
                    Category = pt.Category,
                    ImageId = pt.ImageId,
                    ImageSize = ImageSizeType.View,
                    WebUrl = pt.WebUrl ?? string.Empty,
                    Created = pt.Created,
                    Updated = pt.Updated,
                    Status = pt.Status
                };
            },
            1000)
    {
    }
}
