using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Model;
using BizSrt.Data.Entities;
using BizSrt.Foundation.Cache;
using Microsoft.Extensions.DependencyInjection;

namespace BizSrt.Api.Data.Cache.Featured
{
    public class FeaturedCompaniesCache : FeaturedCache<int[]>
    {
        public FeaturedCompaniesCache() { }

        protected override int[] FetchItems(Tuple<short, int> key)
        {
            using var dbContext = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();

            var cq = from c in dbContext.CompanyProfiles
                     join a in dbContext.Accounts on c.Id equals a.Id
                     where a.Status == 2 // Active
                     select c;

            if (key.Item1 > 0)
            {
                var catIds = dbContext.Categories_Unwound.Where(cu => cu.Parent == key.Item1).Select(cu => cu.Child).ToList();
                catIds.Add(key.Item1);

                cq = from c in cq
                     where catIds.Contains(c.Category)
                     select c;
            }

            if (key.Item2 > 0)
            {
                var locIds = dbContext.Locations_Unwound.Where(lu => lu.Parent == key.Item2).Select(lu => lu.Child).ToList();
                locIds.Add(key.Item2);

                var companyIdsInLocation = dbContext.CompanyOffices.Where(co => locIds.Contains(co.Location)).Select(co => co.Company).Distinct();

                cq = from c in cq
                     join id in companyIdsInLocation on c.Id equals id
                     select c;
            }

            var allItems = (from b in cq
                            select new { b.Id, b.Created }).ToArray();

            var qt = allItems.OrderByDescending(x => x.Created).Select(x => x.Id).Take(500);

            var result = new System.Collections.Generic.List<int>();
            foreach (var id in qt)
            {
                var cm = dbContext.CompanyMedia
                    .Where(m => m.Company == id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                    .Select(m => m.Metadata)
                    .FirstOrDefault();

                if (cm != null && cm.Length > 0)
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
