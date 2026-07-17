using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Model;
using BizSrt.Data.Entities;
using BizSrt.Foundation.Cache;
using Microsoft.Extensions.DependencyInjection;

namespace BizSrt.Api.Data.Cache.Featured
{
    public class FeaturedProductsCache : FeaturedCache<long[]>
    {
        public FeaturedProductsCache() { }

        protected override long[] FetchItems(Tuple<short, int> key)
        {
            using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();

            var pq = dbContext.Products.Where(p => p.Status == (byte)BizSrt.Model.Product.Status.Active);
            var cpq = dbContext.CompanyProducts.Where(cp => cp.UnlistedType == (byte)BizSrt.Model.Product.UnlistedType.Listed);

            if (key.Item1 > 0)
            {
                var catIds = dbContext.Categories_Unwound.Where(cu => cu.Parent == key.Item1).Select(cu => cu.Child).ToList();
                catIds.Add(key.Item1);

                cpq = from cp in cpq
                      where catIds.Contains(cp.Category)
                      select cp;
            }

            if (key.Item2 > 0)
            {
                var locIds = dbContext.Locations_Unwound.Where(lu => lu.Parent == key.Item2).Select(lu => lu.Child).ToList();
                locIds.Add(key.Item2);

                var companyIdsInLocation = dbContext.CompanyOffices.Where(co => locIds.Contains(co.Location)).Select(co => co.Company).Distinct();

                cpq = from cp in cpq
                      join companyId in companyIdsInLocation on cp.Company equals companyId
                      select cp;
            }

            var allItems = (from p in pq
                            join cp in cpq on p.Id equals cp.Product
                            select new { p.Id, p.Created }).ToArray();

            var qt = allItems.OrderByDescending(p => p.Created).Select(p => p.Id).Take(500);

            var result = new System.Collections.Generic.List<long>();
            foreach (var id in qt)
            {
                var pm = dbContext.ProductMedia
                    .Where(m => m.Product == id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                    .Select(m => m.Metadata)
                    .FirstOrDefault();

                if (pm != null && pm.Length > 0)
                {
                    result.Add(id);
                    if (result.Count >= 100)
                        break;
                }
            }

            return result.ToArray();
        }
    }
}
