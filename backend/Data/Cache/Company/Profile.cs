using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizSrt.Api.Model.Company;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Model.Legacy;
using BizSrt.Api.Foundation.Cache;

namespace BizSrt.Api.Data.Cache.Company;

public class CachedCompanyProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WebSite { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public short Category { get; set; }
    
    // Scaffolding only what's required for ToPreview for now
    public CachedCompanyOffice? HeadOffice { get; set; }
    public CachedCompanyOffice[] Offices { get; set; } = Array.Empty<CachedCompanyOffice>();

    public Preview ToPreview(int officeId = 0, string? categoryName = null)
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
            ProductsView = ProductsView.NoProducts, // Simplified for now
            Category = Category > 0 && categoryName != null ? new Category { Id = Category, Name = categoryName } : null 
        };
        
        return prvw;
    }
}

public class CachedCompanyOffice
{
    public int Id { get; set; }
    public Location Address { get; set; } = new();
    public string Phone { get; set; } = string.Empty;
}

public class CompanyProfilesCache : ReadManyExpirationCache<int, CachedCompanyProfile>
{
    public CompanyProfilesCache(IServiceProvider serviceProvider)
        : base(
            async (List<int> accountIds) =>
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var profiles = await dbContext.CompanyProfiles
                    .Include(c => c.Offices)
                    .Where(c => accountIds.Contains(c.Id))
                    .AsNoTracking()
                    .ToListAsync();

                return profiles.Select(p => 
                {
                    var offices = p.Offices.Select(o => new CachedCompanyOffice
                    {
                        Id = o.Id,
                        Phone = o.Phone,
                        Address = new Location
                        {
                            Address = $"{o.StreetNumber} {o.Address1}, {o.PostalCode}".Trim().Trim(','),
                            GeoLocation = o.GeoLocation is NetTopologySuite.Geometries.Point pt 
                                ? new Geolocation { Lat = pt.Y, Lng = pt.X } 
                                : null
                        }
                    }).ToArray();

                    return new CachedCompanyProfile
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Email = p.Email,
                        WebSite = p.WebSite ?? string.Empty,
                        Text = p.Text ?? string.Empty,
                        Category = p.Category,
                        Offices = offices,
                        HeadOffice = offices.OrderBy(o => o.Id).FirstOrDefault()
                    };
                }).ToArray();
            },
            async (int accountId) =>
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var p = await dbContext.CompanyProfiles
                    .Include(c => c.Offices)
                    .Where(c => c.Id == accountId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (p == null) return null;

                var offices = p.Offices.Select(o => new CachedCompanyOffice
                {
                    Id = o.Id,
                    Phone = o.Phone,
                    Address = new Location
                    {
                        Address = $"{o.StreetNumber} {o.Address1}, {o.PostalCode}".Trim().Trim(','),
                        GeoLocation = o.GeoLocation is NetTopologySuite.Geometries.Point pt 
                            ? new Geolocation { Lat = pt.Y, Lng = pt.X } 
                            : null
                    }
                }).ToArray();

                return new CachedCompanyProfile
                {
                    Id = p.Id,
                    Name = p.Name,
                    Email = p.Email,
                    WebSite = p.WebSite ?? string.Empty,
                    Text = p.Text ?? string.Empty,
                    Category = p.Category,
                    Offices = offices,
                    HeadOffice = offices.OrderBy(o => o.Id).FirstOrDefault()
                };
            },
            profile => profile.Id)
    {
    }
}
