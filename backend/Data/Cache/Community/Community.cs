using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Model.Community;
using BizSrt.Model;
using BizSrt.Foundation.Cache;

namespace BizSrt.Api.Data.Cache.Community;

public class CachedCommunity : PartCache, IKey<int>, IExpirationItem
{
    public int Key => Id;
    public int HitCount { get; set; }
    public int LastHit { get; set; }

    public int Id { get; set; }
    public byte Type { get; set; }
    public int Owner { get; set; }
    public int Company { get; set; }

    public string Name { get; set; } = string.Empty;
    public int ImageId { get; set; }
    public ImageSizeType ImageSize { get; set; }
    public string Text { get; set; } = string.Empty;
    private string? _richText;
    public string? RichText 
    { 
        get 
        {
            return Get(ref _richText, Id, (id) =>
            {
                using var dc = LegacyCache.GetDbContext();
                var community = dc.Communities.Where(c => c.Id == id).Select(c => new { c.RichText }).SingleOrDefault();
                return community?.RichText;
            });
        } 
    }
    public BizSrt.Model.Location? Address { get; set; }
    public Option.Set Options { get; set; } = new();
    public string? DefaultCategory { get; set; }

    public Image<int> Image => new Image<int> { Entity = ImageEntity.Community, ImageId = ImageId, MaxImageSize = ImageSize };

    public Preview ToPreview(Action<Preview>? populate = null)
    {
        var prvw = new Preview 
        { 
            Type = (UnlistedType)Type, 
            Id = Id, 
            // We can omit Company if we don't strictly need it right now for the slider
            Name = Name, 
            Location = Address, 
            Text = Text,
            Image = Image,
            Options = Options
        };
        populate?.Invoke(prvw);
        return prvw;
    }
}

public class CommunitiesCache : ReadManyExpirationCache<int, CachedCommunity>
{
    public CommunitiesCache() 
        : base(
            (List<int> communityIds) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                
                var query = from c in dbContext.Communities
                            where communityIds.Contains(c.Id)
                            from cc in dbContext.CompanyCommunities
                                .Where(cc => cc.Community == c.Id)
                                .DefaultIfEmpty()
                            from cm in dbContext.CommunityMedia
                                .Where(ci => ci.Community == c.Id && ci.Type == (byte)MediaType.Default_Image)
                                .Take(1)
                                .DefaultIfEmpty()
                            select new { 
                                Community = c, 
                                Type = cc != null ? cc.UnlistedType : (byte)UnlistedType.Listed, 
                                Company = cc != null ? cc.Company : 0, 
                                ImageId = cm != null ? (int?)cm.Id : null, 
                                ImageMetadata = cm != null ? cm.Metadata : null 
                            };

                var results = query.AsNoTracking().ToArray();
                return results.Select(ct => new CachedCommunity 
                { 
                    Id = ct.Community.Id,
                    Type = ct.Type,
                    Owner = ct.Community.Owner,
                    Company = ct.Company,
                    Name = ct.Community.Name,
                    ImageId = ct.ImageId ?? 0,
                    Text = ct.Community.Text ?? string.Empty,
                    Address = ct.Community.Location != null && ct.Community.Location.HasValue ? new BizSrt.Model.Location { Address = (ct.Community.StreetNumber + " " + ct.Community.Address1 + ", " + ct.Community.PostalCode).Trim().Trim(',') } : null,
                    Options = new Option.Set { Value = (Option.Flags)ct.Community.Options },
                    DefaultCategory = !string.IsNullOrEmpty(ct.Community.DefaultCategory) ? ct.Community.DefaultCategory : null 
                }).ToArray();
            }, 
            (int communityId) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                
                var query = from c in dbContext.Communities
                            where c.Id == communityId
                            from cc in dbContext.CompanyCommunities
                                .Where(cc => cc.Community == c.Id)
                                .DefaultIfEmpty()
                            from cm in dbContext.CommunityMedia
                                .Where(ci => ci.Community == c.Id && ci.Type == (byte)MediaType.Default_Image)
                                .Take(1)
                                .DefaultIfEmpty()
                            select new { 
                                Community = c, 
                                Type = cc != null ? cc.UnlistedType : (byte)UnlistedType.Listed, 
                                Company = cc != null ? cc.Company : 0, 
                                ImageId = cm != null ? (int?)cm.Id : null, 
                                ImageMetadata = cm != null ? cm.Metadata : null 
                            };

                var ct = query.AsNoTracking().SingleOrDefault();
                if (ct == null) return null;

                return new CachedCommunity 
                { 
                    Id = ct.Community.Id,
                    Type = ct.Type,
                    Owner = ct.Community.Owner,
                    Company = ct.Company,
                    Name = ct.Community.Name,
                    ImageId = ct.ImageId ?? 0,
                    Text = ct.Community.Text ?? string.Empty,
                    Address = ct.Community.Location != null && ct.Community.Location.HasValue ? new BizSrt.Model.Location { Address = (ct.Community.StreetNumber + " " + ct.Community.Address1 + ", " + ct.Community.PostalCode).Trim().Trim(',') } : null,
                    Options = new Option.Set { Value = (Option.Flags)ct.Community.Options },
                    DefaultCategory = !string.IsNullOrEmpty(ct.Community.DefaultCategory) ? ct.Community.DefaultCategory : null 
                };
            }, 
            1000)
    { 
    }
}
