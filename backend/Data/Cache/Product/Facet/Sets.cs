using System;
using System.Linq;
using BizSrt.Data.Entities;
using BizSrt.Foundation.Cache;
using BizSrt.Data;

namespace BizSrt.Api.Data.Cache.Product.Facet
{
    public class SetsCache : TwoKeyExpirationCache<int, CachedSet.Key, CachedSet, AppDbContext>
    {
        public SetsCache()
            : base((int key1) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                var q = from pfs in dc.CompanyProductFacetSets
                        where pfs.Id == key1
                        select new CachedSet { UseCount = pfs.UseCount, LastUsed = pfs.LastUsed, Indexed = (pfs.Indexed != null) };
                return q.Single();
            }, (CachedSet.Key key2, bool exists) =>
            {
                if (key2 != null && key2.Value.Length > 1)
                {
                    using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                    var q = from pfs in dc.CompanyProductFacetSets
                            where pfs.Key == key2.Value 
                            select pfs.Id;
                    if (exists)
                        return q.Single();
                    else
                        return q.SingleOrDefault();
                }
                else
                    throw new InvalidOperationException();
            }, (AppDbContext dc, CachedSet.Key key2, object data) =>
            {
                var searchInput = data as BizSrt.Model.Product.SearchInput;
                if (key2 != null && key2.Value.Length > 1 && searchInput != null && ((searchInput.InclFacets != null && searchInput.InclFacets.NoFilters > 0) || (searchInput.ExclFacets != null && searchInput.ExclFacets.NoFilters > 0)))
                {
                    var pfs = new CompanyProductFacetSet();
                    pfs.Key = key2.Value; 
                    pfs.InclFacets = (byte)(searchInput.InclFacets != null ? searchInput.InclFacets.NoFilters : 0);
                    pfs.UseCount = 0;
                    pfs.LastUsed = DateTime.UtcNow;

                    dc.CompanyProductFacetSets.Add(pfs);

                    if (searchInput.InclFacets != null && searchInput.InclFacets.NoFilters > 0)
                    {
                        for (int i = 0; i < searchInput.InclFacets.NoFilters; i++)
                        {
                            dc.CompanyProductFacetSetDetails.Add(new CompanyProductFacetSetDetail { CompanyProductFacetSet = pfs, Value = searchInput.InclFacets.FilterValues[i], Exclude = false });
                        }
                    }

                    if (searchInput.ExclFacets != null && searchInput.ExclFacets.NoFilters > 0)
                    {
                        for (int i = 0; i < searchInput.ExclFacets.NoFilters; i++)
                        {
                            dc.CompanyProductFacetSetDetails.Add(new CompanyProductFacetSetDetail { CompanyProductFacetSet = pfs, Value = searchInput.ExclFacets.FilterValues[i], Exclude = true });
                        }
                    }

                    return pfs;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Api.Data.Cache.LegacyCache.GetDbContext(), 100)
        { }

        protected override int Created(AppDbContext dc, BizSrt.Foundation.Cache.IKey<int> set, object data)
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using var localDc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                    await BizSrt.Api.Process.Company.IndexProductFacetSetAsync(localDc, set.Key, default);
                }
                catch (Exception ex)
                {
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
                                var facetSet = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(dc.CompanyProductFacetSets, las => las.Id == setId);
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
