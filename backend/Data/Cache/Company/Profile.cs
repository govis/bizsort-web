using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizSrt.Model.Company;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BizSrt.Model;
using BizSrt.Foundation.Cache;

namespace BizSrt.Api.Data.Cache.Company;

public class CachedCompanyProfile : BizSrt.Foundation.Cache.PartCache, BizSrt.Foundation.Cache.IKey<int>, BizSrt.Foundation.Cache.IExpirationItem
{
    public int Key => Id;
    public int HitCount { get; set; }
    public int LastHit { get; set; }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WebSite { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public short Category { get; set; }
    
    public BizSrt.Model.Company.Option.Set Options { get; set; } = new();

    public int ImageId { get; set; }
    public BizSrt.Model.ImageSizeType ImageSize { get; set; }

    public BizSrt.Model.Image<int> Image => new BizSrt.Model.Image<int> { Entity = BizSrt.Model.ImageEntity.Company, ImageId = ImageId, MaxImageSize = ImageSize };

    private string? _richText;
    public string RichText
    {
        get
        {
            return Get(ref _richText, Id, (id) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                var companyProfile = dc.CompanyProfiles.Where(cp => cp.Id == id).Select(cp => new { cp.RichText }).SingleOrDefault();
                return companyProfile?.RichText != null && companyProfile.RichText.Length > 0 ? 
                    System.Text.Encoding.UTF8.GetString(companyProfile.RichText) : string.Empty;
            }) ?? string.Empty;
        }
    }

    private CachedCompanyOffice[]? _offices;
    public CachedCompanyOffice[] Offices
    {
        get
        {
            return GetArray(ref _offices, Id, (id) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                return dc.CompanyOffices
                    .Where(co => co.Company == id)
                    .Select(co => new
                    {
                        co.Id, co.Phone, co.Phone1, co.Fax, co.Name, co.Order, co.GeoLocation,
                        co.StreetNumber, co.StreetName, co.Address1, co.Location, co.PostalCode
                    })
                    .ToArray()
                    .Select(co => new CachedCompanyOffice
                    {
                        Id = co.Id,
                        Phone = co.Phone ?? string.Empty,
                        Phone1 = co.Phone1,
                        Fax = co.Fax,
                        Name = co.Name,
                        Order = co.Order,
                        GeoLocation = co.GeoLocation,
                        Address = new BizSrt.Model.Location
                        {
                            Address = ((co.StreetNumber + " " + co.StreetName).Trim() + " " + co.Address1).Trim() + ", " + (co.Location > 0 ? (BizSrt.Api.Data.Cache.LegacyCache.Locations[co.Location]?.Name + ", " ?? "") : "") + co.PostalCode,
                        }
                    }).ToArray();
            }) ?? Array.Empty<CachedCompanyOffice>();
        }
    }

    private long[]? _products;
    public long[] Products
    {
        get
        {
            return GetArray(ref _products, Id, (id) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                return dc.CompanyProducts
                    .Where(cp => cp.Company == id && cp.UnlistedType == 0)
                    .Select(cp => cp.Product)
                    .ToArray();
            }) ?? Array.Empty<long>();
        }
    }

    private string? _multiProduct;
    public string MultiProduct
    {
        get
        {
            return Get(ref _multiProduct, Id, (id) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                return dc.CompanyProducts
                    .Where(cp => cp.Company == id && cp.UnlistedType == 0)
                    .Join(dc.Products, cp => cp.Product, p2 => p2.Id, (cp, p2) => p2.RichText)
                    .FirstOrDefault(rt => !string.IsNullOrEmpty(rt)) ?? string.Empty;
            }) ?? string.Empty;
        }
    }

    public CachedCompanyOffice? HeadOffice => Offices.OrderBy(o => o.Order).FirstOrDefault();

    public Preview ToPreview(int officeId = 0, Action<Preview, CachedCompanyProfile>? populate = null)
    {
        var office = officeId > 0 ? Offices.FirstOrDefault(o => o.Id == officeId) ?? HeadOffice : HeadOffice;
        
        var prvw = new Preview 
        { 
            Id = Id, 
            Name = Name, 
            Location = office?.Address, 
            WebSite = WebSite, 
            Phone = office?.Phone, 
            Text = Text,
            ProductsView = !string.IsNullOrEmpty(MultiProduct) ? BizSrt.Model.ProductsView.Multiproduct : Options.Products_Marketplace ? BizSrt.Model.ProductsView.Marketplace : BizSrt.Model.ProductsView.ProductList,
            Category = Category > 0 ? BizSrt.Api.Data.Cache.LegacyCache.Categories[Category].ToModel(BizSrt.Model.Group.DisplayType.Name) : null,
            Image = Image
        };
        
        if (prvw.ProductsView != BizSrt.Model.ProductsView.Multiproduct && Products?.Length == 0)
            prvw.ProductsView = BizSrt.Model.ProductsView.NoProducts;
        
        populate?.Invoke(prvw, this);

        return prvw;
    }
}

public class CachedCompanyOffice
{
    public int Id { get; set; }
    public BizSrt.Model.Location Address { get; set; } = new();
    public string Phone { get; set; } = string.Empty;
    public string? Phone1 { get; set; }
    public string? Fax { get; set; }
    public string? Name { get; set; }
    public short Order { get; set; }
    public NetTopologySuite.Geometries.Geometry? GeoLocation { get; set; }
}

public class CompanyProfilesCache : ReadManyExpirationCache<int, CachedCompanyProfile>
{
    public CompanyProfilesCache() 
        : base(
            (List<int> accountIds) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                
                var query = from c in dbContext.CompanyProfiles
                            where accountIds.Contains(c.Id)
                            from biId in dbContext.CompanyMedia
                                .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                                .Select(m => (int?)m.Id)
                                .Take(1)
                                .DefaultIfEmpty()
                            select new { Profile = c, ImageId = biId ?? 0 };

                var profiles = query.AsNoTracking().ToArray();
                
                return profiles.Select(p => 
                {
                    return new CachedCompanyProfile
                    {
                        Id = p.Profile.Id,
                        Name = p.Profile.Name,
                        Email = p.Profile.Email ?? string.Empty,
                        WebSite = p.Profile.WebSite ?? string.Empty,
                        Text = p.Profile.Text ?? string.Empty,
                        Category = p.Profile.Category,
                        Options = new BizSrt.Model.Company.Option.Set { Value = (BizSrt.Model.Company.Option.Flags)p.Profile.Options },
                        ImageId = p.ImageId
                    };
                }).ToArray();
            },
            (int accountId) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                
                var profileQuery = from c in dbContext.CompanyProfiles
                                   where c.Id == accountId
                                   from biId in dbContext.CompanyMedia
                                       .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                                       .Select(m => (int?)m.Id)
                                       .Take(1)
                                       .DefaultIfEmpty()
                                   select new { Profile = c, ImageId = biId ?? 0 };

                var p = profileQuery.AsNoTracking().SingleOrDefault();

                if (p == null) return null;

                return new CachedCompanyProfile
                {
                    Id = p.Profile.Id,
                    Name = p.Profile.Name,
                    Email = p.Profile.Email ?? string.Empty,
                    WebSite = p.Profile.WebSite ?? string.Empty,
                    Text = p.Profile.Text ?? string.Empty,
                    Category = p.Profile.Category,
                    Options = new BizSrt.Model.Company.Option.Set { Value = (BizSrt.Model.Company.Option.Flags)p.Profile.Options },
                    ImageId = p.ImageId
                };
            },
            1000)
    {
    }
}
