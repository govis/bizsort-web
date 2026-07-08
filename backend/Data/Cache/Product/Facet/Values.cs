using System;
using System.Linq;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Foundation.Cache;
using BizSrt.Api.Data;

namespace BizSrt.Api.Data.Cache.Product.Facet
{
    public class ValuesCache : TwoKeyCache<int, CachedValue.Key, CachedValue, AppDbContext>
    {
        public ValuesCache()
            : base((int key1) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                var q = from pfv in dc.CompanyProductFacetValues
                        where pfv.Id == key1
                        select new CachedValue { Text = pfv.Text };
                return q.Single();
            }, (CachedValue.Key key2, bool exists) =>
            {
                if (key2 != null && key2.Value.Length > 0)
                {
                    using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                    var value = key2.Value; 
                    var q = from pfv in dc.CompanyProductFacetValues
                            where pfv.Name == key2.Name && pfv.ValueType == key2.ValueType && pfv.Value == value
                            select pfv.Id;
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
                    var pfv = new CompanyProductFacetValue();
                    pfv.Name = key2.Name;
                    pfv.ValueType = key2.ValueType;
                    pfv.Value = key2.Value; 
                    pfv.Text = text; 
                    dc.CompanyProductFacetValues.Add(pfv);
                    return pfv;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
        { }
    }
    
    public class CachedValue : BizSrt.Api.Foundation.Cache.IExpirationItem
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
