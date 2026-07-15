using System;
using System.Linq;
using System.Collections.Generic;
using BizSrt.Model;
using BizSrt.Foundation.Cache;
using BizSrt.Data.Entities;

//namespace Entity
//{
//    public partial class Cache
//    {
//        public static readonly Foundation.Cache.SecurityProfileDictionary Dictionary;

//        private Cache()
//        {
//            Dictionary = new Foundation.Cache.SecurityProfileDictionary();
//        }
//    }
//}

namespace BizSrt.Data.Cache
{
    public class Dictionary : ReadOneCache<DictionaryType, DictionaryItem[]> //Dictionary<DictionaryType>
    {
        public static Dictionary SecurityProfile = new Dictionary();
        private Dictionary()
            : base((DictionaryType type) =>
            {
                if (type == DictionaryType.SecurityProfile)
                    return fetchSecurityProfiles(type);
                else
                    return null;
            })
        { }

        protected Dictionary(FetchOne<DictionaryItem[], DictionaryType> fetchOneMethod)
            : base(fetchOneMethod)
        { }

        protected static DictionaryItem[] fetchSecurityProfiles(DictionaryType type)
        {
            using (var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext())
            {
                /* Query with GroupBy or GroupJoin throws exception
                 * https://github.com/aspnet/EntityFrameworkCore/issues/17068
                 * 
                 * EF Core 2.2 LINQ query not working in EF Core 3.0
                 * https://stackoverflow.com/questions/58092869/ef-core-2-2-linq-query-not-working-in-ef-core-3-0
                 * 
                 * Support ability to select top N of each group
                 * https://github.com/aspnet/EntityFrameworkCore/issues/13805
                 * 
                 * QueryRewrite: add navigation expansion support for GroupBy
                 * https://github.com/aspnet/EntityFrameworkCore/issues/15249
                 */

                var profiles1 = from sp in dc.SecurityProfiles.AsEnumerable()
                                join spp in dc.SecurityProfilePriviledges.AsEnumerable() on sp.Id equals spp.Profile into sppt
                                from spp in sppt.DefaultIfEmpty()
                                group spp by sp into sppg
                                orderby sppg.Key.Id
                                select sppg;
                var profiles2 = new List<BizSrt.Model.SecurityProfile>();
                foreach (var sp in profiles1)
                {
                    var model = new BizSrt.Model.SecurityProfile { ItemKey = (BizSrt.Model.SecurityProfile.Type)sp.Key.Id, Name = sp.Key.Name };
                    populateSecurityProfile(model, sp, profiles1);
                    profiles2.Add(model);
                }
                return profiles2.ToArray();
            }
        }

        private static void populateSecurityProfile(BizSrt.Model.SecurityProfile model, IGrouping<BizSrt.Data.Entities.SecurityProfile, BizSrt.Data.Entities.SecurityProfilePriviledge> priviledges, IEnumerable<IGrouping<BizSrt.Data.Entities.SecurityProfile, BizSrt.Data.Entities.SecurityProfilePriviledge>> profiles)
        {
            if (priviledges.Key.ParentProfile.HasValue)
                populateSecurityProfile(model, profiles.SingleOrDefault(sp => sp.Key.Id == priviledges.Key.ParentProfile.Value), profiles);

            foreach (var spp in priviledges)
            {
                if (spp != null)
                    model[(BizSrt.Model.SecurityPriviledge)spp.Priviledge] = !spp.Restricted;
            }
        }

        public T[] Get<T>(DictionaryType type) where T : DictionaryItem
        {
            return this[type] as T[];
        }

        public T GetItem<T, K>(DictionaryType type, K id) where T : DictionaryItem<K>
        {
            return GetItem(type, id) as T;
        }

        public DictionaryItem<K> GetItem<K>(DictionaryType type, K id)
        {
            var dictionary = this[type];
            return (dictionary as DictionaryItem<K>[]).SingleOrDefault(di => id.Equals(di.ItemKey));
        }

        public string GetItemText<K>(DictionaryType type, K id)
        {
            return GetItem(type, id).ToString();
        }
    }

    /*public class Dictionary<TType> : ReadOneCache<TType, Model.DictionaryItem[]>
    {
        protected Dictionary(FetchOne<Model.DictionaryItem[], TType> fetchOneMethod)
            : base(fetchOneMethod)
        { }

        public T[] Get<T>(TType type) where T : Model.DictionaryItem
        {
            return this[type] as T[];
        }

        public T GetItem<T, K>(TType type, K id) where T : Model.DictionaryItem<K>
        {
            return GetItem(type, id) as T;
        }

        public Model.DictionaryItem<K> GetItem<K>(TType type, K id)
        {
            var dictionary = this[type];
            return (dictionary as Model.DictionaryItem<K>[]).SingleOrDefault(di => id.Equals(di.Id));
        }

        public string GetItemText<K>(TType type, K id)
        {
            return GetItem<K>(type, id).ToString();
        }
    }*/
}
