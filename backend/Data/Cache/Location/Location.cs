using LocationRef = BizSrt.Model.LocationRef;
using System;
using System.Linq;
using System.Collections.Generic;

using BizSrt.Data.Entities;
using BizSrt.Data;
using BizSrt.Foundation.Cache;
using System;

using BizSrt.Model;
using BizSrt.Model.Group;


namespace BizSrt.Api.Data.Cache.Location
{
    internal class LocationsCache : TreeCache<int, CachedLocation.NameKey, CachedLocation, byte, BizSrt.Data.AppDbContext>
    {
        internal LocationsCache()
            : base(
            (int locationId) =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    var lq = (from l in dc.Locations
                              where l.Parent == locationId //l.Parent != null && l.Parent.Value == locationId
                              orderby l.SortOrder, l.Name
                              select new { l.Id, l.Name, l.Type, l.SortOrder }).AsEnumerable();
                    return lq.Select(lt => new CachedLocation(lt.Id, lt.Name, (LocationType)lt.Type, locationId) { SortOrder = (byte)lt.SortOrder }).ToArray();
                }
            }, (int locationId) =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    var lt =  (from l in dc.Locations
                               where l.Id == locationId
                               select new { l.Id, l.Name, l.Type, l.SortOrder, l.Parent }).SingleOrDefault();
                    return lt != null ? new CachedLocation(lt.Id, lt.Name, (LocationType)lt.Type, (lt.Parent != null && lt.Parent.HasValue ? lt.Parent.Value : -1)) { SortOrder = (byte)lt.SortOrder } : null;
                }
            }, (CachedLocation.NameKey key2, bool exists) =>
            {
                if (!string.IsNullOrWhiteSpace(key2.Name))
                {
                    using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                    {
                        var q = from l in dc.Locations
                                where l.Parent == key2.Parent && l.Type == (byte)key2.Type && l.Name == key2.Name //String.Compare(k.Value, keyword, true) == 0
                                select l.Id;
                        if (exists)
                            return q.Single();
                        else
                            return q.SingleOrDefault();
                    }
                }
                else
                    throw new InvalidOperationException();
            }, (BizSrt.Data.AppDbContext dc, CachedLocation.NameKey key2, object data) =>
            {
                //Auto create states should be turned off
                if (!(key2.Type == LocationType.City))
                    throw new InvalidOperationException("Not supported");

                if (key2.Parent > 0 && !string.IsNullOrWhiteSpace(key2.Name))
                {
                    var l = new BizSrt.Data.Entities.Location();
                    l.Parent = key2.Parent;
                    l.Type = (byte)key2.Type;
                    l.Name = key2.Name;
                    l.UserDefined = true;
                    l.Created = DateTime.UtcNow;
                    dc.Locations.Add(l);
                    return l;
                }
                else
                    throw new InvalidOperationException();
            }, () => BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
        { }

        protected bool _updated = false;
        public bool Updated
        {
            get
            {
                return _updated;
            }
        }

        protected override int Created(BizSrt.Data.AppDbContext dc, BizSrt.Foundation.Cache.IKey<int> location, object data)
        {
            _updated = true;
            var parent = (int)data;
            DropGroup(parent);
            //Cache.LocationSearch.Reset();
            try
            {
                Location_Unwound lu;
                while (parent > 0)
                {
                    lu = new Location_Unwound();
                    lu.Parent = parent;
                    lu.Child = location.Key;
                    dc.Locations_Unwound.Add(lu);
                    parent = base[parent].ParentKey;
                }
                dc.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return base.Created(dc, location, data);
        }

        public override void Drop(int key)
        {
            base.Drop(key);
            //Cache.LocationSearch.Reset();
        }

        public void Drop(int[] locations, IEnumerable<int> parents)
        {
            foreach (var location in locations)
                base.Drop(location);
            foreach (var parent in parents)
                DropGroup(parent);
            //Cache.LocationSearch.Reset();
        }
    }

    internal class CachedLocation : CachedNode<int>
    {
        #region Part Cache
        /*[Flags]
        public enum PartType : ushort
        {
            UnwoundChildren = 1,
            All = 65535
        }

        public void Reset(PartType type)
        {
            if ((type & PartType.UnwoundChildren) > 0)
            {
                this.unwoundChildren = null;
            }
        }
        int[] unwoundChildren;
        public int[] UnwoundChildren
        {
            get
            {
                return base.GetArray<int, int>(ref unwoundChildren, this.Id, (location) =>
                {
                    using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                    {
                        return (from lu in dc.Locations_Unwound
                                where lu.Parent == location
                                select lu.Child).ToArray();
                    }
                });
            }
        }*/
        #endregion

        public struct NameKey
        {
            //public NameKey(int parent, LocationType type, string name)
            //{
            //    Parent = parent;
            //    Type = type;
            //    Name = name;
            //}

            public int Parent
            {
                get;
                set;
            }

            public LocationType Type
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
                return Parent.GetHashCode() ^ Type.GetHashCode() ^ _lowerName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is NameKey)
                {
                    var key = (NameKey)obj;
                    return Parent == key.Parent && Type == key.Type && _lowerName == key._lowerName;
                }
                else
                    return false;
            }
        }

        protected internal CachedLocation(int id, string name, LocationType type, int parent)
        {
            _id = id;
            _name = name;
            _type = (byte)type;
            _parentKey = parent;
        }

        public override CachedNode<int, byte> Parent
        {
            get
            {
                return (ParentKey >= 0 ? BizSrt.Api.Data.Cache.LegacyCache.Locations[ParentKey] : null);
            }
        }

        public override string DisplayText
        {
            get
            {
                return (ParentKey == -1 ? "Global" : Name);
            }
        }

        public override string DisplayPath
        {
            get
            {
                return (ParentKey > 0 ? Parent.DisplayPath + "\\" : string.Empty) + DisplayText;
            }
        }

        public string[] AutocompletePath(int scope/*, string path = null*/)
        {
            var path = new List<string>();
            var parent = (CachedLocation)Parent;
            while (parent != null && parent.Id != scope)
            {
                switch(parent.Type)
                {
                    case LocationType.State:
                    case LocationType.Country:
                        path.Add(parent.Name);
                        break;
                }
                parent = (CachedLocation)parent.Parent;
            }
            return path.Count > 0 ? path.ToArray() : null;

            /*var parentPath = ParentKey > 0 && ParentKey != scope ? ((CachedLocation)Parent).DisplayPath2(scope, path) : null;
            if (String.IsNullOrEmpty(parentPath))
                return DisplayText;
            else if (String.IsNullOrEmpty(path))
                return DisplayText + " in " + parentPath;
            else
                return DisplayText + " " + parentPath;*/
        }

        public override Node<int> ParentNode(byte type)
        {
            if (ParentKey >= 0)
            {
                var parentLocation = BizSrt.Api.Data.Cache.LegacyCache.Locations[ParentKey] as CachedLocation;
                return new Node<int> { Id = ParentKey, Name = parentLocation.Name, NodeType = parentLocation.NodeType(type), Parent = parentLocation.ParentNode(type) };
            }
            else
                return null;
        }

        public new LocationType Type
        {
            get { return (LocationType)_type; }
        }

        public override BizSrt.Model.Group.NodeType NodeType(byte type)
        {
            return (_type == (byte)LocationType.City ? BizSrt.Model.Group.NodeType.Class : BizSrt.Model.Group.NodeType.Super);
        }

        internal BizSrt.Model.LocationRef PopulateWithPath()
        {
            BizSrt.Model.LocationRef model = new BizSrt.Model.LocationRef { Id = _id, Name = _name, Type = Type };
            var parent = Parent as CachedLocation;
            if (parent != null)
            {
                if (parent.Type == LocationType.County)
                {
                    var locationSettings = BizSrt.Api.Data.Cache.LegacyCache.LocationSettings[0];
                    if(!locationSettings.CountyRequired)
                        parent = parent.Parent as CachedLocation;
                }
                model.Parent = parent.PopulateWithPath();
            }
            return model;
        }

        internal LocationRef EntityRef(BizSrt.Model.Group.DisplayType type)
        {
            var model = ToModel<LocationRef>(type);
            model.Type = Type;
            return model;
        }
    }
}
