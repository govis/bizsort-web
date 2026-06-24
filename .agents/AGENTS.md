# BizSort Project Conventions

This file contains structural and naming conventions that all agents must follow to ensure consistency with the legacy architecture.

## Legacy Compatibility Rules

### 1. Folder Structure
- **Singular Naming:** Always use singular names for architectural folders. Use `Model`, `Data`, `Service`, `Endpoint` rather than `Models`, `Data`, `Services`, `Endpoints`.
- **Domain-Driven Nesting:** Group related files under their domain folder when possible (e.g., `src/company/`, `src/components/search/`).

### 2. File Naming
- **No Redundant Suffixes:** Do not append architectural suffixes to filenames. For example, use `Company.cs` instead of `CompanyModels.cs`, `CompanyService.cs`, or `CompanyEndpoints.cs`. 
- **Frontend Components:** 
  - Page-level Lit elements go into `src/company/` (or the respective domain directory replacing the legacy `wwwroot/company/`).
  - Reusable building blocks go into `src/components/` (and its subdirectories like `src/components/layout/` or `src/components/search/`, mapping to legacy `wwwroot/component/`).

### 3. Namespace Conventions (Backend)
- Backend namespaces strictly follow the singular naming convention:
  - `BizSrt.Api.Model`
  - `BizSrt.Api.Service`
  - `BizSrt.Api.Endpoint`
  - `BizSrt.Api.Data`

### 4. Subagent Concurrency (Claude API Limits)
- **NEVER** launch more than one subagent in parallel.
- If a task requires multiple subagents, you must invoke them sequentially. Wait for the first subagent to finish and report back before using `invoke_subagent` for the next one.

## Backend Modernization Rules

### 1. API Semantics & Naming Conventions
- **Legacy API Parity:** You must strictly follow the legacy API semantics for all pages and APIs you port. Ensure endpoints match the exact names, query parameters, and payload structures expected by the legacy frontend code unless explicitly asked to change them.
- **Method Naming:** Modernized backend methods (e.g., in `Service` or `Data` layers) MUST strictly match the exact names of the legacy methods they are porting, simply appending `Async`. (e.g. legacy `ToPreview` becomes `ToPreviewAsync`, legacy `View` becomes `ViewAsync`. Do NOT invent new descriptive names like `GetCompanyPreviewsAsync`).
- **Porting Queries:** Do not improvise or write new LINQ queries from scratch for backend APIs. All necessary LINQ queries already exist in the legacy codebase (e.g. `..\legacy\server\Data`). You must find and port the existing queries directly to ensure database constraints and logic match.

### 2. Caching Scaffolding
- **Legacy Caching:** The legacy backend extensively utilizes memory caching (e.g., `ReadManyExpirationCache`). When porting data access logic, you must check if the legacy system used a cache for the entity.
- **Do Not Bypass Cache:** Do NOT hit the database directly via EF Core in the modern `Service` classes if the legacy implementation relied on cache.
- **Cache Porting Approach:** Scaffold and port the required cache mechanism. Use the modernized `BizSrt.Api.Data.Cache.ReadManyExpirationCache<TKey, TValue>` base class. Create specific cache implementations (e.g., `CompanyProfilesCache`), define the corresponding `Cached*` models (porting their mapping methods like `ToPreview()`), and register the caches as Singletons in `Program.cs`.
