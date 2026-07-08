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
        private static DbContextOptions<AppDbContext> _dbContextOptions;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _dbContextOptions = serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
            Categories = new CategoriesCache();
            Locations = new LocationsCache();
            LocationSettings = new Dictionary<int, BizSrt.Api.Model.LocationSettings>();
            Dictionary = BizSrt.Api.Foundation.Cache.Dictionary.SecurityProfile;
            CategorySearch = new BizSrt.Api.Data.CategorySearchCache();
            LocationSearch = new BizSrt.Api.Data.LocationSearchCache();
            AreaNames = new BizSrt.Api.Data.AreaNamesCache();
            StreetNames = new BizSrt.Api.Data.StreetNamesCache();
            CompanyProfiles = new BizSrt.Api.Data.Cache.Company.CompanyProfilesCache();
            FeaturedCompanies = new BizSrt.Api.Data.Cache.Featured.FeaturedCompaniesCache();
            CompanyProducts = new BizSrt.Api.Data.Cache.Company.CompanyProductCache();
            FeaturedProducts = new BizSrt.Api.Data.Cache.Featured.FeaturedProductsCache();
            Images = new BizSrt.Api.Data.Cache.ImagesCache();
        }

        public static AppDbContext GetDbContext()
        {
            return new AppDbContext(_dbContextOptions);
        }

        internal static CategoriesCache Categories { get; private set; }
        internal static LocationsCache Locations { get; private set; }
        internal static BizSrt.Api.Foundation.Cache.Dictionary Dictionary { get; private set; }
        internal static BizSrt.Api.Data.CategorySearchCache CategorySearch { get; private set; }
        internal static BizSrt.Api.Data.LocationSearchCache LocationSearch { get; private set; }
        internal static BizSrt.Api.Data.AreaNamesCache AreaNames { get; private set; }
        internal static BizSrt.Api.Data.StreetNamesCache StreetNames { get; private set; }
        internal static BizSrt.Api.Data.Cache.Company.CompanyProfilesCache CompanyProfiles { get; private set; }
        internal static BizSrt.Api.Data.Cache.Featured.FeaturedCompaniesCache FeaturedCompanies { get; private set; }
        internal static BizSrt.Api.Data.Cache.Company.CompanyProductCache CompanyProducts { get; private set; }
        internal static BizSrt.Api.Data.Cache.Featured.FeaturedProductsCache FeaturedProducts { get; private set; }
        internal static BizSrt.Api.Data.Cache.ImagesCache Images { get; private set; }
        
        internal static Dictionary<int, BizSrt.Api.Model.LocationSettings> LocationSettings { get; private set; }
    }
}
