using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Model;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Foundation.Cache;
using Microsoft.Extensions.DependencyInjection;

namespace BizSrt.Api.Data.Cache.Company
{
    public class FeaturedCompaniesCache : FolderItemCache<Tuple<short, int>, int[]>
    {
        private ConcurrentDictionary<Tuple<short, int>, DateTime> _dirtyStamps;

        public FeaturedCompaniesCache()
        {
            _dirtyStamps = new ConcurrentDictionary<Tuple<short, int>, DateTime>();
        }

        internal virtual void MarkDirty(Tuple<short, int> folder)
        {
            DateTime dirtyStamp;
            Tuple<short, int> dirtyFolder;
            
            var category = LegacyCache.Categories[folder.Item1] as BizSrt.Api.Data.CachedCategory; 
            var locationNode = LegacyCache.Locations[folder.Item2] as BizSrt.Api.Data.Cache.Location.CachedLocation; 
            
            var locations = new System.Collections.Generic.List<BizSrt.Api.Data.Cache.Location.CachedLocation>();
            while (locationNode != null && locationNode.Id > 0)
            {
                locations.Add(locationNode);
                locationNode = locationNode.Parent as BizSrt.Api.Data.Cache.Location.CachedLocation;
            }

            do
            {
                foreach (var l in locations)
                {
                    dirtyFolder = new Tuple<short, int>(category.Id, l.Id);
                    if (!_dirtyStamps.TryGetValue(dirtyFolder, out dirtyStamp))
                        _dirtyStamps.TryAdd(dirtyFolder, DateTime.Now);
                }
                category = category.Parent as BizSrt.Api.Data.CachedCategory;
            }
            while (category != null);
        }

        public int[] this[Tuple<short, int> folder, bool checkDirty]
        {
            get
            {
                DateTime dirtyStamp;
                if (checkDirty && _dirtyStamps.TryGetValue(folder, out dirtyStamp) && dirtyStamp < DateTime.Now.AddMinutes(-10))
                {
                    _dirtyStamps.TryRemove(folder, out dirtyStamp);
                    int[] folderItems;
                    if (_folderItems.TryRemove(folder, out folderItems))
                        return folderItems;
                }
                return base[folder];
            }
        }

        protected override int[] FetchItems(Tuple<short, int> key)
        {
            using var dbContext = LegacyCache.GetDbContext();
            
            var cq = from c in dbContext.CompanyProfiles
                     join a in dbContext.Accounts on c.Id equals a.Id
                     where a.Status == 2 // Active
                     select c;

            if (key.Item1 > 0)
            {
                cq = from c in cq
                     where c.Category == key.Item1 || dbContext.Categories_Unwound.Any(cu => cu.Parent == key.Item1 && cu.Child == c.Category)
                     select c;
            }

            if (key.Item2 > 0)
            {
                var coq = from co in dbContext.CompanyOffices
                          join lu in dbContext.Locations_Unwound on new { Parent = key.Item2, Child = co.Location } equals new { lu.Parent, lu.Child } into lut
                          from lu in lut.DefaultIfEmpty()
                          where co.Location == key.Item2 || lu != null
                          select co;

                cq = (from c in cq
                      join co in coq on c.Id equals co.Company
                      select c).Distinct(); 
            }

            var qt = (from b in cq
                      let bi = dbContext.CompanyMedia.FirstOrDefault(bi => bi.Company == b.Id && bi.Type == 1) // 1 = Default_Image
                      where bi != null
                      orderby b.Created descending
                      select new { b.Id, bi.Metadata }).AsEnumerable();

            // Image size resolution requires Foundation.Image logic, which we port as simple check for now
            return qt.Where(b => b.Metadata != null && b.Metadata.Length > 0).Select(b => b.Id).Take(100).ToArray();
        }
    }
}
