using System;
using System.Linq;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Foundation.Cache;
using BizSrt.Api.Data;

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
                // In legacy, this used QueryInput for InclFacets, but modern port simplifies this or omits tracking 
                // We will just do a standard insert
                if (key2 != null && key2.Value.Length > 1)
                {
                    var cfs = new CompanyFacetSet();
                    cfs.Key = key2.Value; 
                    cfs.InclFacets = 0; 
                    cfs.UseCount = 0;
                    cfs.LastUsed = DateTime.Now;
                    dc.CompanyFacetSets.Add(cfs);
                    return cfs;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Api.Data.Cache.LegacyCache.GetDbContext(), 100)
        { }
    }

    public class CachedSet : BizSrt.Api.Foundation.Cache.IExpirationItem
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
