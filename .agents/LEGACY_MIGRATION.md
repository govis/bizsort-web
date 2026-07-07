# BizSort Legacy Codebase & Migration Tracking

This document provides a comprehensive overview of the legacy BizSort architecture and tracks the modernization progress. **Please also refer to [LEGACY_BACKEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_BACKEND_TRACKER.md) for a line-by-line backend file tracking matrix and [LEGACY_FRONTEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_FRONTEND_TRACKER.md) for the frontend tracking matrix.**for the new Next.js / .NET 8 codebase. **All agents must review this file when deciding how to port or where to place code.**

## Legacy Architecture Overview

The legacy codebase is split into two primary areas:

1. **`legacy/server/` (Backend - C#):** 
   - A highly cached, layered monolith.
   - **Data Layer (`Data/`):** Directly queries EF Core `Entities` and uses heavily cached read-through proxies (e.g., `Cache.cs` and `ReadManyExpirationCache`).
   - **Service Layer (`Service/`):** Contains business logic that invokes the Data layer caches and helpers.
   - **Model Layer (`Model/`):** Contains DTOs and view models mapped from the Data layer or returned to the UI.
   - **API/Endpoints:** Historically used Web API/MVC controllers.

2. **`legacy/website/` (Frontend - TypeScript/Lit):**
   - **`wwwroot/src/model/`**: Foundational Typescript types, enums, DTOs (e.g. `IdName`, `LocationRef`).
   - **`wwwroot/src/viewmodel/`**: Centralized logic for input data binding, error handling (`Validateable`, `ErrorInfo`), and state tracking.
   - **`wwwroot/src/service/`**: Frontend API wrappers (using `fetch`/AJAX) to communicate with the backend.
   - **`wwwroot/component/`**: Pre-compiled WebComponents and LitElements orchestrating ViewModels and UI.

## Modernization Principles

1. **Strict API Parity:** You must not invent new REST schemas. Your ported .NET Endpoints must exactly match what the legacy TypeScript `src/service/` layers expect.
2. **Schema Name Remapping (Business -> Company):** During modernization, all instances of `Business` in the database schema and object models have been renamed to `Company` (e.g., `Businesses` table became `CompanyProfiles`, `BusinessOffices` became `CompanyOffices`, and LINQ variables `bi` -> `cm`, `bo` -> `co`). All database indexes have also been renamed to use the `Company*` prefix.
3. **ViewModel Preservation:** Do not rip out the legacy `ViewModel` pattern for inputs. Extract the logic from Lit components into modern `frontend/src/viewmodel/` classes to maintain data-flow consistency.
4. **No Novel DB Queries:** All complex EF queries already exist in `legacy/server/Data/`. Port them exactly as they are.
5. **LINQ Building Blocks:** Legacy code constructed complex LINQ queries using reusable building blocks (e.g. `Company.GetActive()`, `Company.GetFiltered()`, `CompanyProduct.GetActive()`). When porting search methods for Company and Company Product profiles, implement these building blocks as reusable extension methods to maintain the legacy architecture.
6. **EF Core 8 Query Translation (APPLY vs JOIN):** Legacy EF6 queries that heavily utilized the `let` keyword with multiple conditions (e.g., `let x = dbContext...FirstOrDefault() where x != null && x.Prop != null`) were natively translated into a single optimized `OUTER APPLY`. EF Core 8 parsing can sometimes regress these patterns into multiple redundant scalar subqueries. When porting these explicit "APPLY" patterns, either use standard `join` statements (with `.Distinct()` if necessary) OR explicitly structure `from...Take(1)` subqueries to guarantee performant SQL generation.

   **How to Hint EF Core 8 to generate `CROSS APPLY` / `OUTER APPLY`:**
   In SQL Server, `APPLY` operates like a `foreach` loop: it evaluates the right-hand table expression once for every single row returned by the left-hand table. EF Core will naturally generate a `CROSS APPLY` (like an INNER JOIN) or `OUTER APPLY` (like a LEFT JOIN) when you use correlated subqueries in your LINQ—specifically using a second `from` clause.

   **To generate an `OUTER APPLY` (returns the row even if the subquery is empty):**
   ```csharp
   var query =
       from c in dbContext.CompanyProfiles
       // Notice how the right side depends on 'c.Id' and uses .Take(1)
       from media in dbContext.CompanyMedia
           .Where(m => m.Company == c.Id && m.Type == 1)
           .Take(1)
           .DefaultIfEmpty()
       where media != null && media.Metadata.Length > 0
       select c;
   ```

   **To generate a `CROSS APPLY` (drops the row if the subquery is empty):**
   ```csharp
   var query =
       from c in dbContext.CompanyProfiles
       from media in dbContext.CompanyMedia
           .Where(m => m.Company == c.Id && m.Type == 1)
           .Take(1)
       // Omitting DefaultIfEmpty() makes it a CROSS APPLY
       where media.Metadata.Length > 0
       select c;
   ```

   *Rule of thumb:* If you encounter legacy code using `let x = dbContext.Table.FirstOrDefault(...)`, rewriting it to the second `from` clause with `.Take(1)` is the most reliable way to hint EF Core 8 to generate a clean, single `APPLY` without the "triple-subquery" bug.

5. **LOB (Large Object) Columns over Network:** When querying entities that have `varbinary(max)` or `nvarchar(max)` columns (like `CompanyMedia.Content`), NEVER query the full entity if you only need the ID. EF Core 8 will attempt to download the multi-megabyte payloads for every record over the network just to populate the unused property. Always use `.Select(m => m.Id).FirstOrDefault()`.
6. **Distinct() vs Any():** Avoid using `.Distinct()` on full entities (e.g., `select c).Distinct()`) when joining tables. EF Core 8 often translates this by fetching *all* scalar columns of the entity into a subquery to compute uniqueness before applying ordering. Instead, use an `.Any()` inside a `where` clause (e.g., `where otherTable.Any(o => o.Id == c.Id)`) which translates cleanly to an `EXISTS` statement.

## Migration progress. **Please also refer to [LEGACY_BACKEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_BACKEND_TRACKER.md) for a line-by-line backend tracking matrix and [LEGACY_FRONTEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_FRONTEND_TRACKER.md) for the frontend tracking matrix.**

### 1. Backend Services & Data

- [x] **Location Infrastructure:** Ported `LocationRef`, `IdName`, `LocationSettings`, `LocationType` enum, and the static `BizSrt.Api.Data.Master.Location` facade.
- [x] **Caching Scaffolding:** Ported base caching logic (`ReadManyExpirationCache`) and instantiated `LegacyCache` singletons.
- [x] **Location Service & Endpoints:** Ported `LocationService` (Resolve, PopulateWithPath) and mapped legacy `OperationExceptionType` / Geocoding appropriately.
- [x] **Category Service:** Ported `CategoryService` resolving namespace collisions for `SubType`.

### 2. Frontend Infrastructure

- [x] **Core Models:** Ported `frontend/src/model.ts`, `frontend/src/model/foundation.ts`, `exception.ts`, and `settings.ts`. Disabled strict mode on these legacy files to ease migration.
- [x] **Base ViewModels:** Rewrote `ViewModel`, `IViewAdapter`, `Validateable`, and `ErrorInfo` without jQuery dependencies in `frontend/src/viewmodel.ts`.
- [x] **Component ViewModels:** Extracted `<search-category-input>` and `<search-location-input>` logic into `frontend/src/viewmodel/search/category/input.ts` and `frontend/src/viewmodel/location/input.ts`.
- [x] **Lit Components:** Upgraded `SearchCategoryInput` and `SearchLocationInput` to use WebAwesome UI and wire into their modern ViewModels via `IViewAdapter`.

### 3. Company Profile Infrastructure

- [x] **`CompanyProfilesCache`:** Ported to `backend/Data/Cache/Company/Profile.cs`. **Performance Note:** Fixed a massive memory/network leak. When querying `CompanyMedia` to check if a default image exists, we must project only the `Id` (`.Select(m => m.Id)`). Fetching the entire `CompanyMedia` entity downloads the `varbinary(max)` blob payload over the network for every company, taking `toPreview` from ~300ms to over 3 seconds. 
- [x] **`FeaturedCompaniesCache`:** Ported to `backend/Data/Cache/Company/FeaturedCompaniesCache.cs`. Keyed by `(Category, Location)`. **Performance Note:** Fixed two massive EF Core 8 query generation issues. 
   1. Replaced `.Distinct()` and `join` for `CompanyOffices` with a native `coq.Any(co => co.Company == c.Id)`. EF Core 8 translates `.Distinct()` on entities by fetching *all* columns just to check uniqueness.
   2. Added a SQL `IX_CompanyProfiles_Created` index and appended `.Take(500)` to the LINQ query. The complex `EXISTS` conditions in `getFeatured` forced SQL Server to bypass the index and scan/sort the entire 30,000+ row table on cold cache (7 seconds). Enforcing `.Take(500)` in LINQ biases the query optimizer to scan the index instead.
- [x] **`CachedCompanyProfile` model:** Ported to `backend/Data/Cache/Company/Profile.cs` (inner class). Properties match legacy: `Id`, `Name`, `Email`, `WebSite`, `Text`, `Category`, `ServiceType`, `TransactionType`, `Options`, `ImageId`, `ImageSize`, `Offices`, `Products`, `MultiProduct`.
- [x] **`CompanyProfileService`:** Ported to `backend/Service/Company/Profile.cs`. Implements `GetFeaturedAsync` (with default `Location=1` / Canada fix) and `ToPreviewAsync`. Uses `LegacyCache.FeaturedCompanies` and `LegacyCache.CompanyProfiles` — does NOT hit DB directly.
- [x] **`Company.Profile` Data Layer:** Ported to `backend/Data/Company/Profile.cs`. Contains `GetFeaturedAsync` and `ToPreviewAsync` logic matching legacy `Data.Company.Profile`.
- [x] **`CompanyProfileEndpoints`:** Mapped in `backend/Endpoint/` to `/api/company/profile/getFeatured` and `/api/company/profile/toPreview`.
- [x] **EF Schema fix:** `Category_Unwound` and `Location_Unwound` entities have no `Id` column — composite keys configured in `AppDbContext.cs` via `HasKey(e => new { e.Parent, e.Child })`.

### 4. Frontend — Company Pages & Components

- [x] **`company/home.ts`:** Page-level Lit element (`<company-home>`). Orchestrates `<search-home>` and `<company-featured>`, handles `search-submit` events, passes selection to featured component.
- [x] **`components/company/featured.ts`:** New `<company-featured>` Lit element. Accepts `selection: { category, location }` property (defaults `category=0, location=1`). Re-fetches on property change via `updated()` lifecycle. Calls `getFeatured()` service helper.
- [x] **`components/company/card.ts`:** New `<company-card>` Lit element rendering a single company preview card.
- [x] **`components/search/home.ts`:** Updated to track numeric `_categoryId` / `_locationId` instead of strings; dispatches numeric IDs on `search-submit`.
- [x] **`src/service/company.ts`:** `getFeatured(index, length, category=0, location=1)` sends category+location in JSON payload. `toPreview(ids)` sends array of IDs. Matches legacy `src/service/company.ts` method signatures.

### Pending Tasks

- [x] Port remaining company pages: `company/search.ts`, `company/profile.ts`, `company/header-layout.ts`.
- [ ] Fix Google Maps API Loader version incompatibility in `components/search/location/input.ts` (uses deprecated `Loader` class — must switch to functional `setOptions()`/`importLibrary()` API).
- [ ] Further migration of remaining legacy frontend modules (see `LEGACY_FRONTEND_TRACKER.md`).
