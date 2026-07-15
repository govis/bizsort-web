using System;
using System.Linq;
using BizSrt.Data.Entities;
using BizSrt.Foundation.Cache;
using BizSrt.Data;

namespace BizSrt.Api.Data.Cache.Company.Facet
{
    public class NamesCache : TwoKeyCache<short, string, string, AppDbContext>
    {
        public NamesCache()
            : base((short key1) =>
            {
                using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                var q = from bfn in dc.CompanyFacetNames
                        where bfn.Id == key1
                        select bfn.Name;
                return q.Single();
            }, (string key2, bool exists) =>
            {
                if (!string.IsNullOrWhiteSpace(key2))
                {
                    using var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext();
                    var q = from bfn in dc.CompanyFacetNames
                            where bfn.Name == key2 
                            select bfn.Id;
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
                    var bfn = new CompanyFacetName();
                    bfn.Name = BizSrt.Foundation.TextConverter.Normalize(name) ?? name;
                    dc.CompanyFacetNames.Add(bfn);
                    return bfn;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
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
