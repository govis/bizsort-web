using BizSrt.Api.Foundation.Cache;
using System;
using System.Collections.Generic;
using System.Linq;

using BizSrt.Api.Data.Entities;
using BizSrt.Api.Data;
using BizSrt.Api.Model;
using BizSrt.Api.Model.Group;

namespace BizSrt.Api.Data.Cache.Location
{
    internal class LocationSettingsCache : ReadAllCache<int, CachedLocationSettings>
    {
        internal LocationSettingsCache()
            : base(() =>
            {
                using (var dc = BizSrt.Api.Data.Cache.LegacyCache.GetDbContext())
                {
                    // Mock query since LocationSettings doesn't exist in DB
                    return new KeyValuePair<int, CachedLocationSettings>[0];
                }
            }) { }


        protected string stringValue(string value)
        {
            return(!string.IsNullOrWhiteSpace(value) ? value : null);
        }

        public new LocationSettings this[int location]
        {
            get
            {
                List<byte> types = new List<byte>(Enum.GetValues(typeof(CachedLocationSettings.Type)).Cast<byte>());
                var locationSettings = new LocationSettings { Id = location };
                var cachedLocation = base[location, ReadAllSuppress.RecordNotFound];
                while (types.Count > 0 && location >= 0)
                {
                    if (cachedLocation != null)
                    {
                        var t2 = new List<byte>(types);
                        foreach (CachedLocationSettings.Type type in t2)
                        {
                            switch (type)
                            {
                                case CachedLocationSettings.Type.CountyRequired:
                                    if (cachedLocation.CountyRequired.HasValue)
                                    {
                                        locationSettings.CountyRequired = cachedLocation.CountyRequired.Value;
                                        types.Remove((byte)type);
                                    }
                                    break;
                            }
                        }
                    }
                    if (location > 0)
                    {
                        var parent = LegacyCache.Locations[location].ParentKey;
                        if (parent >= 0)
                        {
                            location = parent;
                            cachedLocation = base[location, ReadAllSuppress.RecordNotFound];
                        }
                        else
                            location = -1;
                    }
                    else
                        location = -1;
                }
                return locationSettings;
            }
        }
    }

    internal class CachedLocationSettings
    {
        public enum Type : byte
        {
            CountyRequired = 1
        }

        public CachedLocationSettings(IGrouping<int, object> settings) // Mock object grouping
        {
            /*
            foreach (var setting in settings)
            {
                switch ((Type)setting.Setting)
                {
                    case Type.CountyRequired:
                        bool cr;
                        if (bool.TryParse(setting.Value, out cr))
                            CountyRequired = cr;
                        break;
                }
            }
            */
        }

        public bool? CountyRequired { get; set; }
    }
}
