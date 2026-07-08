using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Model;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Foundation.Cache;
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
                cq = from c in cq
                     where c.Category == key.Item1 || dbContext.Categories_Unwound.Any(cu => cu.Parent == key.Item1 && cu.Child == c.Category)
                     select c;
            }

            if (key.Item2 > 0)
            {
                var coq = BizSrt.Api.Data.QueryExtensions.LocationQuery(dbContext.CompanyOffices, dbContext, key.Item2);

                cq = from c in cq
                     where coq.Any(co => co.Company == c.Id)
                     select c; 
            }

            var qt = (from b in cq
                      where dbContext.CompanyMedia.Any(m => m.Company == b.Id && m.Type == (byte)BizSrt.Api.Model.MediaType.Default_Image)
                      orderby b.Created descending
                      select b.Id).Take(500).AsEnumerable();

            var result = new System.Collections.Generic.List<int>();
            foreach (var id in qt)
            {
                var cm = dbContext.CompanyMedia
                    .Where(m => m.Company == id && m.Type == (byte)BizSrt.Api.Model.MediaType.Default_Image)
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
