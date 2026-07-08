using System;
using System.Linq;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Foundation.Cache;
using BizSrt.Api.Data;

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
                if (key2 != null && key2.Value.Length > 1)
                {
                    var pfs = new CompanyProductFacetSet();
                    pfs.Key = key2.Value; 
                    pfs.InclFacets = 0; 
                    pfs.UseCount = 0;
                    pfs.LastUsed = DateTime.Now;
                    dc.CompanyProductFacetSets.Add(pfs);
                    return pfs;
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
