using System;
using System.Collections.Generic;
using System.Linq;

using BizSrt.Api.Data;
using BizSrt.Api.Data.Cache;

using BizSrt.Api.Data.Entities;
using BizSrt.Api.Foundation.Cache;
using System;
using BizSrt.Api.Model;
using BizSrt.Api.Model.Group;


namespace BizSrt.Api.Data
{
    public partial class Master
    {
        public static class Location
        {
            public static ResolvedLocation /*Interface.Master.ILocation.*/Resolve(Model.Geocoder.City city, string street, bool allowCreate)
            {
                if (city != null && !string.IsNullOrWhiteSpace(city.Country))
                {
                    return resolveLocation(city, street, allowCreate);
                }
                else
                    throw new ArgumentException("Invalid input.");
            }

            //New UI: X in Y
            public static Autocomplete<int>[] /*Interface.Master.ILocation.*/Autocomplete(int parentLocation, string name, Model.IdName<int> scope)
            {
                if (parentLocation >= 0 && !string.IsNullOrWhiteSpace(name))
                {
                    var locations = LegacyCache.LocationSearch[new GroupSearchCache<int> { Parent = parentLocation, Name = name }].Take(15);
                    return (from l in locations
                            let location = LegacyCache.Locations[l]
                            select new Autocomplete<int>
                            {
                                Id = l,
                                Name = location.Name,
                                Path = location.AutocompletePath(scope != null ? scope.Id : 0),
                                NodeType = location.NodeType(0),
                                HasChildren = LegacyCache.Locations.HasChildren(l, 0)
                            }).ToArray();
                }
                else
                    throw new ArgumentException("Invalid input.");
            }

            public static Model.Group.IdName<int> /*Interface.Master.ILocation.*/Get(int location)
            {
                if (location > 0)
                {
                    return LegacyCache.Locations[location].IdName;
                }
                else
                    throw new ArgumentException("Invalid input.");
            }

            public static LocationRef /*Interface.Master.ILocation.*/Get(int location, DisplayType type)
            {
                if (location > 0)
                {
                    return LegacyCache.Locations[location].EntityRef(type);
                }
                else
                    throw new ArgumentException("Invalid input.");
            }

            public static Model.Group.IdName<int>[] /*Interface.Master.ILocation.*/GetPath(int location, Model.IdName<int> scope)
            {
                if (location > 0)
                {
                    return LegacyCache.Locations[location].GetPath(scope);
                }
                else
                    throw new ArgumentException("Invalid input.");
            }

            static Dictionary<string, string> streetQualifiers = new Dictionary<string, string> {
                { "avenue", "Ave" }, { "boulevard", "Blvd" }, { "crescent", "Cres" }, { "drive", "Dr" }, { "east", "E" }, { "north", "N" }, { "northeast", "NE" }, { "northwest", "NW" }, { "road", "Rd" }, { "south", "S" }, { "southeast", "SE" }, { "southwest", "SW" }, { "street", "St" }, { "west", "W" }
            };

            internal static Model.Location Resolve(Model.Geocoder.City city, Model.Location location, int parentId, bool allowCreate)
            {
                switch (location.Type)
                {
                    case LocationType.Country:
                        location.Name = city.Country;
                        break;
                    case LocationType.State:
                        location.Name = city.State;
                        break;
                    case LocationType.County:
                        location.Name = city.County;
                        break;
                    case LocationType.City:
                        location.Name = city.Name;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                try
                {
                    //Try to resolve by alternative Area name
                    var areaLocation = resolveByArea(LegacyCache.Locations[parentId, 0], location.Name);
                    if (areaLocation == null)
                    {
                        location.Id = LegacyCache.Locations[new BizSrt.Api.Data.Cache.Location.CachedLocation.NameKey
                        {
                            Parent = parentId,
                            Type = location.Type,
                            Name = location.Name
                        }, //Auto create states should be turned off
                        (allowCreate && /*parent.Type == LocationType.City*/(location.Type == LocationType.City || (location.Type == LocationType.County && false) || (location.Type == LocationType.State && false)) ? TwoKeySuppress.None : allowCreate ? TwoKeySuppress.Create : TwoKeySuppress.Create | TwoKeySuppress.CreateNotAllowed), parentId];
                    }
                    else if (location.Type == areaLocation.Type)
                    {
                        location.Id = areaLocation.Id;
                        location.Name = areaLocation.Name;
                    }
                    else
                        throw new InvalidOperationException(string.Format("Unexpected location type {0}:{1}, expecting {2}", areaLocation.Type, areaLocation.Name, location.Type));
                }
                catch (Exception)
                {
                    throw new ArgumentException($"Invalid {location.Type}");
                }

                if (location.Type == LocationType.Country && location.Id == 0)
                    throw new ArgumentException("Invalid Country");

                BizSrt.Api.Model.LocationSettings locationSettings;
                var childType = LocationType.Unknown;
                string childName = null;
                switch (location.Type)
                {
                    case LocationType.Country:
                        childType = LocationType.State;
                        childName = city.State;
                        break;
                    case LocationType.State:
                        locationSettings = LegacyCache.LocationSettings[location.Id];
                        if ((locationSettings != null && locationSettings.CountyRequired) || resolveLocationGap(city, location.Id, LocationType.County, city.County) == LocationType.County)
                        {
                            childType = LocationType.County;
                            childName = city.County;
                        }
                        else
                        {
                            childType = LocationType.City;
                            childName = city.Name;
                        }
                        break;
                    case LocationType.County:
                        childType = LocationType.City;
                        childName = city.Name;
                        break;
                }

                if (childType != LocationType.Unknown && (!string.IsNullOrWhiteSpace(childName) || resolveLocationGap(city, location.Id, childType, childName) != LocationType.Unknown))
                    return Resolve(city, new Model.Location { Type = childType, Parent = location }, location.Id, allowCreate);
                else
                    return location;
            }

            internal static BizSrt.Api.Data.Cache.Location.CachedLocation resolveByArea(BizSrt.Api.Data.Cache.Location.CachedLocation[] locations, string areaName)
            {
                if (locations != null)
                {
                    foreach (var location in locations)
                    {
                        var area = LegacyCache.AreaNames[location.Id, areaName];
                        if (area != null)
                            return location;
                    }
                }
                return null;
            }

            internal static LocationType resolveLocationGap(Model.Geocoder.City city, int parentId, LocationType locationType, string locationName)
            {
                var locations = LegacyCache.Locations[parentId, 0];
                if (locations == null || locations.Length == 0)
                    return LocationType.Unknown;

                BizSrt.Api.Data.Cache.Location.CachedLocation areaLocation; string areaName;
                if (!string.IsNullOrWhiteSpace(locationName))
                {
                    foreach (var location in locations)
                    {
                        if (location.Type == locationType && location.Name == locationName)
                            return locationType;
                    }
                    areaName = locationName;
                }
                else if (locationType == LocationType.City)
                    areaName = city.Area;
                else
                    areaName = null;

                if (!string.IsNullOrWhiteSpace(areaName))
                {
                    //Try to resolve by alternative Area name
                    areaLocation = resolveByArea(locations, areaName);
                    if (areaLocation != null)
                    {
                        if (fillLocationValue(city, locationType, areaLocation, true))
                            return locationType;
                    }
                }

                BizSrt.Api.Model.LocationSettings locationSettings;
                var childType = LocationType.Unknown;
                string childName = null;
                switch (locationType)
                {
                    case LocationType.State:
                        locationSettings = LegacyCache.LocationSettings[parentId];
                        if (locationSettings != null && locationSettings.CountyRequired)
                        {
                            childType = LocationType.County;
                            childName = city.County;
                        }
                        else
                        {
                            childType = LocationType.City;
                            childName = city.Name;
                        }
                        break;
                    case LocationType.County:
                        childType = LocationType.City;
                        childName = city.Name;
                        break;
                }
                if (childType != LocationType.Unknown)
                {
                    foreach (var location in locations)
                    {
                        //When resolving the County
                        if (location.Type == LocationType.City)
                        {
                            //Handled by resolveByArea above
                            /*if (locationType == LocationType.City && string.IsNullOrWhiteSpace(city.Name) && !string.IsNullOrWhiteSpace(city.Area))
                            {
                                areaLocation = resolveByArea(new BizSrt.Api.Data.Cache.Location.CachedLocation[] { location }, city.Area);
                                if (areaLocation != null)
                                {
                                    if (fillLocationValue(city, LocationType.City, areaLocation, true))
                                        return LocationType.City;
                                }
                            }*/
                            continue;
                        }
                        if (resolveLocationGap(city, location.Id, childType, childName) != LocationType.Unknown)
                        {
                            if (fillLocationValue(city, locationType, location, !string.IsNullOrWhiteSpace(locationName)))
                                return locationType;
                        }
                    }
                    return LocationType.Unknown;
                }

                return LocationType.Unknown;
            }

            internal static bool fillLocationValue(Model.Geocoder.City city, LocationType locationType, BizSrt.Api.Data.Cache.Location.CachedLocation location, bool reset)
            {
                if (location.Type == locationType)
                {
                    switch (locationType)
                    {
                        case LocationType.State:
                            if (reset || string.IsNullOrWhiteSpace(city.State))
                            {
                                city.State = location.Name;
                                return true;
                            }
                            break;
                        case LocationType.County:
                            if (reset || string.IsNullOrWhiteSpace(city.County))
                            {
                                city.County = location.Name;
                                return true;
                            }
                            break;
                        case LocationType.City:
                            if (reset || string.IsNullOrWhiteSpace(city.Name))
                            {
                                city.Name = location.Name;
                                return true;
                            }
                            break;
                        default:
                            throw new System.ArgumentException(string.Format("Unexpected type: {0}", locationType, "locationType"));
                    }
                    throw new InvalidOperationException();
                }
                else
                    throw new InvalidOperationException(string.Format("Unexpected location type {0}:{1}, expecting {2}", location.Type, location.Name, locationType));
            }

            static ResolvedLocation resolveLocation(Model.Geocoder.City city, string street, bool allowCreate)
            {
                var location = Resolve(city, new Model.Location { Type = LocationType.Country }, 0, allowCreate);
                var partial = false;
                if (street != null && !string.IsNullOrWhiteSpace(street))
                {
                    if (location != null && location.Type == LocationType.City && location.Id > 0)
                    {
                        var streetId = resolveId(location.Id, street, allowCreate);
                        if (streetId > 0)
                            return new ResolvedLocation { Type = LocationType.Street, Id = streetId, Name = street, Parent = location };
                        else //if (!allowCreate) //Return Partially Resolved for Country specific formating, etc
                            partial = true; //return null;
                    }
                    else //if (!allowCreate) //Return Partially Resolved for Country specific formating, etc
                        partial = true; //return null;
                }
                else if (location.Type != LocationType.City) //Return Partially Resolved for Country specific formating, etc
                    partial = true; //return null;

                return location != null && location.Id > 0 ? new ResolvedLocation()
                {
                    Id = location.Id,
                    Name = location.Name,
                    Type = location.Type,
                    Parent = location.Parent,
                    Partial = partial
                } : null; //Return null if City could not be resolved
            }

            static int resolveId(int city, string streetName, bool allowCreate)
            {
                streetName = streetName.Trim();
                var shortened = false;
                var streetParts = streetName.Trim().Split(' ');
                for (var i = streetParts.Length - 1; i >= 0; i--)
                {
                    var streetPart = streetParts[i].ToLower();
                    if (streetQualifiers.ContainsKey(streetPart))
                    {
                        streetParts[i] = streetQualifiers[streetPart];
                        shortened = true;
                    }
                    else
                        break;
                }
                if (shortened)
                    streetName = string.Join(" ", streetParts);
                /*var idx = streetName.LastIndexOf(' ');
                if (idx > 0 && idx + 1 < streetName.Length && streetQualifiers.ContainsKey(streetQualifier = streetName.Substring(idx + 1).ToLower()))
                    streetName = streetName.Substring(0, idx + 1) + streetQualifiers[streetQualifier];*/
                return LegacyCache.StreetNames[new CachedStreetName.NameKey { City = city, Name = streetName }, allowCreate ? TwoKeySuppress.None : TwoKeySuppress.Create | TwoKeySuppress.CreateNotAllowed, new CachedStreetName.GroupKey { City = city, FirstLetter = streetName[0] }];
            }

            //Foundation.Controls.Location.Edit.Resolve
            /* public static Address AddressFromText(string textLocation, Model.Geocoder.Geolocation geoLocation = null, bool populatePath = false)
            {
                if (!string.IsNullOrWhiteSpace(textLocation))
                {
                    int dashIdx = textLocation.IndexOfAny(new char[] { '-', '–' }); string address1 = null;
                    //Look for xxx-yyy Street name
                    var oneThird = textLocation.Length / 3;
                    if (dashIdx > 0 && dashIdx < oneThird)
                    {
                        for (var i = 0; i < dashIdx; i++)
                        {
                            if (!(char.IsLetterOrDigit(textLocation[i]) || textLocation[i] == '#' || textLocation[i] == ' '))
                            {
                                dashIdx = -1;
                                break;
                            }
                        }
                        if (dashIdx >= 0 && char.IsDigit(textLocation.Substring(dashIdx + 1).Trim()[0]))
                        {
                            address1 = textLocation.Substring(0, dashIdx).Trim();
                            //textLocation = textLocation.Substring(dashIdx + 1);
                            if (address1.IndexOf('#') >= 0)
                            {
                                address1 = address1.Replace("#", "");
                                textLocation = textLocation.Replace("#", "");
                            }
                        }
                    }
                    if (address1 == null)
                    {
                        dashIdx = -1;
                        address1 = Foundation.Address1.Parse(ref textLocation);
                    }

                    var address = AddressFromGeocoder(Foundation.Google.Geocode(textLocation), populatePath);
                    if (!string.IsNullOrEmpty(address1) && address != null)
                    {
                        if (dashIdx > 0 && (address.Text.IndexOf(address1, StringComparison.OrdinalIgnoreCase) == -1 || address1.Length == 1))
                        {
                            if (address1.Count(c => char.IsLetter(c)) <= 2)
                                address.Address1 = "Unit " + address1.ToUpper();
                            else
                                address.Address1 = address1;
                        }
                        else if (dashIdx == -1)
                            address.Address1 = address1;
                    }
                    return address;
                }
                else if (geoLocation != null)
                {
                    var geocoded = Foundation.Google.Geocode(geoLocation);
                    if (geocoded.Address != null)
                    {
                        geocoded.Address.StreetName = null;
                        geocoded.Address.StreetNumber = null;
                        if (geocoded.Geolocation != null)
                        {
                            geocoded.Geolocation.Lat = geoLocation.Lat;
                            geocoded.Geolocation.Lng = geoLocation.Lng;
                        }
                    }
                    return AddressFromGeocoder(geocoded, populatePath);
                }
                else
                    return null;
            } */

            /* public static Address AddressFromGeocoder(Model.Geocoder.Location geocoded, bool populatePath = false)
            {
                //try
                //{
                if (geocoded != null && geocoded.Address != null && !string.IsNullOrEmpty(geocoded.Address.Country))
                {
                    var city = new Model.Geocoder.City { Country = geocoded.Address.Country };
                    //State and County may not get populated (London, UK)
                    if (!string.IsNullOrEmpty(geocoded.Address.State))
                    {
                        city.State = geocoded.Address.State;
                        //if (!String.IsNullOrEmpty(geocoded.Address.City))
                        //    city.Name = geocoded.Address.City;
                    }
                    if (!string.IsNullOrEmpty(geocoded.Address.County))
                        city.County = geocoded.Address.County;
                    if (!string.IsNullOrEmpty(geocoded.Address.City))
                        city.Name = geocoded.Address.City;
                    var location = resolveLocation(city, geocoded.Address.StreetName, true);
                    if (location != null && location.Id > 0)
                    {
                        var address = new Address();
                        if (populatePath) //Engine.Company.Metadata.Process needs the street Name
                            address.LocationPath = location.Street != null ? location.Parent : location; //location - WCF fault
                        if (location.Street != null)
                        {
                            address.Location = location.Parent.Id;
                            address.Street = location.Street.Id;
                            if (!string.IsNullOrEmpty(geocoded.Address.StreetNumber))
                                address.StreetNumber = geocoded.Address.StreetNumber;
                        }
                        else
                            address.Location = location.Id;
                        if (!string.IsNullOrEmpty(geocoded.Address.PostalCode))
                            address.PostalCode = geocoded.Address.PostalCode;
                        if (geocoded.Geolocation != null)
                        {
                            address.Lat = geocoded.Geolocation.Lat;
                            address.Lng = geocoded.Geolocation.Lng;
                        }
                        address.Text = geocoded.Text;
                        return address;
                    }
                }
                //} //Need to propagate ArgumentException(Invalid) when Country does not exist in the system
                //catch { }
                return null;
            } */

            public static Node<int> /*Interface.Master.ILocation.*/PopulateWithChildren(int parentLocation, BizSrt.Api.Model.Group.SubType type)
            {
                if (parentLocation >= 0)
                {
                    return CachedNode<int>.PopulateWithChildren(parentLocation, type, 0, LegacyCache.Locations);
                }
                else
                    throw new ArgumentException("Invalid input.");
            }

            private static Model.Location ToLocation(Model.LocationRef locRef)
            {
                if (locRef == null) return null;
                return new Model.Location { Id = locRef.Id, Name = locRef.Name, Type = locRef.Type, HasChildren = locRef.HasChildren, Parent = ToLocation((Model.LocationRef)locRef.Parent) };
            }

            public static Model.Location /*Interface.Master.ILocation.*/PopulateWithPath(int location)
            {
                if (location > 0)
                {
                    return ToLocation(LegacyCache.Locations[location].PopulateWithPath());
                }
                else
                    throw new ArgumentException("Invalid input.");
            }

            public static Model.Location PopulateWithPath(int city, int street)
            {
                var cachedCity = LegacyCache.Locations[city].PopulateWithPath();
                if (cachedCity.Type != LocationType.City)
                    throw new InvalidOperationException("Unexpected state");

                if (street > 0)
                {
                    var cachedStreet = LegacyCache.StreetNames[street];
                    if (cachedStreet.City != city)
                        throw new InvalidOperationException("Unexpected state");

                    return new Model.Location { Id = street, Name = cachedStreet.Name, Type = LocationType.Street, Parent = ToLocation(cachedCity) };
                }
                else
                    return ToLocation(cachedCity);
            }
        }

        LocationRef[] /*Interface.Master.ILocation.*/GetChildren(int parentLocation)
        {
            if (parentLocation >= 0)
            {
                //return LegacyCache.Locations[parentLocation, 0].Select(l => new LocationRef { Id = l.Id, Name = l.Name, Type = l.Type }).ToArray();
                var cachedLocations = LegacyCache.Locations[parentLocation, 0];
                if (cachedLocations != null && cachedLocations.Length > 0)
                {
                    var cachedParent = LegacyCache.Locations[parentLocation];
                    if (cachedParent.Type != LocationType.State || LegacyCache.LocationSettings[parentLocation].CountyRequired)
                        return cachedLocations.Select(l => l.EntityRef(DisplayType.Name)).ToArray();
                    else
                    {
                        var locations = new List<LocationRef>();
                        foreach (var location in cachedLocations)
                        {
                            if (location.Type == LocationType.City)
                                locations.Add(location.EntityRef(DisplayType.Name));
                            else
                                locations.AddRange(LegacyCache.Locations[location.Id, 0].Select(l => l.EntityRef(DisplayType.Name)));
                        }
                        return (from l in locations
                                orderby l.Name
                                select l).ToArray();
                    }
                }
                else
                    return null;
            }
            else
                throw new ArgumentException("Invalid input.");
        }

        LocationRef[] /*Interface.Master.ILocation.*/GetStreetNames(int city, char firstLetter)
        {
            if (city >= 0 && char.IsLetterOrDigit(firstLetter))
            {
                //return LegacyCache.StreetNames[new global::Entity.CachedStreetName.GroupKey { City = city, FirstLetter = firstLetter }, 0].Select(l => new LocationRef { Id = l.Id, Name = l.Name, Type = LocationType.Street }).ToArray();
                var streetNames = LegacyCache.StreetNames[new CachedStreetName.GroupKey { City = city, FirstLetter = firstLetter }, 0];
                if (streetNames != null && streetNames.Length > 0)
                    return streetNames.Select(l => new LocationRef { Id = l.Id, Name = l.Name, Type = LocationType.Street }).ToArray();
                else
                    return null;
            }
            else
                throw new ArgumentException("Invalid input.");
        }

        //Succeeded by Autocomplete
        Model.Group.IdName<int>[] /*Interface.Master.ILocation.*/Search(int parentLocation, string name, Model.IdName<int> scope)
        {
            if (parentLocation >= 0 && !string.IsNullOrWhiteSpace(name))
            {
                var locations = LegacyCache.LocationSearch[new GroupSearchCache<int> { Parent = parentLocation, Name = name }].Take(15);
                //return locations.Select(c => LegacyCache.Locations[c].GetPath(scope)).ToArray();
                return locations.Select(location => LegacyCache.Locations[location].EntityRef<Model.Group.IdName<int>>(DisplayType.Path, (l, m) =>
                {
                    m.NodeType = l.NodeType(0);
                    m.HasChildren = LegacyCache.Locations.HasChildren(l.Id, 0);
                })).ToArray();
            }
            else
                throw new ArgumentException("Invalid input.");
        }

        LocationSettings /*Interface.Master.ILocation.*/GetSettings(int location)
        {
            if (location >= 0)
            {
                return LegacyCache.LocationSettings[location];
            }
            else
                throw new ArgumentException("Invalid input.");
        }

        Model.Location /*Interface.Master.ILocation.*/PopulateWithPath(int city, int street)
        {
            if (city > 0 && street > 0)
            {
                return Location.PopulateWithPath(city, street);
            }
            else
                throw new ArgumentException("Invalid input.");
        }

        /*int Interface.Master.ILocation.Resolve(int city, string streetName, bool allowCreate)
        {
            if (city > 0 && !string.IsNullOrWhiteSpace(streetName))
            {
                return Location.Resolve(city, streetName, allowCreate);
            }
            else
                throw new ArgumentException("Invalid input.");
        }*/

        //Service.Geocoder.Geocode on the client
        /*Address Interface.Master.ILocation.Resolve(global::Model.Geocoder.Geolocation location)
        {
            if (location != null)
            {
                return Location.Resolve(null, location);
            }
            else
                throw new ArgumentException("Invalid input.");
        }*/
    }
}