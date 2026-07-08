using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Model;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Foundation.Cache;
using Microsoft.Extensions.DependencyInjection;

namespace BizSrt.Api.Data.Cache.Featured
{
    public class FeaturedProductsCache : FeaturedCache<long[]>
    {
        public FeaturedProductsCache() { }

        protected override long[] FetchItems(Tuple<short, int> key)
        {
            using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();

            var pq = dbContext.Products.Where(p => p.Status == 2);
            var cpq = dbContext.CompanyProducts.Where(cp => cp.UnlistedType == 1);

            if (key.Item1 > 0)
            {
                cpq = from cp in cpq
                      where cp.Category == key.Item1 || dbContext.Categories_Unwound.Any(cu => cu.Parent == key.Item1 && cu.Child == cp.Category)
                      select cp;
            }

            if (key.Item2 > 0)
            {
                var coq = BizSrt.Api.Data.QueryExtensions.LocationQuery(dbContext.CompanyOffices, dbContext, key.Item2);

                cpq = from cp in cpq
                      where coq.Any(co => co.Company == cp.Company)
                      select cp; 
            }

            var qt = (from p in pq
                      join cp in cpq on p.Id equals cp.Product
                      join pi in dbContext.ProductMedia on p.Id equals pi.Product
                      where pi.Type == (byte)BizSrt.Api.Model.MediaType.Default_Image
                      orderby p.Created descending
                      select new { p.Id, pi.Metadata }).Take(500).AsEnumerable();

            return qt.Where(p => p.Metadata != null && p.Metadata.Length > 0)
                     .Select(p => p.Id)
                     .Take(100)
                     .ToArray();
        }
    }
}
