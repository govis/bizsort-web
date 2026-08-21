# BizSort Project Conventions

This file contains structural and naming conventions that all agents must follow to ensure consistency with the legacy architecture.

**CRITICAL:** You must also reference [LEGACY_MIGRATION.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_MIGRATION.md) for a complete overview of the legacy architecture and the ongoing modernization progress. This tracker file is the absolute source of truth for what has been ported (e.g. ViewModels, Components, Caching) and what is pending.

**CRITICAL: FRONTEND TARGET AND LEGACY PATH**
- **Astro is the Active Branch:** Astro (\rontend-astro\) is the sole active frontend development branch and the official target for UI modernization. The \rontend-nextjs\ and \rontend-nuxt\ directories were architectural experiments and are now **shelved**. Do not write code for them.
- **Legacy Source Locations:** The legacy codebase is NOT located inside the \izsort-web\ repository. It is located externally:
  - **Frontend:** \C:\Bizsort\legacy\website\ (contains legacy Polymer components, \wwwroot\, tokens, endpoints mapping).
  - **Backend / Background:** \C:\Bizsort\legacy\server\ (contains legacy C# services, models, data access, and worker engines).

## Legacy Compatibility Rules

### 1. Folder Structure
- **Singular Naming:** Always use singular names for architectural folders. Use \Model\, \Data\, \Service\, \Endpoint\ rather than \Models\, \Data\, \Services\, \Endpoints\.
- **Domain-Driven Nesting:** Group related files under their domain folder when possible (e.g., \src/company/\, \src/components/search/\).

### 2. File Naming
- **No Redundant Suffixes:** Do not append architectural suffixes to filenames. For example, use \Company.cs\ instead of \CompanyModels.cs\, \CompanyService.cs\, or \CompanyEndpoints.cs\. 
- **Frontend Components:** 
  - Page-level Lit elements go into \src/company/\ (or the respective domain directory replacing the legacy \wwwroot/company/\).
  - Reusable building blocks go into \src/components/\ (and its subdirectories like \src/components/layout/\ or \src/components/search/\, mapping to legacy \wwwroot/component/\).

### 3. Namespace Conventions (Backend)
- Backend namespaces strictly follow the singular naming convention:
  - `BizSrt.Api.Model`
  - `BizSrt.Api.Service`
  - `BizSrt.Api.Endpoint`
  - `BizSrt.Api.Data`
- **Admin UI DTOs:** All models used for backend Admin UI endpoints must be placed in the `BizSrt.Model.Admin.*` namespace (e.g. `BizSrt.Model.Admin.Offering.SaveRequest`). Do NOT reuse the public-facing ViewModels (e.g., `BizSrt.Model.Offering.Profile`) for data entry forms, as they have been flattened to include complex UI navigation properties.

### 4. Database Schema Remapping
- **Business -> Company:** The legacy database heavily used the \Business\ domain terminology (e.g. \Businesses\ table, \BusinessOffices\). This has been completely modernized to \Company\. When porting queries, remap legacy table names to \CompanyProfiles\, \CompanyMedia\, \CompanyOffices\, etc., and ensure LINQ aliases use updated abbreviations (\ i\ becomes \cm\, \ o\ becomes \co\).

### 5. Subagent Concurrency (Claude API Limits)
- **NEVER** launch more than one subagent in parallel.
- If a task requires multiple subagents, you must invoke them sequentially. Wait for the first subagent to finish and report back before using \invoke_subagent\ for the next one.


### 6. Enums and Magic Numbers
- **No Magic Numbers:** Never hardcode "magic numbers" in the UI (e.g., `if (type === 41)`). You MUST always find, port, and use the explicit Enums from the legacy codebase.
- **Preserve Enum Integer Mappings:** When porting enums from the legacy frontend (`legacy\website\wwwroot\src\`) or C# backend to the modern Astro frontend, you MUST preserve their exact legacy integer values (e.g., `Argument_Invalid = 41`). Do not let TypeScript auto-increment them from zero. If the integers diverge, API error parsing and JSON serialization will silently break.

## Backend Modernization Rules

### 1. API Semantics & Naming Conventions
- **Legacy API Parity:** You must strictly follow the legacy API semantics for all pages and APIs you port. Ensure endpoints match the exact names, query parameters, and payload structures expected by the legacy frontend code unless explicitly asked to change them. This is critical to avoid breaking the modernized frontend that relies on legacy schemas.
- **Method Naming:** Modernized backend methods (e.g., in \Service\ or \Data\ layers) MUST strictly match the exact names of the legacy methods they are porting, simply appending \Async\. (e.g. legacy \ToPreview\ becomes \ToPreviewAsync\, legacy \View\ becomes \ViewAsync\. Do NOT invent new descriptive names like \GetCompanyPreviewsAsync\).
- **No Novel Implementations:** Do not improvise or write new LINQ queries, services, or endpoints from scratch. All necessary queries and logic already exist in the legacy codebase (e.g. \..\legacy\server\Data\). You must find and port the existing queries directly to ensure database constraints and logic perfectly match.

### 2. Caching Scaffolding
- **Legacy Caching:** The legacy backend extensively utilizes memory caching (e.g., \ReadManyExpirationCache\). When porting data access logic, you must check if the legacy system used a cache for the entity.
- **Do Not Bypass Cache:** Do NOT hit the database directly via EF Core in the modern \Service\ classes if the legacy implementation relied on cache.
- **Cache Porting Approach:** Scaffold and port the required cache mechanism. Use the modernized \BizSrt.Api.Data.Cache.ReadManyExpirationCache<TKey, TValue>\ base class. Create specific cache implementations (e.g., \CompanyProfilesCache\), define the corresponding \Cached*\ models (porting their mapping methods like \ToPreview()\), and register the caches as Singletons in \Program.cs\.

### 3. Namespace Collision & Caching Pitfalls
- **Importance of Existing Namespaces:** The legacy backend uses a highly structured domain-driven namespace design (\BizSrt.Api.Data.*\, \BizSrt.Api.Model.*\). It is crucial that you place your modernized files in the exact same matching folders to inherit the correct namespaces. If you invent new namespaces or place files arbitrarily, you will cause catastrophic compiler errors across the large monolith.
- **Shared Class Names:** Be extremely cautious of identical class names that exist across different namespaces (e.g., \BizSrt.Api.Data.Entities.Category\ vs. \BizSrt.Api.Model.Legacy.Category\ vs \BizSrt.Api.Data.Master.Category\). The C# compiler will resolve them incorrectly or complain about ambiguous references if \using\ directives overlap.
- **Fully Qualify Entities:** When porting legacy LINQ queries or Cache accessors, always fully qualify the generic arguments, class names, or EF Core DbSet references if there's any risk of namespace collision (e.g. \System.Exception\ vs \Foundation.Exception.Exception\).
- **Anonymous Types & Type Inference:** If a LINQ \join\ into an anonymous type fails type inference (\CS1941\), check if the underlying property types perfectly match. \short\ vs \short?\ vs \int\ across different namespaces will break \GroupJoin\ or \Join\ clauses.
- **Cache Singletons:** When accessing caches like \LegacyCache.Categories\, ensure you don't confuse the cache property with the underlying entity namespace. If C# confuses \BizSrt.Api.Data.Cache.LegacyCache.Categories\ with a namespace resolution error, explicitly use an alias like \using LegacyCache = BizSrt.Api.Data.Cache.LegacyCache;\ and call \LegacyCache.Categories\.

**CRITICAL:** Always consult [LEGACY_BACKEND_TRACKER.md](file:///C:/Bizsort/bizsort-web/.agents/LEGACY_BACKEND_TRACKER.md) to track porting status of specific files.

## Frontend Modernization Rules

### 1. API Helper Abstractions
- **Port Legacy Service Helpers:** For EVERY API call required by the frontend, you MUST check the legacy codebase in `..\legacy\website\wwwroot\src\service\` (e.g. `company.ts`, `offering.ts`, etc.).
- **No Raw Fetch Calls:** Do NOT improvise or write new inline `fetch()` calls directly inside React components or Lit elements. 
- **Maintain Method Names:** Find the exact legacy helper method, port it to the modern `frontend/src/service/` directory, and use that abstracted function. Maintain the legacy method name (e.g., `view()`, `getFeatured()`, `toPreview()`) and logic to ensure complete parity with the legacy UI data flow.
- **Strip Frontend Caching:** Note that the legacy `..\legacy\website\wwwroot\src\service\` files contained complex internal caching mechanisms (e.g., storing responses in dictionaries or local variables). These mechanisms are being simplified and **stripped out** during modernization. The modern service layer should be stateless pure functions that just `fetch()` and return the response.

### 2. WebAwesome (Shoelace v3.0) Nuances & Gotchas
- **CRITICAL RULE:** Do not invent your own UI patterns. You MUST read the **WebAwesome v3 Guidelines** skill (located at \C:\Bizsort\bizsort-web\.agents\skills\webawesome_v3\SKILL.md\) whenever interacting with or styling WebAwesome components.

### 3. Lit & TypeScript Gotchas (Class Field Shadowing)
- **CRITICAL:** When compiling Lit components with \useDefineForClassFields: true\ (standard in Next.js/Vite TS configs), you MUST use the \declare\ keyword for all \@property()\ and \@state()\ fields.
  - **Bad:** \@property() active = false;\ (TS will compile this to a native class field, completely destroying Lit's reactive getters/setters, causing the component to silently fail to re-render when state changes).
  - **Good:** \@property() declare active: boolean;\ (Initializations should be moved to the \constructor()\).
- **First Render DOM Access:** \@query\ elements and other DOM nodes do not exist when \ender()\ is first called. If you need to pass a DOM node to a child component (e.g., passing \.anchorElement=\${this.inputElement}\ to a popup), \	his.inputElement\ will be undefined on the first render. You must call \	his.requestUpdate()\ inside \irstUpdated()\ to force a second render so the child receives the actual DOM node.
- **Vite/esbuild Type Erasure (import type):** Unlike Next.js (SWC) which deeply analyzes dependencies, Astro/Vite uses \esbuild\ which strictly transpiles files in isolation. If you import a TypeScript \interface\ (like \IdName\ from \model/foundation\) without the \	ype\ modifier, Vite assumes it's a value and leaves the import in the bundled Javascript. The browser will then throw an \Uncaught SyntaxError: ... does not provide an export named ...\. You **MUST** use the inline type modifier (e.g. \import { type IdName, MyClass }\) for any interfaces or types to ensure they are stripped from the runtime bundle.

**CRITICAL:** For a comprehensive deep-dive into how the modern search UI, routing, component lifecycles, and backend cache APIs perfectly mirror the legacy search architecture, always read [SEARCH_ARCHITECTURE.md](file:///C:/Bizsort/bizsort-web/.agents/docs/SEARCH_ARCHITECTURE.md) before making modifications to `search-home`, `search-category-input`, or `search-location-input`.

**CRITICAL:** For detailed frontend architectural guidelines including MVVM separation, CSS animations, URL state syncing, and Header layouts, refer to [FRONTEND_ARCHITECTURE.md](file:///C:/Bizsort/bizsort-web/.agents/docs/FRONTEND_ARCHITECTURE.md).

**CRITICAL:** For guidelines regarding the transition from legacy rich OOP models to modern plain interfaces (and the resulting stripped getters/methods), refer to [DATA_MODEL_ARCHITECTURE.md](file:///C:/Bizsort/bizsort-web/.agents/docs/DATA_MODEL_ARCHITECTURE.md).
### 6. "Featured" Methods Dual-Context (Global vs Company-Scoped)
- **CRITICAL:** Be aware that there are two entirely separate contexts for getFeatured (and similarly for Search or View):
  1. **Global/Master Context:** Ported from C:\Bizsort\legacy\server\Service\Product\Profile.cs (e.g., offering/featured). This is used for global pages like offering-home sliders.
  2. **Company-Scoped Context:** Ported from C:\Bizsort\legacy\server\Service\Company\Product.cs (e.g., company/offering/featured). This is scoped to a specific company and used on the company-profile page to list a company's specific featured offerings.
- The same dual-context pattern will apply to Projects and Jobs when they are ported. Always verify which context you are working in to map to the correct backend service and frontend component.

### 7. "toPreview" Global Context (Hydration)
- **CRITICAL:** The 	oPreview API is inherently a **global** operation because it takes an array of globally-unique Entity IDs (e.g. [ {id: 123}, {id: 456} ]) and hydrates them into rich Preview models for cards.
- Therefore, 	oPreview MUST be ported from the **Master/Global** legacy service (C:\Bizsort\legacy\server\Service\Product\Profile.cs for backend, and C:\Bizsort\legacy\website\wwwroot\src\service\product.ts for frontend).
- Do NOT port 	oPreview from the Company-scoped services. This global 	oPreview pattern applies equally to Offerings, Projects, and Jobs.

