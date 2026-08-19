# Location & Address Architecture

This document tracks how geographic location and address structures are managed across the backend cache, data models, and frontend rendering, specifically detailing the architectural shift between the legacy Polymer/WCF era and the modern Astro/.NET pipeline.

## 1. The Core Entities and Caches

Geographic locations in BizSort are highly normalized. Instead of storing redundant strings in the `CompanyOffice` table, the schema relies on integer references to cached master tables.

### `LegacyCache.Locations` (The Geographic Hierarchy)
- Locations like Cities, States, Counties, and Countries are stored in a hierarchical tree.
- A single integer ID points to a `Location` record (e.g., `894` -> "Courtenay" (City)).
- Resolving the full hierarchy requires walking up the tree using `loc.ParentKey` until the top-level Country is reached.

### `LegacyCache.StreetNames`
- To prevent millions of redundant street strings, street names are stored in a dedicated `StreetNames` dictionary.
- A `CompanyOffice` stores an `int? StreetName` (e.g., `14196`).
- Resolving the street requires looking up `LegacyCache.StreetNames[14196]?.Name` (e.g., "Braidwood Rd").

---

## 2. The Architectural Shift (Legacy vs. Modern... and back)

### Initial Flawed Modernization (Flattened Strings)
1. **Simplified Model**: The modern `.NET` backend initially attempted to simplify the `BizSrt.Model.Location` contract. Instead of sending the full dictionary, `Address` was converted into a simple `string`.
2. **Backend Formatting**: To fulfill this string contract, the C# projection constructed the string directly on the server before serialization via a `FormatAddress` helper.
3. **The Bugs**: This approach broke down quickly. Not only did it sever the frontend from access to raw geographic segments (breaking map routing), but subtle bugs emerged. Most notably, the `BizSrt.Model.LocationType` enum in C# was mistakenly ported as sequential integers (`City = 5`), whereas the SQL `Locations` table stores them as Bitwise `[Flags]` (`City = 8`). This caused the backend to silently fail to resolve the City and State from the cache tree.

### The Restored Legacy Architecture (Current Modern State)
We completely abandoned the flattened server-side string approach and restored the original legacy data flow:
1. **Rich JSON Payload**: `BizSrt.Model.Location.Address` was restored from `string` to `BizSrt.Model.Geocoder.Address`.
2. **Backend Cache Hydration (`GetAddressModel`)**: The backend C# layer (`CachedCompanyProfile`, `Community`, etc.) now instantiates a full `Geocoder.Address` object. It resolves `LegacyCache.StreetNames` and walks up the `LegacyCache.Locations` parent tree to populate `City`, `State`, `County`, and `Country` using the corrected Bitwise `LocationType` enum.
3. **Frontend Hydration (`hydrateProxy`)**: The Astro frontend receives the raw JSON dictionary. When API calls are made (e.g., in `service/location.ts`), the raw JSON is passed through `hydrateProxy(json, ResolvedLocation, { address: Geocoder.Address })` to dynamically bind prototype methods (like `.equalsTo()`) to the POJO.
4. **Client-Side Formatting (`stringify`)**: We ported the legacy `stringify()` Javascript function into the Astro codebase (`src/service/geocoder.ts`). UI components (like `company-profile`) pass the rich JSON object into `stringify(this._office.location.address)` to render the human-readable string exactly as the legacy application did.

---

## 3. The `GetAddressModel` C# Implementation and Enum Gotchas

The `GetAddressModel` helper in the backend caching layer is responsible for converting raw `CompanyOffice` database fields into the rich `Geocoder.Address` object.

**Its exact responsibilities:**
1. Intercepts the `int? streetNameId` and queries `LegacyCache.StreetNames` to resolve the actual street string.
2. Intercepts the `int? locationId` and walks up the `LegacyCache.Locations` parent tree to extract the `City`, `State`, and `County`.

**CRITICAL GOTCHA - LocationType Bitwise Flags:**
The `Locations` table `Type` column strictly maps to power-of-two bitwise values. You must never port or define `LocationType` as sequential integers.
```csharp
[Flags]
public enum LocationType : byte
{
    Unknown = 0,
    World = 0,
    Country = 1,
    State = 2,
    County = 4,
    City = 8,
    Street = 16,
    Neighborhood = 32
}
```

---

## 4. Retaining Routing IDs and Dynamic Office Naming (`GetOfficeName`)

When `CachedCompanyOffice` was initially modernized to output a pre-computed `Address` string, the raw routing IDs (`LocationId`, `StreetNameId`, `StreetNumber`) were stripped from the cached object. However, this broke a critical piece of legacy server-side logic: **Dynamic Office Naming**.

### The Legacy `CompanyOffice.GetText` Algorithm
In the legacy system, if a company office lacked an explicit `Name` in the database, the backend dynamically generated one during the endpoint's `View` projection. 
- It fell back to the City Name (e.g., "Calgary Office").
- If the company had **multiple offices in the exact same city**, it appended the Street Name to disambiguate them (e.g., "Calgary - 919 11 Ave SW").

### The Modern Fix (`GetOfficeName`)
Because this dynamic naming algorithm runs dynamically *per company* at request time (it must check for duplicate cities across the specific company's array of offices), the raw routing IDs must be preserved in the cache. 
- `CachedCompanyOffice` now retains `LocationId`, `StreetNameId`, and `StreetNumber`.
- `CompanyService.ViewAsync` groups the offices by `LocationId` to detect duplicates.
- The `GetOfficeName()` helper then queries `LegacyCache.Locations` and `LegacyCache.StreetNames` using the preserved routing IDs to seamlessly recreate the legacy fallback algorithm.
