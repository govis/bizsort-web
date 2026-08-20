using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Model;
using BizSrt.Model.Offering;
using BizSrt.Foundation.Cache;
using BizSrt.Foundation;

namespace BizSrt.Api.Data.Cache.Offering;

public class CachedCompanyOffering : PartCache, IKey<long>, IExpirationItem
{
    public long Key => Id;
    public int HitCount { get; set; }
    public int LastHit { get; set; }

    public long Id { get; set; }
    public byte UnlistedType { get; set; }
    public short Type { get; set; }
    public int CompanyId { get; set; }
    public string Text { get; set; } = string.Empty;
    private string? _richText;
    public string? RichText 
    { 
        get 
        {
            return Get(ref _richText, Id, (id) =>
            {
                using var dc = LegacyCache.GetDbContext();
                var offering = dc.Offerings.Where(p => p.Id == id).Select(p => new { p.RichText }).SingleOrDefault();
                return offering?.RichText;
            });
        } 
    }
    public string PreviewText => TextConverter.Varchar(Text, 250);
    public string Title { get; set; } = string.Empty;
    public short Category { get; set; }
    
    public long ImageId { get; set; }
    public ImageSizeType ImageSize { get; set; }
    
    public ImageEntity ImageEntity => (ImageId == 0 && Type == (short)BizSrt.Model.Offering.OfferingType.ItemType.Service) 
        ? ImageEntity.Service 
        : ImageEntity.Offering;

    public Image<long> Image => new Image<long> { Entity = ImageEntity, ImageId = ImageId, MaxImageSize = ImageSize };

    public string WebUrl { get; set; } = string.Empty;
    public byte Status { get; set; }
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
    public DateTime Updated { get; set; }

    public Preview ToPreview(Action<Preview, CachedCompanyOffering>? populate = null)
    {
        var prvw = new Preview 
        { 
            Id = Id,
            Name = Title,
            Type = new BizSrt.Model.Offering.OfferingType { ItemKey = Type, ItemText = "Offering" },
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

public class CompanyOfferingCache : ReadManyExpirationCache<long, CachedCompanyOffering>
{
    public CompanyOfferingCache()
        : base(
            (List<long> offeringIds) =>
            {
                using var dbContext = LegacyCache.GetDbContext();
                
                var query = from p in dbContext.Offerings
                            where offeringIds.Contains(p.Id)
                            join cp in dbContext.CompanyOfferings on p.Id equals cp.Offering
                            from pm in dbContext.OfferingMedia
                                .Where(m => m.Offering == p.Id && m.Type == (byte)MediaType.Default_Image)
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
                                ImageId = (long?)pm.Id ?? 0, 
                                ImageMetadata = pm.Metadata,
                                p.Status
                            };

                var offerings = query.AsNoTracking().ToList();

                return offerings.Select(pt => new CachedCompanyOffering
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
            (long offeringId) =>
            {
                using var dbContext = LegacyCache.GetDbContext();

                var query = from p in dbContext.Offerings
                            where p.Id == offeringId
                            join cp in dbContext.CompanyOfferings on p.Id equals cp.Offering
                            from pm in dbContext.OfferingMedia
                                .Where(m => m.Offering == p.Id && m.Type == (byte)MediaType.Default_Image)
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
                                ImageId = (long?)pm.Id ?? 0, 
                                ImageMetadata = pm.Metadata,
                                p.Status
                            };

                var pt = query.AsNoTracking().SingleOrDefault();

                if (pt == null) return null;

                return new CachedCompanyOffering
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
