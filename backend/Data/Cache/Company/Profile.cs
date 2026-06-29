using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizSrt.Api.Model.Company;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Model;
using BizSrt.Api.Foundation.Cache;

namespace BizSrt.Api.Data.Cache.Company;

public class CachedCompanyProfile : BizSrt.Api.Foundation.Cache.PartCache, BizSrt.Api.Foundation.Cache.IKey<int>, BizSrt.Api.Foundation.Cache.IExpirationItem
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
    
    public BizSrt.Api.Model.Company.Option.Set Options { get; set; } = new();

    public int ImageId { get; set; }
    public BizSrt.Api.Model.ImageSizeType ImageSize { get; set; }

    public BizSrt.Api.Model.Image<int> Image => new BizSrt.Api.Model.Image<int> { Entity = BizSrt.Api.Model.ImageEntity.Company, ImageId = ImageId, MaxImageSize = ImageSize };

    private string? _multiProduct;
    public string MultiProduct
    {
        get
        {
            return Get<int, string>(ref _multiProduct, Id, (int company) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                var mp = dbContext.CompanyProducts
                    .Where(cp => cp.Company == company && cp.UnlistedType == 0) // 0 = Listed
                    .Join(dbContext.Products, cp => cp.Product, p => p.Id, (cp, p) => p.RichText)
                    .FirstOrDefault(rt => !string.IsNullOrEmpty(rt));
                return !string.IsNullOrWhiteSpace(mp) ? mp : string.Empty;
            });
        }
    }

    private long[]? _products;
    public long[] Products
    {
        get
        {
            return GetArray<int, long>(ref _products, Id, (int company) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                return dbContext.CompanyProducts
                    .Where(cp => cp.Company == company && cp.UnlistedType == 0) // 0 = Listed
                    .Select(cp => cp.Product)
                    .ToArray();
            });
        }
    }

    private CachedCompanyOffice[]? _offices;
    public CachedCompanyOffice[] Offices
    {
        get
        {
            return GetArray<int, CachedCompanyOffice>(ref _offices, Id, (int company) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                return dbContext.CompanyOffices
                    .Where(co => co.Company == company)
                    .Select(co => new CachedCompanyOffice
                    {
                        Id = co.Id,
                        Phone = co.Phone ?? string.Empty,
                        Address = new BizSrt.Api.Model.Location
                        {
                            Address = (co.StreetNumber + " " + co.Address1 + ", " + co.PostalCode).Trim().Trim(',')
                        }
                    }).ToArray();
            });
        }
    }

    public CachedCompanyOffice? HeadOffice => Offices.OrderBy(o => o.Id).FirstOrDefault();

    public Preview ToPreview(int officeId = 0)
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
            ProductsView = !string.IsNullOrEmpty(MultiProduct) ? BizSrt.Api.Model.ProductsView.Multiproduct : Options.Products_Marketplace ? BizSrt.Api.Model.ProductsView.Marketplace : BizSrt.Api.Model.ProductsView.ProductList,
            Category = Category > 0 ? BizSrt.Api.Data.Cache.LegacyCache.Categories[Category].ToModel(BizSrt.Api.Model.Group.DisplayType.Name) : null,
            Image = Image
        };
        
        if (prvw.ProductsView != BizSrt.Api.Model.ProductsView.Multiproduct && Products?.Length == 0)
            prvw.ProductsView = BizSrt.Api.Model.ProductsView.NoProducts;
        
        return prvw;
    }
}

public class CachedCompanyOffice
{
    public int Id { get; set; }
    public BizSrt.Api.Model.Location Address { get; set; } = new();
    public string Phone { get; set; } = string.Empty;
}

public class CompanyProfilesCache : ReadManyExpirationCache<int, CachedCompanyProfile>
{
    public CompanyProfilesCache()
        : base(
            (List<int> accountIds) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();

                var profilesQuery = from c in dbContext.CompanyProfiles
                                    where accountIds.Contains(c.Id)
                                    let biId = (int?)dbContext.CompanyMedia
                                        .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Api.Model.MediaType.Default_Image)
                                        .Select(m => m.Id)
                                        .FirstOrDefault()
                                    select new { Profile = c, ImageId = biId ?? 0 };

                var profiles = profilesQuery.AsNoTracking().ToList();

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
                        Options = new BizSrt.Api.Model.Company.Option.Set { Value = (BizSrt.Api.Model.Company.Option.Flags)p.Profile.Options },
                        ImageId = p.ImageId
                    };
                }).ToArray();
            },
            (int accountId) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                
                var profileQuery = from c in dbContext.CompanyProfiles
                                   where c.Id == accountId
                                   let biId = (int?)dbContext.CompanyMedia
                                       .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Api.Model.MediaType.Default_Image)
                                       .Select(m => m.Id)
                                       .FirstOrDefault()
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
                    Options = new BizSrt.Api.Model.Company.Option.Set { Value = (BizSrt.Api.Model.Company.Option.Flags)p.Profile.Options },
                    ImageId = p.ImageId
                };
            },
            1000)
    {
    }
}
