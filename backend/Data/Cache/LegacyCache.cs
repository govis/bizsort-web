using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Data;
using BizSrt.Api.Data.Cache.Location;
namespace BizSrt.Api.Data.Cache
{
    public static class LegacyCache
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Categories = new CategoriesCache();
            Locations = new LocationsCache();
            LocationSettings = new Dictionary<int, BizSrt.Api.Model.LocationSettings>();
            Dictionary = BizSrt.Api.Foundation.Cache.Dictionary.SecurityProfile;
            CategorySearch = new BizSrt.Api.Data.CategorySearchCache();
            LocationSearch = new BizSrt.Api.Data.LocationSearchCache();
            AreaNames = new BizSrt.Api.Data.AreaNamesCache();
            StreetNames = new BizSrt.Api.Data.StreetNamesCache();
            CompanyProfiles = new BizSrt.Api.Data.Cache.Company.CompanyProfilesCache();
            FeaturedCompanies = new BizSrt.Api.Data.Cache.Company.FeaturedCompaniesCache();
        }

        public static AppDbContext GetDbContext()
        {
            var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        internal static CategoriesCache Categories { get; private set; }
        internal static LocationsCache Locations { get; private set; }
        internal static BizSrt.Api.Foundation.Cache.Dictionary Dictionary { get; private set; }
        internal static BizSrt.Api.Data.CategorySearchCache CategorySearch { get; private set; }
        internal static BizSrt.Api.Data.LocationSearchCache LocationSearch { get; private set; }
        internal static BizSrt.Api.Data.AreaNamesCache AreaNames { get; private set; }
        internal static BizSrt.Api.Data.StreetNamesCache StreetNames { get; private set; }
        internal static BizSrt.Api.Data.Cache.Company.CompanyProfilesCache CompanyProfiles { get; private set; }
        internal static BizSrt.Api.Data.Cache.Company.FeaturedCompaniesCache FeaturedCompanies { get; private set; }
        
        internal static Dictionary<int, BizSrt.Api.Model.LocationSettings> LocationSettings { get; private set; }
    }
}
