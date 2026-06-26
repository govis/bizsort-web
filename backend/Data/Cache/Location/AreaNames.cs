using System;
using System.Collections.Generic;
using System.Linq;
using BizSrt.Api.Foundation.Cache;

namespace BizSrt.Api.Data
{
    internal class AreaNamesCache : ReadAllCache<CachedAreaName.NameKey, CachedAreaName>
    {
        internal AreaNamesCache()
            : base(() =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    var q = (from an in dc.AreaNames
                             select new { an.Id, an.Location, an.Name }).AsEnumerable();
                    return q.Select(an => new KeyValuePair<CachedAreaName.NameKey, CachedAreaName>(new CachedAreaName.NameKey { Location = an.Location, Name = an.Name }, new CachedAreaName { Id = an.Id })).ToArray();
                }
            }) { }

        public CachedAreaName this[int parentLocation, string areaName]
        {
            get
            {
                return base[new CachedAreaName.NameKey { Location = parentLocation, Name = areaName }, ReadAllSuppress.RecordNotFound];
            }
        }

        public KeyValuePair<int, string>[] this[int location]
        {
            get
            {
                return (from k in _cache.Keys
                        where k.Location == location
                        select new KeyValuePair<int, string>(_cache[k].Id, k.Name)).ToArray();
            }
        }
    }

    internal class CachedAreaName
    {
        protected internal CachedAreaName() { }

        public struct NameKey
        {
            //public NameKey(int city, string name)
            //{
            //    City = city;
            //    Name = name;
            //}

            public int Location
            {
                get;
                set;
            }

            string _name;
            string _lowerName;
            public string Name
            {
                get { return _name; }
                set
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _name = value.Trim();
                        _lowerName = _name.ToLower();
                    }
                    else
                        throw new ArgumentNullException("Name");
                }
            }

            public override int GetHashCode()
            {
                return Location.GetHashCode() ^ _lowerName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is NameKey)
                {
                    var key = (NameKey)obj;
                    return Location == key.Location && _lowerName == key._lowerName;
                }
                else
                    return false;
            }
        }

        public int Id
        {
            get;
            protected internal set;
        }
    }
}
