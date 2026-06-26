# BizSort Project Conventions

This file contains structural and naming conventions that all agents must follow to ensure consistency with the legacy architecture.

**CRITICAL:** You must also reference [LEGACY_MIGRATION.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_MIGRATION.md) for a complete overview of the legacy architecture and the ongoing modernization progress. This tracker file is the absolute source of truth for what has been ported (e.g. ViewModels, Components, Caching) and what is pending.

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
- **Legacy API Parity:** You must strictly follow the legacy API semantics for all pages and APIs you port. Ensure endpoints match the exact names, query parameters, and payload structures expected by the legacy frontend code unless explicitly asked to change them. This is critical to avoid breaking the modernized frontend that relies on legacy schemas.
- **Method Naming:** Modernized backend methods (e.g., in `Service` or `Data` layers) MUST strictly match the exact names of the legacy methods they are porting, simply appending `Async`. (e.g. legacy `ToPreview` becomes `ToPreviewAsync`, legacy `View` becomes `ViewAsync`. Do NOT invent new descriptive names like `GetCompanyPreviewsAsync`).
- **No Novel Implementations:** Do not improvise or write new LINQ queries, services, or endpoints from scratch. All necessary queries and logic already exist in the legacy codebase (e.g. `..\legacy\server\Data`). You must find and port the existing queries directly to ensure database constraints and logic perfectly match.

### 2. Caching Scaffolding
- **Legacy Caching:** The legacy backend extensively utilizes memory caching (e.g., `ReadManyExpirationCache`). When porting data access logic, you must check if the legacy system used a cache for the entity.
- **Do Not Bypass Cache:** Do NOT hit the database directly via EF Core in the modern `Service` classes if the legacy implementation relied on cache.
- **Cache Porting Approach:** Scaffold and port the required cache mechanism. Use the modernized `BizSrt.Api.Data.Cache.ReadManyExpirationCache<TKey, TValue>` base class. Create specific cache implementations (e.g., `CompanyProfilesCache`), define the corresponding `Cached*` models (porting their mapping methods like `ToPreview()`), and register the caches as Singletons in `Program.cs`.

## Frontend Modernization Rules

### 1. API Helper Abstractions
- **Port Legacy Service Helpers:** For EVERY API call required by the frontend, you MUST check the legacy codebase in `..\legacy\website\wwwroot\src\service\` (e.g. `company.ts`, `product.ts`, etc.).
- **No Raw Fetch Calls:** Do NOT improvise or write new inline `fetch()` calls directly inside React components or Lit elements. 
- **Maintain Method Names:** Find the exact legacy helper method, port it to the modern `frontend/src/service/` directory, and use that abstracted function. Maintain the legacy method name (e.g., `view()`, `getFeatured()`, `toPreview()`) and logic to ensure complete parity with the legacy UI data flow.

### 2. WebAwesome (Shoelace v3.0) Nuances & Gotchas
- **Icon Loading:** By default, WebAwesome fetches icons from `ka-f.fontawesome.com`. Do NOT override the `setIconPath()` to other CDNs (like jsdelivr) unless absolutely necessary, as it expects a specific Alpha folder structure for FontAwesome 7 and custom paths will break all icon rendering.
- **Dropdown Item Slots:** WebAwesome renamed their icon slots! Older Shoelace used `slot="prefix"` and `slot="suffix"`, but `wa-dropdown-item` now explicitly expects `slot="icon"` for icons. If you use `start` or `prefix`, the icon will be completely swallowed by the Shadow DOM projection and will not render in the tree.
- **System Icons:** WebAwesome bundles a small set of "system" icons directly into its Javascript (e.g. `search`, `ellipsis-vertical`, `chevron-down`). If you are trying to render one of these core UI icons and it's missing, ensure you add the `library="system"` attribute (e.g. `<wa-icon name="ellipsis-vertical" library="system"></wa-icon>`).
- **Styling Custom Elements (STRICT RULE):** WebAwesome strictly controls its internal component styling and state interactions (like `hover`, `active`, `focus`) via dynamic `color-mix()` CSS variables. 
  - **NEVER** target internal parts like `::part(base)` to manually override `background-color` or `color`. Doing so will permanently destroy the component's built-in interaction states!
  - **ALWAYS** search the WebAwesome source code (e.g. `node_modules/@awesome.me/webawesome/dist/chunks/`) to find the exact internal CSS variables the component expects (e.g., `--wa-color-neutral-fill-loud`, `--wa-color-fill-normal`) and pipe your custom themes into those variables on the host component instead.
  - When styling slotted elements like `<wa-dropdown-item>`, use the `::slotted()` pseudo-element on the host and override the specific `--wa-color-neutral-*` CSS variables to enforce text colors.

### 3. Namespace Collision & Caching Pitfalls (Backend)
- **Importance of Existing Namespaces:** The legacy backend uses a highly structured domain-driven namespace design (`BizSrt.Api.Data.*`, `BizSrt.Api.Model.*`). It is crucial that you place your modernized files in the exact same matching folders to inherit the correct namespaces. If you invent new namespaces or place files arbitrarily, you will cause catastrophic compiler errors across the large monolith.
- **Shared Class Names:** Be extremely cautious of identical class names that exist across different namespaces (e.g., `BizSrt.Api.Data.Entities.Category` vs. `BizSrt.Api.Model.Legacy.Category` vs `BizSrt.Api.Data.Master.Category`). The C# compiler will resolve them incorrectly or complain about ambiguous references if `using` directives overlap.
- **Fully Qualify Entities:** When porting legacy LINQ queries or Cache accessors, always fully qualify the generic arguments, class names, or EF Core DbSet references if there's any risk of namespace collision (e.g. `System.Exception` vs `Foundation.Exception.Exception`).
- **Anonymous Types & Type Inference:** If a LINQ `join` into an anonymous type fails type inference (`CS1941`), check if the underlying property types perfectly match. `short` vs `short?` vs `int` across different namespaces will break `GroupJoin` or `Join` clauses.
- **Cache Singletons:** When accessing caches like `LegacyCache.Categories`, ensure you don't confuse the cache property with the underlying entity namespace. If C# confuses `BizSrt.Api.Data.Cache.LegacyCache.Categories` with a namespace resolution error, explicitly use an alias like `using LegacyCache = BizSrt.Api.Data.Cache.LegacyCache;` and call `LegacyCache.Categories`.

**CRITICAL:** Always consult [LEGACY_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_TRACKER.md) to track porting status of specific files.
