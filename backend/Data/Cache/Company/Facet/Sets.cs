using System;
using System.Linq;
using BizSrt.Data.Entities;
using BizSrt.Foundation.Cache;
using BizSrt.Data;

namespace BizSrt.Api.Data.Cache.Company.Facet
{
    public class SetsCache : TwoKeyExpirationCache<int, CachedSet.Key, CachedSet, AppDbContext>
    {
        public SetsCache()
            : base((int key1) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                var q = from bfs in dc.CompanyFacetSets
                        where bfs.Id == key1
                        select new CachedSet { UseCount = bfs.UseCount, LastUsed = bfs.LastUsed, Indexed = (bfs.Indexed != null) };
                return q.Single();
            }, (CachedSet.Key key2, bool exists) =>
            {
                if (key2 != null && key2.Value.Length > 1)
                {
                    using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                    var q = from bfs in dc.CompanyFacetSets
                            where bfs.Key == key2.Value 
                            select bfs.Id;
                    if (exists)
                        return q.Single();
                    else
                        return q.SingleOrDefault();
                }
                else
                    throw new InvalidOperationException();
            }, (AppDbContext dc, CachedSet.Key key2, object data) =>
            {
                var searchInput = data as BizSrt.Model.Company.SearchInput;
                if (key2 != null && key2.Value.Length > 1 && searchInput != null && ((searchInput.InclFacets != null && searchInput.InclFacets.NoFilters > 0) || (searchInput.ExclFacets != null && searchInput.ExclFacets.NoFilters > 0)))
                {
                    var cfs = new CompanyFacetSet();
                    cfs.Key = key2.Value; 
                    cfs.InclFacets = (byte)(searchInput.InclFacets != null ? searchInput.InclFacets.NoFilters : 0);
                    cfs.UseCount = 0;
                    cfs.LastUsed = DateTime.UtcNow;

                    dc.CompanyFacetSets.Add(cfs);

                    if (searchInput.InclFacets != null && searchInput.InclFacets.NoFilters > 0)
                    {
                        for (int i = 0; i < searchInput.InclFacets.NoFilters; i++)
                        {
                            dc.CompanyFacetSetDetails.Add(new CompanyFacetSetDetail { CompanyFacetSet = cfs, Value = searchInput.InclFacets.FilterValues[i], Exclude = false });
                        }
                    }

                    if (searchInput.ExclFacets != null && searchInput.ExclFacets.NoFilters > 0)
                    {
                        for (int i = 0; i < searchInput.ExclFacets.NoFilters; i++)
                        {
                            dc.CompanyFacetSetDetails.Add(new CompanyFacetSetDetail { CompanyFacetSet = cfs, Value = searchInput.ExclFacets.FilterValues[i], Exclude = true });
                        }
                    }

                    return cfs;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Api.Data.Cache.LegacyCache.GetDbContext(), 100)
        { }

        protected override int Created(AppDbContext dc, BizSrt.Foundation.Cache.IKey<int> set, object data)
        {
            // Fire and forget the background indexing locally (no gRPC required for local caching calculations)
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using var localDc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                    await BizSrt.Api.Process.Company.IndexCompanyFacetSetAsync(localDc, set.Key, default);
                }
                catch (Exception ex)
                {
                    // TODO: Log exception
                    Console.WriteLine(ex);
                }
            });

            return 0;
        }

        public override CachedSet this[int setId]
        {
            get
            {
                var set = base[setId];
                if (set != null && set.Indexed)
                {
                    set.UseCount += 1;

                    if ((set.UseCount % 10) == 0 || set.LastUsed < DateTime.UtcNow.AddHours(-1))
                    {
                        set.LastUsed = DateTime.UtcNow;

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                                var facetSet = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(dc.CompanyFacetSets, las => las.Id == setId);
                                if (facetSet != null)
                                {
                                    facetSet.UseCount = set.UseCount;
                                    facetSet.LastUsed = set.LastUsed;
                                    await dc.SaveChangesAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        });
                    }
                    else
                    {
                        set.LastUsed = DateTime.UtcNow;
                    }
                }
                return set;
            }
        }
    }

    public class CachedSet : BizSrt.Foundation.Cache.IExpirationItem
    {
        public int HitCount { get; set; }
        public int LastHit { get; set; }
        public int UseCount { get; set; }
        public DateTime LastUsed { get; set; }
        public bool Indexed { get; set; }
        
        public class Key
        {
            public byte[] Value { get; set; } = Array.Empty<byte>();
            public Key(byte[] value) { Value = value; }
        }
    }
}
