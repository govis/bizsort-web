# Frontend Modernization TODO: Broken/Stubbed Helpers

This document tracks all the known frontend helper methods, getters, and viewmodel logic that were inadvertently broken, stubbed out, or stripped of functionality during the Astro modernization port (primarily due to the transition from `Node.deserialize` rich OOP classes to plain JSON interfaces).

## 1. Domain Model Getters & Methods
*Note: The Hybrid Proxy implemented in `service/location.ts` fixes these for direct location fetches, but they remain broken if the data is fetched natively elsewhere (e.g., nested inside a `Company` fetch).*

- [ ] **`LocationRef.get city()` & `get county()`**: Traverses the parent hierarchy to find a specific location type.
- [ ] **`Address.equalsTo()`**: Compares two addresses for equality. Used heavily in location search viewmodels to cache geocoded results.
- [ ] **`Semantic.facetFilter()`**: Complex filtering logic embedded in the `Semantic` class in `model/foundation.ts`. Critical for search filtering if the modern UI needs to perform client-side facet manipulation.
- [ ] **`Company.headOffice`**: Legacy `get headOffice()` returned `this.offices[0]`. This is currently stripped from the `Company` model and manually mitigated inline in `company/profile.ts` using `this.company.headOffice || this.company.offices[0]`. It should be centralized as a utility function `getHeadOffice(company)` or proxied.
- [ ] **`WebAppImage.getImageRef()`**: Legacy getter in `model.ts`. Needs to be ported to a pure utility function (similar to what was done for `getLogoUrl` in `service/image.ts`) if WebApp icons are rendered anywhere in the modern UI.

## 2. Stubbed ViewModels & Components
The following classes in the `viewmodel` layer have been heavily stubbed out during the port to WebAwesome/Lit:

- [ ] **`Input.resolve()` (Search Location)**: Located in `src/viewmodel/search/location/input.ts`. Currently stubbed with `// Ported stub: normally hits /api/location/resolve with Google Places ID`. This prevents new geographic locations (like custom street addresses) from being fully resolved and saved to the backend database.
- [ ] **`Input.initAutocomplete()` & `clearAutocomplete()` (Search Location)**: Stubbed out because Google Maps logic was delegated directly to the Lit view. Need to ensure the Lit view fully implements the legacy autocomplete teardown and error handling.

## 3. Potential C# Backend Mismatches
- [ ] **`ImageRef` Backend Construction**: The modern UI no longer uses the `ImageRef` string from the backend (as we moved it to the client via `getLogoUrl()`). Ensure the C# backend `CompanyProfile` viewmodel doesn't waste CPU cycles constructing these strings anymore.
- [ ] **Missing Caches**: The legacy `Community.cs` cache logic needs to be fully ported over to `LegacyCache.cs` (Jobs, CompanyProjects) to prevent `NullReferenceException` crashes when the modern UI requests search endpoints.
