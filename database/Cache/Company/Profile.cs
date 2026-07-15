using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizSrt.Model.Company;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BizSrt.Model;
using BizSrt.Foundation.Cache;

namespace BizSrt.Data.Cache.Company;

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

    // Eagerly loaded by cache — no per-company DB hit in ToPreview
    public CachedCompanyOffice[] Offices { get; set; } = Array.Empty<CachedCompanyOffice>();
    public long[] Products { get; set; } = Array.Empty<long>();
    public string MultiProduct { get; set; } = string.Empty;

    public CachedCompanyOffice? HeadOffice => Offices.OrderBy(o => o.Id).FirstOrDefault();

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
            Category = Category > 0 ? BizSrt.Data.Cache.LegacyCache.Categories[Category].ToModel(BizSrt.Model.Group.DisplayType.Name) : null,
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
}

public class CompanyProfilesCache : ReadManyExpirationCache<int, CachedCompanyProfile>
{
    public CompanyProfilesCache()
        : base(
            (List<int> accountIds) =>
            {
                using var dbContext = BizSrt.Data.Cache.LegacyCache.GetDbContext();

                // Batch-load all three related collections in 3 queries (not N*3)
                var profilesQuery = from c in dbContext.CompanyProfiles
                                    where accountIds.Contains(c.Id)
                                    from biId in dbContext.CompanyMedia
                                        .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                                        .Select(m => (int?)m.Id)
                                        .Take(1)
                                        .DefaultIfEmpty()
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
                        Address = new BizSrt.Model.Location
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
                        Options = new BizSrt.Model.Company.Option.Set { Value = (BizSrt.Model.Company.Option.Flags)p.Profile.Options },
                        ImageId = p.ImageId,
                        Offices = allOffices.GetValueOrDefault(p.Profile.Id, Array.Empty<CachedCompanyOffice>()),
                        Products = allProducts.GetValueOrDefault(p.Profile.Id, Array.Empty<long>()),
                        MultiProduct = allMultiProducts.GetValueOrDefault(p.Profile.Id, string.Empty)
                    };
                }).ToArray();
            },
            (int accountId) =>
            {
                using var dbContext = BizSrt.Data.Cache.LegacyCache.GetDbContext();
                
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

                var offices = dbContext.CompanyOffices
                    .Where(co => co.Company == accountId)
                    .Select(co => new CachedCompanyOffice
                    {
                        Id = co.Id,
                        Phone = co.Phone ?? string.Empty,
                        Address = new BizSrt.Model.Location
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
                    Options = new BizSrt.Model.Company.Option.Set { Value = (BizSrt.Model.Company.Option.Flags)p.Profile.Options },
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

