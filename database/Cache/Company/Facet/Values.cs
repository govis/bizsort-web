using System;
using System.Linq;
using BizSrt.Data.Entities;
using BizSrt.Foundation.Cache;
using BizSrt.Data;

namespace BizSrt.Data.Cache.Company.Facet
{
    public class ValuesCache : TwoKeyCache<int, CachedValue.Key, CachedValue, AppDbContext>
    {
        public ValuesCache()
            : base((int key1) =>
            {
                using var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext();
                var q = from bfv in dc.CompanyFacetValues
                        where bfv.Id == key1
                        select new CachedValue { Text = bfv.Text };
                return q.Single();
            }, (CachedValue.Key key2, bool exists) =>
            {
                if (key2 != null && key2.Value.Length > 0)
                {
                    using var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext();
                    var value = key2.Value; 
                    var q = from bfv in dc.CompanyFacetValues
                            where bfv.Name == key2.Name && bfv.ValueType == key2.ValueType && bfv.Value == value
                            select bfv.Id;
                    if (exists)
                        return q.Single();
                    else
                        return q.SingleOrDefault();
                }
                else
                    throw new InvalidOperationException();
            }, (AppDbContext dc, CachedValue.Key key2, object data) =>
            {
                var text = data as string;
                if (key2 != null && key2.Value.Length > 0 && !string.IsNullOrWhiteSpace(text))
                {
                    var bfv = new CompanyFacetValue();
                    bfv.Name = key2.Name;
                    bfv.ValueType = key2.ValueType;
                    bfv.Value = key2.Value; 
                    bfv.Text = text; 
                    dc.CompanyFacetValues.Add(bfv);
                    return bfv;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Data.Cache.LegacyCache.GetDbContext())
        { }
    }
    
    public class CachedValue : BizSrt.Foundation.Cache.IExpirationItem
    {
        public int HitCount { get; set; }
        public int LastHit { get; set; }
        public string Text { get; set; } = string.Empty;
        public int UseCount { get; set; }
        public DateTime LastUsed { get; set; }
        
        public class Key
        {
            public short Name { get; set; }
            public byte ValueType { get; set; }
            public byte[] Value { get; set; } = Array.Empty<byte>();
        }
    }
}
