    private static string FormatAddress(string streetNumber, int? streetNameId, string address1, int? locationId, string postalCode)
    {
        var parts = new System.Collections.Generic.List<string>();
        string streetName = null;
        if (streetNameId.HasValue && streetNameId.Value > 0)
        {
            var cachedStreet = BizSrt.Api.Data.Cache.LegacyCache.StreetNames[streetNameId.Value];
            if (cachedStreet != null) streetName = cachedStreet.Name;
        }

        string city = null;
        string state = null;
        string county = null;

        if (locationId.HasValue && locationId.Value > 0)
        {
            var loc = BizSrt.Api.Data.Cache.LegacyCache.Locations[locationId.Value];
            while (loc != null && loc.Id > 1)
            {
                if (loc.Type == BizSrt.Model.LocationType.City) city = loc.Name;
                else if (loc.Type == BizSrt.Model.LocationType.State) state = loc.Name;
                else if (loc.Type == BizSrt.Model.LocationType.County) county = loc.Name;
                
                if (loc.ParentKey > 0)
                    loc = BizSrt.Api.Data.Cache.LegacyCache.Locations[loc.ParentKey] as BizSrt.Api.Data.Cache.Location.CachedLocation;
                else
                    loc = null;
            }
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            if (!string.IsNullOrWhiteSpace(streetName))
            {
                if (!string.IsNullOrWhiteSpace(streetNumber))
                {
                    if (!string.IsNullOrWhiteSpace(address1))
                    {
                        if (address1.Length <= 10) parts.Add($"{streetNumber} {streetName} {address1}");
                        else parts.Add($"{address1} {streetNumber} {streetName}");
                    }
                    else parts.Add($"{streetNumber} {streetName}");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(address1)) parts.Add($"{address1} {streetName}");
                    else parts.Add(streetName);
                }
                parts.Add(city);
            }
            else if (!string.IsNullOrWhiteSpace(address1))
            {
                parts.Add($"{address1} {city}");
            }
            else
            {
                parts.Add(city);
            }
        }
        else if (!string.IsNullOrWhiteSpace(streetName))
        {
            if (!string.IsNullOrWhiteSpace(streetNumber)) parts.Add($"{streetNumber} {streetName}");
            else parts.Add(streetName);
        }
        
        if (!string.IsNullOrWhiteSpace(state))
        {
            if (!string.IsNullOrWhiteSpace(county)) parts.Add(county);
            if (!string.IsNullOrWhiteSpace(postalCode)) parts.Add($"{state} {postalCode}");
            else parts.Add(state);
        }
        else if (!string.IsNullOrWhiteSpace(county))
        {
            if (!string.IsNullOrWhiteSpace(postalCode)) parts.Add($"{county} {postalCode}");
            else parts.Add(county);
        }
        else if (!string.IsNullOrWhiteSpace(postalCode))
        {
            parts.Add(postalCode);
        }
        
        return string.Join(", ", parts).Trim();
    }
