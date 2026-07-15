using System.Linq;

using BizSrt.Data.Entities;
using BizSrt.Data;
using BizSrt.Model;
using BizSrt.Foundation.Cache;

namespace BizSrt.Data
{
    internal class CategorySearchCache : GroupSearchCache<short, short>
    {
        internal CategorySearchCache()
            : base(
            (GroupSearchCache<short> ck) =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    var q = (IQueryable<BizSrt.Data.Entities.Category>) dc.Categories;
                    if(ck.Name.Length < 3)
                        q = q.Where(c => c.Name.StartsWith(ck.Name));
                    else
                        q = q.Where(c => c.Name.Contains(ck.Name));
                    if (ck.Parent > 0)
                        q = (from c in q
                             where c.Id == ck.Parent || dc.Categories_Unwound.Any(cu => cu.Parent == ck.Parent && cu.Child == c.Id)
                             select c);
                    return q.Where(c => c.Id > 0).Select(c => c.Id).ToArray();
                }
            })
        { }
    }
}
