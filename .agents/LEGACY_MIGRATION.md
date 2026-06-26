# BizSort Legacy Codebase & Migration Tracking

This document provides a comprehensive overview of the legacy BizSort architecture and tracks the modernization progress. **Please also refer to [LEGACY_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_TRACKER.md) for a line-by-line file tracking matrix.**for the new Next.js / .NET 8 codebase. **All agents must review this file when deciding how to port or where to place code.**

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
2. **ViewModel Preservation:** Do not rip out the legacy `ViewModel` pattern for inputs. Extract the logic from Lit components into modern `frontend/src/viewmodel/` classes to maintain data-flow consistency.
3. **No Novel DB Queries:** All complex EF queries already exist in `legacy/server/Data/`. Port them exactly as they are.

## Migration progress. **Please also refer to [LEGACY_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_TRACKER.md) for a line-by-line file tracking matrix.**Tracking

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

### Pending Tasks
- [ ] Port the complete legacy `CompanyProfilesCache` and `CachedCompanyProfile`.
- [ ] Port the legacy `Company.Profile` Endpoint `/profile/toPreview` properly utilizing the `CompanyProfilesCache`.
- [ ] Further migration of legacy frontend `service` layers.
