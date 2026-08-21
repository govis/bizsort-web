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
                cq = from c in cq
                     where c.Category == key.Item1 || dbContext.Categories_Unwound.Any(cu => cu.Parent == key.Item1 && cu.Child == c.Category)
                     select c;
            }

            if (key.Item2 > 0)
            {
                var locIds = BizSrt.Api.Data.Cache.LegacyCache.LocationSearch.GetPath(key.Item2).Select(i => i.Id).ToArray();

                cq = from c in cq
                     where dbContext.CompanyOffices
                         .Any(co => co.Company == c.Id && locIds.Contains(co.Location))
                     select c;
            }

            // Port of legacy FeaturedCompanyCache.FetchItems:
            // Use CROSS APPLY pattern (skill guideline #10) to let SQL Server:
            //   1. Filter companies that have a Default_Image media entry (CROSS APPLY = no DefaultIfEmpty)
            //   2. Sort by Created DESC and project only Id + Metadata (never fetch the blob body)
            //   3. Bring only the top ~2000 candidates to C# for ImageSize resolution
            // This replaces the prior two-step anti-pattern that fetched ALL matching companies
            // into C# memory before sorting and then issued N+1 CompanyMedia queries.
            var qt = (from b in cq
                      from media in dbContext.CompanyMedia
                          .Where(m => m.Company == b.Id && m.Type == (byte)BizSrt.Model.MediaType.Default_Image)
                          .Select(m => new { m.Metadata })
                          .Take(1)                        // CROSS APPLY (no DefaultIfEmpty) — drops companies with no media
                      orderby b.Created descending
                      select new { b.Id, media.Metadata })
                     .Take(2000)                          // SQL-side cap: prevent unbounded fetch on massive tables
                     .AsEnumerable();                     // Stream remainder of work to C#

            // Resolve ImageSize from Metadata blob locally (never load the full blob via SQL)
            return qt
                .Where(b => b.Metadata != null && b.Metadata.Length > 0 && BizSrt.Foundation.Entity.Image.ResolveSize(BizSrt.Model.ImageEntity.Company, b.Metadata) > 0)
                .Select(b => b.Id)
                .Take(100)
                .ToArray();
        }
    }
}
