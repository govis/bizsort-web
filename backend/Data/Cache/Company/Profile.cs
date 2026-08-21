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
    public long ServiceType { get; set; }
    public short TransactionType { get; set; }
    
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
                        LocationId = co.Location,
                        StreetNameId = co.StreetName,
                        StreetNumber = co.StreetNumber,
                        Address = new BizSrt.Model.Location
                        {
                            Address = GetAddressModel(co.StreetNumber, co.StreetName, co.Address1, co.Location, co.PostalCode),
                        }
                    }).ToArray();
            }) ?? Array.Empty<CachedCompanyOffice>();
        }
    }

    private long[]? _offerings;
    public long[] Offerings
    {
        get
        {
            return GetArray(ref _offerings, Id, (id) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                return dc.CompanyOfferings
                    .Where(cp => cp.Company == id && cp.UnlistedType == 0)
                    .Select(cp => cp.Offering)
                    .ToArray();
            }) ?? Array.Empty<long>();
        }
    }

    private string? _multiOffering;
    public string MultiOffering
    {
        get
        {
            return Get(ref _multiOffering, Id, (id) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                var offerings = dc.CompanyOfferings
                    .Where(cp => cp.Company == id && cp.UnlistedType == 0)
                    .Join(dc.Offerings, cp => cp.Offering, p2 => p2.Id, (cp, p2) => p2)
                    .Take(2)
                    .ToArray();

                if (offerings.Length == 1 && offerings[0].Type == 0)
                    return !string.IsNullOrEmpty(offerings[0].RichText) ? offerings[0].RichText : string.Empty;

                return string.Empty;
            }) ?? string.Empty;
        }
    }

    public CachedCompanyOffice? HeadOffice => Offices.OrderBy(o => o.Order).FirstOrDefault();

    private static BizSrt.Model.Geocoder.Address GetAddressModel(string? streetNumber, int? streetNameId, string? address1, int? locationId, string? postalCode)
    {
        var address = new BizSrt.Model.Geocoder.Address();
        
        if (streetNameId.HasValue && streetNameId.Value > 0)
        {
            var cachedStreet = BizSrt.Api.Data.Cache.LegacyCache.StreetNames[streetNameId.Value];
            if (cachedStreet != null) address.StreetName = cachedStreet.Name;
        }

        if (!string.IsNullOrWhiteSpace(streetNumber)) address.StreetNumber = streetNumber;
        if (!string.IsNullOrWhiteSpace(address1)) address.Address1 = address1;
        if (!string.IsNullOrWhiteSpace(postalCode)) address.PostalCode = postalCode;

        if (locationId.HasValue && locationId.Value > 0)
        {
            var loc = BizSrt.Api.Data.Cache.LegacyCache.Locations[locationId.Value];
            while (loc != null && loc.Id > 1)
            {
                if (loc.Type == BizSrt.Model.LocationType.City) address.City = loc.Name;
                else if (loc.Type == BizSrt.Model.LocationType.State) address.State = loc.Name;
                else if (loc.Type == BizSrt.Model.LocationType.County) address.County = loc.Name;
                else if (loc.Type == BizSrt.Model.LocationType.Country) address.Country = loc.Name;
                
                if (loc.ParentKey > 0)
                    loc = BizSrt.Api.Data.Cache.LegacyCache.Locations[loc.ParentKey] as BizSrt.Api.Data.Cache.Location.CachedLocation;
                else
                    loc = null;
            }
        }
        
        return address;
    }

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
            OfferingsView = !string.IsNullOrEmpty(MultiOffering) ? BizSrt.Model.OfferingsView.Multioffering : Options.Offerings_Marketplace ? BizSrt.Model.OfferingsView.Marketplace : BizSrt.Model.OfferingsView.OfferingList,
            Category = Category > 0 ? BizSrt.Api.Data.Cache.LegacyCache.Categories[Category].ToModel(BizSrt.Model.Group.DisplayType.Name) : null,
            Image = Image
        };
        
        if (prvw.OfferingsView != BizSrt.Model.OfferingsView.Multioffering && Offerings?.Length == 0)
            prvw.OfferingsView = BizSrt.Model.OfferingsView.NoOfferings;
        
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
    public int? LocationId { get; set; }
    public int? StreetNameId { get; set; }
    public string? StreetNumber { get; set; }
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
                            from media in dbContext.CompanyMedia
                                .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                                .Select(m => new { Id = (int?)m.Id, m.Metadata })
                                .Take(1)
                                .DefaultIfEmpty()
                            select new { Profile = c, Media = media };

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
                        ServiceType = p.Profile.ServiceType,
                        TransactionType = p.Profile.TransactionType,
                        Options = new BizSrt.Model.Company.Option.Set { Value = (BizSrt.Model.Company.Option.Flags)p.Profile.Options },
                        ImageId = p.Media != null ? p.Media.Id ?? 0 : 0,
                        ImageSize = p.Media != null ? BizSrt.Foundation.Entity.Image.ResolveSize(BizSrt.Model.ImageEntity.Company, p.Media.Metadata) : BizSrt.Model.ImageSizeType.None
                    };
                }).ToArray();
            },
            (int accountId) =>
            {
                using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                
                var profileQuery = from c in dbContext.CompanyProfiles
                                   where c.Id == accountId
                                   from media in dbContext.CompanyMedia
                                       .Where(m => m.Company == c.Id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                                       .Select(m => new { Id = (int?)m.Id, m.Metadata })
                                       .Take(1)
                                       .DefaultIfEmpty()
                                   select new { Profile = c, Media = media };

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
                    ServiceType = p.Profile.ServiceType,
                    TransactionType = p.Profile.TransactionType,
                    Options = new BizSrt.Model.Company.Option.Set { Value = (BizSrt.Model.Company.Option.Flags)p.Profile.Options },
                    ImageId = p.Media != null ? p.Media.Id ?? 0 : 0,
                    ImageSize = p.Media != null ? BizSrt.Foundation.Entity.Image.ResolveSize(BizSrt.Model.ImageEntity.Company, p.Media.Metadata) : BizSrt.Model.ImageSizeType.None
                };
            },
            1000)
    {
    }
}
