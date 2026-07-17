using BizSrt.Foundation.Cache;
using System.Linq;

using BizSrt.Data.Entities;
using BizSrt.Data;
using BizSrt.Model;
using BizSrt.Foundation.Cache;

namespace BizSrt.Data
{
    internal class LocationSearchCache : GroupSearchCache<int, int>
    {
        internal LocationSearchCache()
            : base(
            (GroupSearchCache<int> lk) =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    var q = (IQueryable<BizSrt.Data.Entities.Location>) dc.Locations;
                    if(lk.Name.Length < 3)
                        q = q.Where(l => l.Name.StartsWith(lk.Name));
                    else
                        q = q.Where(l => l.Name.Contains(lk.Name));
                    if (lk.Parent > 0)
                    {
                        var parentIds = dc.Locations_Unwound.Where(lu => lu.Parent == lk.Parent).Select(lu => lu.Child);
                        q = (from l in q
                             where l.Id == lk.Parent || parentIds.Contains(l.Id)
                             select l);
                    }
                    return q.Where(l => l.Id > 0).Select(l => l.Id).ToArray();
                }
            })
        { }
    }
}
