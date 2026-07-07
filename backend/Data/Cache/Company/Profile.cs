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

    // Eagerly loaded by cache — no per-company DB hit in ToPreview
    public CachedCompanyOffice[] Offices { get; set; } = Array.Empty<CachedCompanyOffice>();
    public long[] Products { get; set; } = Array.Empty<long>();
    public string MultiProduct { get; set; } = string.Empty;

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

                // Batch-load all three related collections in 3 queries (not N*3)
                var profilesQuery = from c in dbContext.CompanyProfiles
                                    where accountIds.Contains(c.Id)
                                    let biId = (int?)dbContext.CompanyMedia
                                        .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Api.Model.MediaType.Default_Image)
                                        .Select(m => m.Id)
                                        .FirstOrDefault()
                                    select new { Profile = c, ImageId = biId ?? 0 };

                var profiles = profilesQuery.AsNoTracking().ToList();

                // Batch-load offices for all companies in one query
                var allOffices = dbContext.CompanyOffices
                    .Where(co => accountIds.Contains(co.Company))
                    .Select(co => new { co.Company, co.Id, co.Phone, co.StreetNumber, co.Address1, co.PostalCode })
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(co => co.Company)
                    .ToDictionary(g => g.Key, g => g.Select(co => new CachedCompanyOffice
                    {
                        Id = co.Id,
                        Phone = co.Phone ?? string.Empty,
                        Address = new BizSrt.Api.Model.Location
                        {
                            Address = (co.StreetNumber + " " + co.Address1 + ", " + co.PostalCode).Trim().Trim(',')
                        }
                    }).ToArray());

                // Batch-load products for all companies in one query
                var allProducts = dbContext.CompanyProducts
                    .Where(cp => accountIds.Contains(cp.Company) && cp.UnlistedType == 0)
                    .Select(cp => new { cp.Company, cp.Product })
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(cp => cp.Company)
                    .ToDictionary(g => g.Key, g => g.Select(cp => cp.Product).ToArray());

                // Batch-load multiproduct html for all companies in one query
                var allMultiProducts = dbContext.CompanyProducts
                    .Where(cp => accountIds.Contains(cp.Company) && cp.UnlistedType == 0)
                    .Join(dbContext.Products, cp => cp.Product, p => p.Id, (cp, p) => new { cp.Company, p.RichText })
                    .Where(x => !string.IsNullOrEmpty(x.RichText))
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(x => x.Company)
                    .ToDictionary(g => g.Key, g => g.First().RichText ?? string.Empty);

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
                        ImageId = p.ImageId,
                        Offices = allOffices.GetValueOrDefault(p.Profile.Id, Array.Empty<CachedCompanyOffice>()),
                        Products = allProducts.GetValueOrDefault(p.Profile.Id, Array.Empty<long>()),
                        MultiProduct = allMultiProducts.GetValueOrDefault(p.Profile.Id, string.Empty)
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

                var offices = dbContext.CompanyOffices
                    .Where(co => co.Company == accountId)
                    .Select(co => new CachedCompanyOffice
                    {
                        Id = co.Id,
                        Phone = co.Phone ?? string.Empty,
                        Address = new BizSrt.Api.Model.Location
                        {
                            Address = (co.StreetNumber + " " + co.Address1 + ", " + co.PostalCode).Trim().Trim(',')
                        }
                    }).AsNoTracking().ToArray();

                var products = dbContext.CompanyProducts
                    .Where(cp => cp.Company == accountId && cp.UnlistedType == 0)
                    .Select(cp => cp.Product)
                    .ToArray();

                var multiProduct = dbContext.CompanyProducts
                    .Where(cp => cp.Company == accountId && cp.UnlistedType == 0)
                    .Join(dbContext.Products, cp => cp.Product, p2 => p2.Id, (cp, p2) => p2.RichText)
                    .FirstOrDefault(rt => !string.IsNullOrEmpty(rt)) ?? string.Empty;

                return new CachedCompanyProfile
                {
                    Id = p.Profile.Id,
                    Name = p.Profile.Name,
                    Email = p.Profile.Email ?? string.Empty,
                    WebSite = p.Profile.WebSite ?? string.Empty,
                    Text = p.Profile.Text ?? string.Empty,
                    Category = p.Profile.Category,
                    Options = new BizSrt.Api.Model.Company.Option.Set { Value = (BizSrt.Api.Model.Company.Option.Flags)p.Profile.Options },
                    ImageId = p.ImageId,
                    Offices = offices,
                    Products = products,
                    MultiProduct = multiProduct
                };
            },
            1000)
    {
    }
}

