using BizSrt.Api.Foundation.Cache;
using System.Linq;

using BizSrt.Api.Data.Entities;
using BizSrt.Api.Data;
using BizSrt.Api.Model;
using BizSrt.Api.Foundation.Cache;

namespace BizSrt.Api.Data
{
    internal class LocationSearchCache : GroupSearchCache<int, int>
    {
        internal LocationSearchCache()
            : base(
            (GroupSearchCache<int> lk) =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    var q = (IQueryable<BizSrt.Api.Data.Entities.Location>) dc.Locations;
                    if(lk.Name.Length < 3)
                        q = q.Where(l => l.Name.StartsWith(lk.Name));
                    else
                        q = q.Where(l => l.Name.Contains(lk.Name));
                    if (lk.Parent > 0)
                        q = (from l in q
                             where l.Id == lk.Parent || dc.Locations_Unwound.Any(lu => lu.Parent == lk.Parent && lu.Child == l.Id)
                             /*join lu in dc.Locations_Unwound on new { Parent = lk.Parent, Child = l.Id } equals new { lu.Parent, lu.Child } into lut
                             from lu in lut.DefaultIfEmpty()
                             where l.Id == lk.Parent || lu != null*/
                             select l);
                    return q.Where(l => l.Id > 0).Select(l => l.Id).ToArray();
                }
            })
        { }
    }
}
