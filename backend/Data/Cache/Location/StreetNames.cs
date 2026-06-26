using BizSrt.Api.Foundation.Cache;
using System;
using System.Linq;

using BizSrt.Api.Data.Entities;
using BizSrt.Api.Data;

using BizSrt.Api.Foundation.Cache;

namespace BizSrt.Api.Data
{
    internal class StreetNamesCache : GroupCache<CachedStreetName.GroupKey, int, CachedStreetName.NameKey, CachedStreetName, byte, BizSrt.Api.Data.AppDbContext>
    {
        internal StreetNamesCache()
            : base(
            (CachedStreetName.GroupKey sng) =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    //var firstLetter = new string(new char[] { sng.FirstLetter });
                    var q = from sn in dc.StreetNames
                            where sn.City == sng.City //&& sn.LocationType == (byte)LocationType.City &&
                            //Caching by City and then filtering by FirstLetter using Linq
                            //sn.Name.Substring(0, 1) == firstLetter //done in the override below
                            select new CachedStreetName { City = sn.City, Id = sn.Id, Name = sn.Name };
                    return q.ToArray();
                }
            },
            (int streetNameId) =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    return (from sn in dc.StreetNames
                            where sn.Id == streetNameId
                            select new CachedStreetName { City = sn.City, Id = sn.Id, Name = sn.Name }).SingleOrDefault();
                }
            }, (CachedStreetName.NameKey key2, bool exists) =>
            {
                if (key2.City > 0 && !string.IsNullOrWhiteSpace(key2.Name))
                {
                    using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                    {
                        var q = from sn in dc.StreetNames
                                where sn.City == key2.City &&
                                //sn.LocationType == (byte)LocationType.City &&
                                sn.Name == key2.Name //String.Compare(k.Value, keyword, true) == 0
                                select sn.Id;
                        if (exists)
                            return q.Single();
                        else
                            return q.SingleOrDefault();
                    }
                }
                else
                    throw new InvalidOperationException();
            }, (BizSrt.Api.Data.AppDbContext dc, CachedStreetName.NameKey key2, object data) =>
            {
                if (!string.IsNullOrWhiteSpace(key2.Name))
                {
                    var sn = new StreetName();
                    sn.City = key2.City;
                    //sn.LocationType = (byte)LocationType.City;
                    sn.Name = key2.Name;
                    dc.StreetNames.Add(sn);
                    return sn;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
        { }

        public override CachedStreetName[] this[CachedStreetName.GroupKey groupKey, byte memberTypes]
        {
            get
            {
                var streetNames = base[groupKey, memberTypes];
                if (groupKey.FirstLetter != '\0')
                    return streetNames.Where(sn => char.ToUpperInvariant(sn.Name[0]) == groupKey.FirstLetter).ToArray();
                else
                    return streetNames;
            }
        }

        protected override int Created(BizSrt.Api.Data.AppDbContext dc, BizSrt.Api.Foundation.Cache.IKey<int> streetName, object data)
        {
            DropGroup((CachedStreetName.GroupKey)data);
            return base.Created(dc, streetName, data);
        }
    }

    internal class CachedStreetName : BizSrt.Api.Foundation.Cache.IKey<int>, BizSrt.Api.Foundation.Cache.IMemberType<byte>
    {
        protected internal CachedStreetName() { }

        protected internal CachedStreetName(int city, int id, string name)
        {
            City = city;
            Id = id;
            Name = name;
        }

        int BizSrt.Api.Foundation.Cache.IKey<int>.Key
        {
            get { return Id; }
        }

        bool BizSrt.Api.Foundation.Cache.IMemberType<byte>.OfType(byte type)
        {
            return true;
        }

        public int City
        {
            get;
            protected internal set;
        }

        public int Id
        {
            get;
            protected internal set;
        }

        public string Name
        {
            get;
            protected internal set;
        }

        //Caching by City and then filtering by FirstLetter using Linq
        public struct GroupKey
        {
            //public GroupKey(int city, char firstLetter)
            //{
            //    City = city;
            //    FirstLetter = firstLetter;
            //}

            public int City
            {
                get;
                set;
            }

            char _firstLetter;
            public char FirstLetter
            {
                get { return _firstLetter; }
                set
                {
                    if (char.IsLetterOrDigit(value))
                        _firstLetter = char.ToUpperInvariant(value);
                    else if (value == '\0')
                        _firstLetter = value;
                    else
                        throw new ArgumentException("FirstLetter");
                }
            }

            public override int GetHashCode()
            {
                return City.GetHashCode(); //^ FirstLetter.GetHashCode()
            }

            public override bool Equals(object obj)
            {
                if (obj is GroupKey)
                {
                    var key = (GroupKey)obj;
                    return City == key.City; //&& FirstLetter == key.FirstLetter
                }
                else
                    return false;
            }
        }

        public struct NameKey
        {
            //public NameKey(int city, string name)
            //{
            //    City = city;
            //    Name = name;
            //}

            public int City
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
                return City.GetHashCode() ^ _lowerName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is NameKey)
                {
                    var key = (NameKey)obj;
                    return City == key.City && _lowerName == key._lowerName;
                }
                else
                    return false;
            }
        }
    }
}
