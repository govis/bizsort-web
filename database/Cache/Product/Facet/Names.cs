using System;
using System.Linq;
using BizSrt.Data.Entities;
using BizSrt.Foundation.Cache;
using BizSrt.Data;

namespace BizSrt.Data.Cache.Product.Facet
{
    public class NamesCache : TwoKeyCache<short, string, string, AppDbContext>
    {
        public NamesCache()
            : base((short key1) =>
            {
                using var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext();
                var q = from pfn in dc.CompanyProductFacetNames
                        where pfn.Id == key1
                        select pfn.Name;
                return q.Single();
            }, (string key2, bool exists) =>
            {
                if (!string.IsNullOrWhiteSpace(key2))
                {
                    using var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext();
                    var q = from pfn in dc.CompanyProductFacetNames
                            where pfn.Name == key2 
                            select pfn.Id;
                    if (exists)
                        return q.Single();
                    else
                        return q.SingleOrDefault();
                }
                else
                    throw new InvalidOperationException();
            }, (AppDbContext dc, string key2, object data) =>
            {
                var name = data as string;
                if (key2 != null && !string.IsNullOrWhiteSpace(key2) && !string.IsNullOrWhiteSpace(name))
                {
                    var pfn = new CompanyProductFacetName();
                    pfn.Name = BizSrt.Foundation.TextConverter.Normalize(name) ?? name;
                    dc.CompanyProductFacetNames.Add(pfn);
                    return pfn;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Data.Cache.LegacyCache.GetDbContext())
        { }

        public override short this[string name]
        {
            get
            {
                name = name.Trim();
                return base[name.ToLower(), TwoKeySuppress.None, name];
            }
        }
    }
}
