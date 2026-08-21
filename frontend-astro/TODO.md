# Frontend Modernization TODO: Broken/Stubbed Helpers

This document tracks all the known frontend helper methods, getters, and viewmodel logic that were inadvertently broken, stubbed out, or stripped of functionality during the Astro modernization port (primarily due to the transition from `Node.deserialize` rich OOP classes to plain JSON interfaces).

## 1. Domain Model Getters & Methods
*Note: The Hybrid Proxy implemented in service/location.ts fixes these for direct location fetches, but they remain broken if the data is fetched natively elsewhere (e.g., nested inside a Company fetch).*

- [x] **LocationRef.get city() & get county()**: Restored traversal logic as static methods in model/foundation.ts with member wrappers for the Admin UI.
- [x] **Address.equalsTo()**: Restored as a static utility method in model/foundation.ts while preserving the member method.
- [x] **Semantic.facetFilter()**: Ported the QueryInput class to provide static and member acetFilter() logic to safely sort and split included/excluded facet lists.
- [x] **Company.headOffice**: Extracted into a centralized pure utility getHeadOffice(company) in components/types.ts.
- [x] **WebAppImage.getImageRef()**: Legacy getter preserved and relocated to model/admin.ts for future Admin UI use.

## 2. Stubbed ViewModels & Components
The following classes in the `viewmodel` layer have been heavily stubbed out during the port to WebAwesome/Lit:

- [ ] **`Input.resolve()` (Search Location)**: Located in `src/viewmodel/search/location/input.ts`. Currently stubbed with `// Ported stub: normally hits /api/location/resolve with Google Places ID`. This prevents new geographic locations (like custom street addresses) from being fully resolved and saved to the backend database.
- [ ] **`Input.initAutocomplete()` & `clearAutocomplete()` (Search Location)**: Stubbed out because Google Maps logic was delegated directly to the Lit view. Need to ensure the Lit view fully implements the legacy autocomplete teardown and error handling.

## 3. Potential C# Backend Mismatches
- [ ] **`ImageRef` Backend Construction**: The modern UI no longer uses the `ImageRef` string from the backend (as we moved it to the client via `getLogoUrl()`). Ensure the C# backend `CompanyProfile` viewmodel doesn't waste CPU cycles constructing these strings anymore.
- [ ] **Missing Caches**: The legacy `Community.cs` cache logic needs to be fully ported over to `LegacyCache.cs` (Jobs, CompanyProjects) to prevent `NullReferenceException` crashes when the modern UI requests search endpoints.

---

## 4. Critical Bugs (from 2026-08-21 Audit)

- [x] **`service/api.ts:3` — Wrong SSR protocol**: Stripped `API_BASE` completely; it now reads cleanly from `.env` via `ApiConfig`.
- [x] **All SSR pages — Remove `NODE_TLS_REJECT_UNAUTHORIZED`**: Removed the `process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'` hack from all 4 astro routing pages now that `api.ts` is configured for HTTP.
- [x] **`settings.ts:264` — Next.js env var in Astro**: Switched config mechanism to use `import.meta.env.PUBLIC_GOOGLE_MAPS_API_KEY`.
- [x] **`settings.ts:255` — `Service.origin` points to legacy port**: `origin` was renamed to `baseUrl` and hooked up to the new standard `ApiConfig`.
- [x] **`company/profile.ts:252-256` — State mutation inside `render()`**: Moved URL tab parsing to `willUpdate()`.
- [x] **`company/profile.ts:393` — Broken map overlay**: Fixed broken `this.shadowRoot?.getElementById` by replacing it with a robust `querySelector('#mapView')` so map pop-ups work again.
- [x] **`components/search/category/menu.ts` — Stub component**: Replaced the console log and stale Next.js comment with a standard `app-navigate` CustomEvent stub.
- [x] **`components/types.ts:Location.address` — Wrong type**: Typed as `string` but the backend returns a `Geocoder.Address` object. Breaks `stringify()` calls in `company/profile.ts:328`. Fix: `address: Geocoder.Address | string`.
- [x] **`exception.ts` — `ErrorMessageType` wrong numeric values**: Modern enum uses different integers from the legacy/backend enum (e.g. `Argument_Invalid=1` vs legacy `41`). All backend error type comparisons are silently wrong. Restore numeric values to match backend.

---

## 5. Service Layer Cleanup (from 2026-08-21 Audit)

- [x] **All service files — Redundant `!response.ok` checks**: Removed the redundant `if (!response.ok) throw...` blocks from `category.ts`, `company.ts`, `community.ts`, `location.ts`, and `offering.ts`. Kept the intentional 404 handler in `offering.view()`.
- [ ] **`service/company.ts` — Remove dead `CompanyProfileCache` class**: Ripped out the disabled cache class and refactored `view()` to use `apiFetch()` directly.
- [ ] **`service/community.ts` — Remove dead `CommunityProfileCache` class**: Ripped out the disabled cache class and refactored `view()` to use `apiFetch()` directly.
- [x] **`service/geocoder.ts` — Convert `geocode()` to async/await**: Refactored the legacy callback signature to return a modern Promise, wrapping `google.maps.Geocoder.geocode`.
- [x] **`service/geocoder.ts:11` — Hardcoded `LocationSettings`**: Stripped the hardcoded mock block and imported `LocationSettings` directly from `../../settings.js`.
- [ ] **`service/company.ts` — Missing methods (pending feature work)**: `newProfiles`, `getProjects`, `getNews`, `getArticles`, `getJobs`, `getPromotions`, `getInfo`, `Product.view`, `Project.view`, `Job.view`, `ArticleCategory.get`, `Department.get` — add stubs or track in LEGACY_FRONTEND_TRACKER when porting the respective tabs.
- [ ] **`service/community.ts` — Severely incomplete**: Only 2 of 16 legacy methods are ported. Missing: `Category.get`, `News.*`, `Article.*`, `ArticleMessage.*`, `Company.*`, `Product.*`, `Project.*`. Track in LEGACY_FRONTEND_TRACKER.

---

## 6. Model / Type Inconsistencies (from 2026-08-21 Audit)

- [x] **`model/foundation.ts:List.SliceOutput` — Duplicate/conflicting type**: PascalCase `{ Series, Index }` conflicts with the camelCase `{ series, index }` version in `components/types.ts`. The `foundation.ts` version is never used. Remove it to avoid confusion.
- [x] **`model/foundation.ts:DirectorySliceInput` — PascalCase vs camelCase mismatch**: Declared with PascalCase props (`Index`, `Length`, `Category`, `Location`) but `service/company.ts` constructs camelCase literals. Type safety hole.
- [x] **`model.ts:DictionaryType` — Rename parity risk**: `OfferingType=5`, `OfferingPriceType=6`, `OfferingAttributeType=7` were renamed from `ProductType/ProductPriceType/ProductAttributeType`. Integer values appear correct but must be verified against the backend C# `DictionaryType` enum to confirm parity.

---

## 7. Navigation Gaps (from 2026-08-21 Audit)

- [ ] **`navigation.ts` — Only ~10% ported**: 103 lines vs 1,312 in legacy. Missing `Community`, `Project`, `Job` namespaces. Missing `productsView()`, `projectView()` methods used in `toPreview` nav token generation. Port as new features are added.
- [x] **Stale Next.js comments in 3 files**: Removed all obsolete Next.js routing references and modernized comments to reference Astro/CustomEvent routing.

---

## 8. Dead Code Cleanup (from 2026-08-21 Audit)

- [ ] **`model.ts` — 4 unused classes**: `OfferingStats`, `WebAppImage`, `Image`, `SecurityProfile` have no consumers in current Astro src. Review before removal — some may be needed when admin/edit UIs are added.
- [ ] **`model.ts:Image.getImageRef()`**: Routes through `Service.origin` (port 8000 — wrong). All image construction now done by `service/image.ts:getLogoUrl()`. The `Image` class in model.ts is dead for display purposes.
- [ ] **`viewmodel.ts:ElementType` enum**: No consumers anywhere in Astro src.

---

## 9. Debug Code to Remove Before Shipping (from 2026-08-21 Audit)

- [x] **`company/search.ts`** — Removed all 11 spammy `console.log` statements.
- [x] **`company/offering.ts:158`** — Removed the `console.log` inside `render()` that fired on every frame.
- [x] **`viewmodel/list/view.ts`** — Scrubbed the pagination `console.log` traces.
# Frontend Modernization TODO: Broken/Stubbed Helpers

This document tracks all the known frontend helper methods, getters, and viewmodel logic that were inadvertently broken, stubbed out, or stripped of functionality during the Astro modernization port (primarily due to the transition from `Node.deserialize` rich OOP classes to plain JSON interfaces).

## 1. Domain Model Getters & Methods
*Note: The Hybrid Proxy implemented in service/location.ts fixes these for direct location fetches, but they remain broken if the data is fetched natively elsewhere (e.g., nested inside a Company fetch).*

- [x] **LocationRef.get city() & get county()**: Restored traversal logic as static methods in model/foundation.ts with member wrappers for the Admin UI.
- [x] **Address.equalsTo()**: Restored as a static utility method in model/foundation.ts while preserving the member method.
- [x] **Semantic.facetFilter()**: Ported the QueryInput class to provide static and member acetFilter() logic to safely sort and split included/excluded facet lists.
- [x] **Company.headOffice**: Extracted into a centralized pure utility getHeadOffice(company) in components/types.ts.
- [x] **WebAppImage.getImageRef()**: Legacy getter preserved and relocated to model/admin.ts for future Admin UI use.

## 2. Stubbed ViewModels & Components
The following classes in the `viewmodel` layer have been heavily stubbed out during the port to WebAwesome/Lit:

- [ ] **`Input.resolve()` (Search Location)**: Located in `src/viewmodel/search/location/input.ts`. Currently stubbed with `// Ported stub: normally hits /api/location/resolve with Google Places ID`. This prevents new geographic locations (like custom street addresses) from being fully resolved and saved to the backend database.
- [ ] **`Input.initAutocomplete()` & `clearAutocomplete()` (Search Location)**: Stubbed out because Google Maps logic was delegated directly to the Lit view. Need to ensure the Lit view fully implements the legacy autocomplete teardown and error handling.

## 3. Potential C# Backend Mismatches
- [ ] **`ImageRef` Backend Construction**: The modern UI no longer uses the `ImageRef` string from the backend (as we moved it to the client via `getLogoUrl()`). Ensure the C# backend `CompanyProfile` viewmodel doesn't waste CPU cycles constructing these strings anymore.
- [ ] **Missing Caches**: The legacy `Community.cs` cache logic needs to be fully ported over to `LegacyCache.cs` (Jobs, CompanyProjects) to prevent `NullReferenceException` crashes when the modern UI requests search endpoints.

---

## 4. Critical Bugs (from 2026-08-21 Audit)

- [x] **`service/api.ts:3` — Wrong SSR protocol**: Stripped `API_BASE` completely; it now reads cleanly from `.env` via `ApiConfig`.
- [x] **All SSR pages — Remove `NODE_TLS_REJECT_UNAUTHORIZED`**: Removed the `process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'` hack from all 4 astro routing pages now that `api.ts` is configured for HTTP.
- [x] **`settings.ts:264` — Next.js env var in Astro**: Switched config mechanism to use `import.meta.env.PUBLIC_GOOGLE_MAPS_API_KEY`.
- [x] **`settings.ts:255` — `Service.origin` points to legacy port**: `origin` was renamed to `baseUrl` and hooked up to the new standard `ApiConfig`.
- [x] **`company/profile.ts:252-256` — State mutation inside `render()`**: Moved URL tab parsing to `willUpdate()`.
- [x] **`company/profile.ts:393` — Broken map overlay**: Fixed broken `this.shadowRoot?.getElementById` by replacing it with a robust `querySelector('#mapView')` so map pop-ups work again.
- [x] **`components/search/category/menu.ts` — Stub component**: Replaced the console log and stale Next.js comment with a standard `app-navigate` CustomEvent stub.
- [x] **`components/types.ts:Location.address` — Wrong type**: Typed as `string` but the backend returns a `Geocoder.Address` object. Breaks `stringify()` calls in `company/profile.ts:328`. Fix: `address: Geocoder.Address | string`.
- [x] **`exception.ts` — `ErrorMessageType` wrong numeric values**: Modern enum uses different integers from the legacy/backend enum (e.g. `Argument_Invalid=1` vs legacy `41`). All backend error type comparisons are silently wrong. Restore numeric values to match backend.

---

## 5. Service Layer Cleanup (from 2026-08-21 Audit)

- [x] **All service files — Redundant `!response.ok` checks**: Removed the redundant `if (!response.ok) throw...` blocks from `category.ts`, `company.ts`, `community.ts`, `location.ts`, and `offering.ts`. Kept the intentional 404 handler in `offering.view()`.
- [ ] **`service/company.ts` — Remove dead `CompanyProfileCache` class**: Ripped out the disabled cache class and refactored `view()` to use `apiFetch()` directly.
- [ ] **`service/community.ts` — Remove dead `CommunityProfileCache` class**: Ripped out the disabled cache class and refactored `view()` to use `apiFetch()` directly.
- [x] **`service/geocoder.ts` — Convert `geocode()` to async/await**: Refactored the legacy callback signature to return a modern Promise, wrapping `google.maps.Geocoder.geocode`.
- [x] **`service/geocoder.ts:11` — Hardcoded `LocationSettings`**: Stripped the hardcoded mock block and imported `LocationSettings` directly from `../../settings.js`.
- [ ] **`service/company.ts` — Missing methods (pending feature work)**: `newProfiles`, `getProjects`, `getNews`, `getArticles`, `getJobs`, `getPromotions`, `getInfo`, `Product.view`, `Project.view`, `Job.view`, `ArticleCategory.get`, `Department.get` — add stubs or track in LEGACY_FRONTEND_TRACKER when porting the respective tabs.
- [ ] **`service/community.ts` — Severely incomplete**: Only 2 of 16 legacy methods are ported. Missing: `Category.get`, `News.*`, `Article.*`, `ArticleMessage.*`, `Company.*`, `Product.*`, `Project.*`. Track in LEGACY_FRONTEND_TRACKER.

---

## 6. Model / Type Inconsistencies (from 2026-08-21 Audit)

- [x] **`model/foundation.ts:List.SliceOutput` — Duplicate/conflicting type**: PascalCase `{ Series, Index }` conflicts with the camelCase `{ series, index }` version in `components/types.ts`. The `foundation.ts` version is never used. Remove it to avoid confusion.
- [x] **`model/foundation.ts:DirectorySliceInput` — PascalCase vs camelCase mismatch**: Declared with PascalCase props (`Index`, `Length`, `Category`, `Location`) but `service/company.ts` constructs camelCase literals. Type safety hole.
- [x] **`model.ts:DictionaryType` — Rename parity risk**: `OfferingType=5`, `OfferingPriceType=6`, `OfferingAttributeType=7` were renamed from `ProductType/ProductPriceType/ProductAttributeType`. Integer values appear correct but must be verified against the backend C# `DictionaryType` enum to confirm parity.

---

## 7. Navigation Gaps (from 2026-08-21 Audit)

- [ ] **`navigation.ts` — Only ~10% ported**: 103 lines vs 1,312 in legacy. Missing `Community`, `Project`, `Job` namespaces. Missing `productsView()`, `projectView()` methods used in `toPreview` nav token generation. Port as new features are added.
- [x] **Stale Next.js comments in 3 files**: Removed all obsolete Next.js routing references and modernized comments to reference Astro/CustomEvent routing.

---

## 8. Dead Code Cleanup (from 2026-08-21 Audit)

- [ ] **`model.ts` — 4 unused classes**: `OfferingStats`, `WebAppImage`, `Image`, `SecurityProfile` have no consumers in current Astro src. Review before removal — some may be needed when admin/edit UIs are added.
- [ ] **`model.ts:Image.getImageRef()`**: Routes through `Service.origin` (port 8000 — wrong). All image construction now done by `service/image.ts:getLogoUrl()`. The `Image` class in model.ts is dead for display purposes.
- [ ] **`viewmodel.ts:ElementType` enum**: No consumers anywhere in Astro src.

---

## 9. Debug Code to Remove Before Shipping (from 2026-08-21 Audit)

- [x] **`company/search.ts`** — Removed all 11 spammy `console.log` statements.
- [x] **`company/offering.ts:158`** — Removed the `console.log` inside `render()` that fired on every frame.
- [x] **`viewmodel/list/view.ts`** — Scrubbed the pagination `console.log` traces.
- [x] **`components/search/category/menu.ts`** — Replaced `console.log` with a standard event stub.

---

## 10. UX / Page Issues (from 2026-08-21 Audit)

- [x] **`pages/index.astro` — Client-side redirect**: Uses `navigate('/companies', { history: 'replace' })` in an inline `<script>`. Replace with `return Astro.redirect('/companies', 301)` in frontmatter for a proper SSR redirect that is SEO-friendly.
- [x] **`company/profile.ts` — Stub tabs visible in tab bar**: "projects", "jobs", "marketplace", "promotions", "news" tabs are rendered and visible but show `_renderStubTab()` ("section not yet available"). Hide these tabs from the `<wa-tab-group>` until the respective pages are ported.
- [x] **`components/company/listview.ts` — List mode broken**: Removed the `layout="horizontal"` card hack and properly ported the dedicated `company-listitem.ts` component from the legacy codebase, completely preserving the rich dashboard data rows (website links, products quick-links, and selectable checkboxes).
- [x] **`components/company/card.ts` — Inconsistent property API**: Uses old `static get properties()` pattern while all other components use `@property()` decorators. Migrate for consistency.
- [ ] **`viewmodel/search/home.ts` — MVVM violation**: `firstUpdated()` uses `shadowRoot.querySelector('search-category-input')` to reach a child viewmodel via DOM. Child should expose model as a property or fire a `model-ready` event.
- [x] **`company/profile.ts:20,26` — Duplicate import**: Removed the duplicate import on line 26.
