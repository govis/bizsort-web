# BizSort Legacy Codebase & Migration Tracking

This document provides a comprehensive overview of the legacy BizSort architecture and tracks the modernization progress. **Please also refer to [LEGACY_BACKEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_BACKEND_TRACKER.md) for a line-by-line backend file tracking matrix and [LEGACY_FRONTEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_FRONTEND_TRACKER.md) for the frontend tracking matrix.**for the new Next.js / .NET 10 codebase. **All agents must review this file when deciding how to port or where to place code.**

## Modern Architecture Overview

The new modern architecture breaks the monolith into standard .NET 10 libraries:
- **BizSrt.Model**: POCO DTOs, Enums, and ViewModels (Zero dependencies).
- **BizSrt.Foundation**: Abstract caching layers, utilities, text conversion (Depends on Model).
- **BizSrt.Data**: A pure Data Access Layer strictly containing ONLY the EF Core 10 `AppDbContext`, Entities, and `IQueryable` extensions. **Do not place Caches or business logic here.** (Depends on Foundation).
- **BizSrt.Api**: The frontend-facing REST API and orchestrating services. **Crucially, this layer acts as the authoritative source and sole host for all concrete memory caches (`backend/Data/Cache`), Legacy Master classes (`backend/Data/Master`), and Process logic (`backend/Process`).** (Depends on Data).
- **BizSrt.Worker**: Separate BackgroundService process for heavy background processing (e.g. queue consumers). **Crucial Rule:** Because the memory caches are instantiated strictly inside `BizSrt.Api`, any background worker or external process MUST NOT attempt to instantiate or access these caches directly. They must interact with `BizSrt.Api` via endpoints or gRPC to trigger cached calculations (like `IndexCompany`). (Depends on Data).


## Legacy Architecture Overview

The legacy codebase is split into two primary areas:

1. **`legacy/server/` (Backend - C#):** 
   - A highly cached, layered monolith.
   - **Data Layer (`Data/`):** Directly queries EF Core `Entities` and uses heavily cached read-through proxies (e.g., `Cache.cs` and `ReadManyExpirationCache`).
   - **Service Layer (`Data/Service.cs`):** Contains business logic that invokes the Data layer caches and helpers. **When porting items from this legacy file, their modern destination is `backend/Process/` (e.g., `BizSrt.Api.Process.Company`). This avoids confusion with the modern `Service/` layer which maps endpoints to DTOs.**
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
6. **EF Core 10 Query Translation (APPLY vs JOIN):** Legacy EF6 queries that heavily utilized the `let` keyword with multiple conditions (e.g., `let x = dbContext...FirstOrDefault() where x != null && x.Prop != null`) were natively translated into a single optimized `OUTER APPLY`. EF Core 10 parsing can sometimes regress these patterns into multiple redundant scalar subqueries. When porting these explicit "APPLY" patterns, either use standard `join` statements (with `.Distinct()` if necessary) OR explicitly structure `from...Take(1)` subqueries to guarantee performant SQL generation.

   **How to Hint EF Core 10 to generate `CROSS APPLY` / `OUTER APPLY`:**
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

   *Rule of thumb:* If you encounter legacy code using `let x = dbContext.Table.FirstOrDefault(...)`, rewriting it to the second `from` clause with `.Take(1)` is the most reliable way to hint EF Core 10 to generate a clean, single `APPLY` without the "triple-subquery" bug.

   - **`wwwroot/component/`**: Pre-compiled WebComponents and LitElements orchestrating ViewModels and UI.

## Modernization Principles

1. **Strict API Parity:** You must not invent new REST schemas. Your ported .NET Endpoints must exactly match what the legacy TypeScript `src/service/` layers expect.
2. **Schema Name Remapping (Business -> Company):** During modernization, all instances of `Business` in the database schema and object models have been renamed to `Company` (e.g., `Businesses` table became `CompanyProfiles`, `BusinessOffices` became `CompanyOffices`, and LINQ variables `bi` -> `cm`, `bo` -> `co`). All database indexes have also been renamed to use the `Company*` prefix.
3. **ViewModel Preservation:** Do not rip out the legacy `ViewModel` pattern for inputs. Extract the logic from Lit components into modern `frontend/src/viewmodel/` classes to maintain data-flow consistency.
4. **No Novel DB Queries:** All complex EF queries already exist in `legacy/server/Data/`. Port them exactly as they are.
5. **LINQ Building Blocks:** Legacy code constructed complex LINQ queries using reusable building blocks (e.g. `Company.GetActive()`, `Company.GetFiltered()`, `CompanyProduct.GetActive()`). When porting search methods for Company and Company Product profiles, implement these building blocks as reusable extension methods to maintain the legacy architecture.
6. **EF Core 10 Query Translation (APPLY vs JOIN):** Legacy EF6 queries that heavily utilized the `let` keyword with multiple conditions (e.g., `let x = dbContext...FirstOrDefault() where x != null && x.Prop != null`) were natively translated into a single optimized `OUTER APPLY`. EF Core 10 parsing can sometimes regress these patterns into multiple redundant scalar subqueries. When porting these explicit "APPLY" patterns, either use standard `join` statements (with `.Distinct()` if necessary) OR explicitly structure `from...Take(1)` subqueries to guarantee performant SQL generation.

   **How to Hint EF Core 10 to generate `CROSS APPLY` / `OUTER APPLY`:**
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

   *Rule of thumb:* If you encounter legacy code using `let x = dbContext.Table.FirstOrDefault(...)`, rewriting it to the second `from` clause with `.Take(1)` is the most reliable way to hint EF Core 10 to generate a clean, single `APPLY` without the "triple-subquery" bug.

7. **LOB (Large Object) Columns over Network:** When querying entities that have `varbinary(max)` or `nvarchar(max)` columns (like `CompanyMedia.Content`), NEVER query the full entity if you only need the ID. EF Core 10 will attempt to download the multi-megabyte payloads for every record over the network just to populate the unused property. Always use `.Select(m => m.Id).FirstOrDefault()`.
8. **Distinct() vs Any():** Avoid using `.Distinct()` on full entities (e.g., `select c).Distinct()`) when joining tables. EF Core 10 often translates this by fetching *all* scalar columns of the entity into a subquery to compute uniqueness before applying ordering. Instead, use an `.Any()` inside a `where` clause (e.g., `where otherTable.Any(o => o.Id == c.Id)`) which translates cleanly to an `EXISTS` statement.
9. **Temp Data Set Materialization (`ToArrayAsync` vs The Triple-Query Penalty):** When porting dynamic "catch-all" queries (like complex Search methods), avoid re-executing the base query multiple times for counts, facets, and pagination. Instead, project only the `Id` and materialize the dataset into RAM (`var allMatchingIds = await query.Select(p => p.Id).ToArrayAsync();`). Modern EF Core 10 translates `.Contains(allMatchingIds)` in subsequent queries (like facet aggregations) cleanly into an `INNER JOIN OPENJSON(...)` statement. This trades a negligible network/serialization cost for a massive Database CPU saving, exactly mirroring how Stored Procedures use `@TableVariables`.
10. **Cache Eviction Policy (LRU/LFU Hybrid):** Both the legacy system and the modern `BizSrt.Api.Foundation.Cache` utilize `IExpirationItem` implementations across cached models (e.g. `CachedValue`, `CachedSet`). These mandate `HitCount` (total access count) and `LastHit` (a global auto-incrementing access stamp) properties. During capacity cleanups, the Cache `Manager` evaluates `LastHit + HitCount` as a single score to safely identify and evict the lowest value (least popular + stalest) items without complex tracking graphs.
11. **Cache Interface Collisions (`IKey<T>` vs EF Schema):** The legacy database occasionally uses columns named `Key` (e.g. `byte[] Key` in `CompanyFacetSet`). The caching layer expects these models to implement `IKey<T>`, which demands a generic `T Key { get; }` property, creating a compiler collision. **Always resolve this using Explicit Interface Implementation mapped to the Primary Key**, and strictly decorate it with `[NotMapped]`. Legacy EF6 ignored explicit interfaces, but EF Core 10's reflection engine will attempt to map the explicit interface to the DB if `[NotMapped]` is omitted!
    ```csharp
    public int Id { get; set; }
    public byte[] Key { get; set; } // SQL column
    [NotMapped] int IKey<int>.Key => Id; // Cache Interface constraint
    ```
12. **Correlated EXISTS Subqueries in OR Clauses (`.Any()` vs `.Union()`):** When evaluating complex conditions that join large tables (e.g., checking if a Company matches a category natively OR via its associated Products), avoid nesting an EF Core `.Any()` check inside an `OR` clause.
    - **Approach 1 - Retaining `.Any()` (Rejected):** 
      `query.Where(c => c.Category == cat || (from cp in dbContext... select cp).Any(cp => cp.Company == c.Id))`
      *Reason against:* This translates into a SQL `OR EXISTS` subquery. The SQL Server query planner notoriously struggles to use indexes efficiently across `OR EXISTS` conditions over millions of rows, frequently resorting to catastrophic full-table scans and nested-loops that trigger 30-second `Execution Timeout` exceptions in production (highly susceptible to parameter sniffing).
    - **Approach 2 - Server-side `.Union()` (Implemented):** 
      Instead, calculate the two sets independently natively on the server, union them, and feed them into a `.Contains()` (`IN`) clause:
      ```csharp
      var selfMatches = dbContext.CompanyProfiles.Where(c => c.Category == cat).Select(c => c.Id);
      var productMatches = (from cp in dbContext.CompanyProducts where cp.Category == cat select cp.Company);
      var allMatches = selfMatches.Union(productMatches);
      query = query.Where(c => allMatches.Contains(c.Id));
      ```
      *Reason for:* `.Contains()` translates to a SQL `IN` clause (or an `INNER JOIN` against the derived union table). This forces SQL Server to evaluate the two distinct filters independently on their respective indexes before combining them, executing flawlessly in milliseconds and completely bypassing the timeout vulnerabilities.
13. **Eagerly Fetching Small Dimension Tables (`IN` vs `OR EXISTS`):** When checking if a column matches a specific value OR any of its hierarchical children (e.g., `Categories_Unwound`), avoid placing `.Any()` inside an `OR` clause directly in the main EF query.
    - **Approach 1 - Retaining `OR .Any()` (Rejected):** 
      `where c.Category == cat || dbContext.Categories_Unwound.Any(cu => cu.Parent == cat && cu.Child == c.Category)`
      *Reason against:* This forces SQL Server to generate an `OR EXISTS` clause. When joined against massive tables (like `Products` or `CompanyProfiles`), bad parameter sniffing can trick the optimizer into performing a catastrophic nested loop scan, resulting in a 30-second `Execution Timeout Expired`.
    - **Approach 2 - Eager Memory Array & `.Contains()` (Implemented):** 
      Instead, eagerly fetch the small dimension table into memory *before* executing the main query, and then use `.Contains()`:
      ```csharp
      var catIds = await dbContext.Categories_Unwound.Where(cu => cu.Parent == cat).Select(cu => cu.Child).ToListAsync();
      catIds.Add(cat);
      query = query.Where(c => catIds.Contains(c.Category));
      ```
      *Reason for:* Because the `Unwound` dimension table only has a few rows for any given parent, fetching it into memory is virtually instantaneous. EF Core translates the subsequent `.Contains()` into a simple `IN (1, 2, 3...)` clause. This eliminates the `EXISTS` block entirely, guaranteeing the SQL Server uses its covering indexes perfectly without any parameter sniffing timeout vulnerabilities.

## Migration progress. **Please also refer to [LEGACY_BACKEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_BACKEND_TRACKER.md) for a line-by-line backend tracking matrix and [LEGACY_FRONTEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_FRONTEND_TRACKER.md) for the frontend tracking matrix.**

### 1. Backend Services & Data

- [x] **Project Structure & Libraries:** Refactored the monolith into BizSrt.Model, BizSrt.Foundation, BizSrt.Data, BizSrt.Worker, and BizSrt.Api. Handled circular dependencies and enforced InternalsVisibleTo.
- [x] **Background Worker (Indexer):** Scaffolded BizSrt.Worker project. Mapped legacy google.protobuf and gRPC implementation plan to rebuild the IndexCompany polling logic. Configured `NetTopologySuite` for spatial data.
- [x] **Facet Indexing Logic:** Rebuilt the highly complex two-way synchronization loop for `FacetSetCompanies`. Ported `IndexCompanyFacetSetAsync` (for dynamically created Sets Cache queries) and `refreshCompanyFacetSetsAsync` (for recalculating set satisfiability when a company updates its generic facets).
- [x] **Dictionary Caches:** Ported DictionaryItem, DictionaryType, DictionaryCache, and integrated into LegacyCache.

- [x] **Location Infrastructure:** Ported `LocationRef`, `IdName`, `LocationSettings`, `LocationType` enum, and the static `BizSrt.Api.Data.Master.Location` facade.
- [x] **Caching Scaffolding:** Ported base caching logic (`ReadManyExpirationCache`) and instantiated `LegacyCache` singletons.
- [x] **Location Service & Endpoints:** Ported `LocationService` (Resolve, PopulateWithPath) and mapped legacy `OperationExceptionType` / Geocoding appropriately.
- [x] **Category Service:** Ported `CategoryService` resolving namespace collisions for `SubType`.

### 2. Frontend Infrastructure

- [x] **Core Models:** Ported `frontend/src/model.ts`, `frontend/src/model/foundation.ts`, `exception.ts`, and `settings.ts`. Disabled strict mode on these legacy files to ease migration.
- [x] **Base ViewModels:** Rewrote `ViewModel`, `IViewAdapter`, `Validateable`, and `ErrorInfo` without jQuery dependencies in `frontend/src/viewmodel.ts`.
- [x] **Component ViewModels:** Extracted `<search-category-input>` and `<search-location-input>` logic into `frontend/src/viewmodel/search/category/input.ts` and `frontend/src/viewmodel/location/input.ts`.
- [x] **Lit Components:** Upgraded `SearchCategoryInput` and `SearchLocationInput` to use WebAwesome UI and wire into their modern ViewModels via `IViewAdapter`.
- [x] **Navigation & Routing:** Ported the legacy `Navigation` structure into `frontend/src/navigation.ts`. 
  - *Purpose:* Replaced hardcoded `window.location.href` redirects across Lit components with domain-specific namespaces (e.g., `Company.profileView()`, `Product.search()`). 
  - *Implementation:* These semantic helper methods construct parameter bags (handling search queries, location, and entity IDs) and pipe them into a global `Navigation.go()` dispatcher. This triggers a bubbling `app-navigate` CustomEvent, caught by a React boundary (`NavigationProvider.tsx`), mapping natively to Next.js's `useRouter()` to preserve SPA-style soft navigation without full page reloads.

### 3. Company Profile Infrastructure

- [x] **`CompanyProfilesCache`:** Ported to `backend/Data/Cache/Company/Profile.cs`. **Performance Note:** Fixed a massive memory/network leak. When querying `CompanyMedia` to check if a default image exists, we must project only the `Id` (`.Select(m => m.Id)`). Fetching the entire `CompanyMedia` entity downloads the `varbinary(max)` blob payload over the network for every company, taking `toPreview` from ~300ms to over 3 seconds. 
- [x] **`FeaturedCompaniesCache`:** Ported to `backend/Data/Cache/Company/FeaturedCompaniesCache.cs`. Keyed by `(Category, Location)`. **Performance Note:** Fixed two massive EF Core 10 query generation issues. 
   1. Replaced `.Distinct()` and `join` for `CompanyOffices` with a native `coq.Any(co => co.Company == c.Id)`. EF Core 10 translates `.Distinct()` on entities by fetching *all* columns just to check uniqueness.
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

### 5. Product Infrastructure & Components

- [x] **`product-home` & `company-product`:** Ported the main product browsing pages, utilizing the modern reactive framework.
- [x] **`product-slider` & `product-featured`:** Ported the product carousel components used within company profiles and product home pages.
- [x] **Global Building Blocks (`image-view`, `richtext-view`):** Ported legacy global UI blocks replacing `unsafeHTML` fallbacks with properly reactive custom elements.
- [x] **Product Endpoints:** Verified `CompanyProductService` (`SearchAsync`, `GetFeaturedAsync`, `ToPreviewAsync`) is fully implemented in the .NET 10 backend and mapped in `ProductEndpoints.cs`.

### 6. Foundation & Base Caches

- [x] **`FeaturedCache<TItems>` Restructuring**: Created a modern base class (`backend/Data/Cache/Featured/Featured.cs`) for all "featured" caches to eliminate code duplication across companies and products, managing dirty invalidation across hierarchical folders using a concurrent dictionary of timestamps.
- [x] **Text Normalization (`TextConverter` & `WordBreaker`)**: Fully ported legacy `TextConverter` and `WordBreaker` to `backend/Foundation/`. Restored `CheckHtml` (along with `IRichText`), `Normalize`, and `VarcharMax` (decoupled from `Settings`).
- [x] **Dynamic Property Bags (`Preview` Models)**: Replaced legacy string-based `Newtonsoft.Json` dictionary property mappings with robust `System.Text.Json.Serialization.JsonExtensionData` property bags (`Dictionary<string, object> Properties`). Added strongly typed fallback properties (`Distance`, `UnlistedType`, `Status`) without explicit indexers.

### Pending Tasks

- [x] Port remaining company pages: `company/search.ts`, `company/profile.ts`, `company/header-layout.ts`.
- [x] Fix Google Maps API Loader version incompatibility in `components/search/location/input.ts` (uses deprecated `Loader` class — must switch to functional `setOptions()`/`importLibrary()` API).
- [ ] Further migration of remaining legacy frontend modules (see `LEGACY_FRONTEND_TRACKER.md`).

