# BizSort Web Implementation Documentation

This document describes the modernization of the BizSort Web application, migrating from a legacy Polymer/Material Web Components stack to a modern architecture using .NET 10, Next.js 16, Lit 3.3, Web Awesome, and .NET Aspire.

## 1. System Architecture

The application is structured as a distributed system orchestrated by **.NET Aspire**.

### Components:
- **AppHost (`/apphost`)**: The .NET Aspire orchestrator. It manages the lifecycle of the backend and frontend, handles port allocation, and injects service discovery environment variables.
- **Backend (`/backend`)**: A .NET 10 Minimal API. It handles business logic, data access via Entity Framework Core (SQL Server with NetTopologySuite), and image processing via ImageSharp.
- **Worker (`/background`)**: A .NET BackgroundService application. It contains the legacy `Engine` polling workers that handle heavy asynchronous processing (like indexing and facet set calculations) and synchronizes updates with the Backend API via **gRPC**.
- **Frontend (`/frontend`)**: A Next.js 16 application. It uses Lit 3.3 for web components and Web Awesome (`wa-` prefix) for UI elements, maintaining compatibility with the legacy component-based logic while utilizing modern React-based routing and SSR capabilities.

## 2. Backend (C#)

The backend was ported from the `Service`, `Model`, `Data`, and `Dev` folders in the `..\legacy\server\` directory. This legacy code was migrated to a modern **Minimal API** structure. It is required to verify endpoint and data format consistency when making changes to models or services to ensure compatibility with the ported UI components.

### Key Porting Strategies:
- **Legacy Model Retention**: Models from the legacy `Model` and `Data` directories were re-implemented in `backend/Models/` to ensure the JSON payloads remain identical to the legacy system. This prevents breaking changes for the ported UI components.
- **Minimal API Mapping**: Legacy `ApiController` routes (e.g., `svc/company/profile/view`) were mapped to `app.MapGet` in `backend/Endpoints/CompanyEndpoints.cs`.
- **Entity Framework Core**: The `AppDbContext` supports 16 entity types with complex relationships (CompanyProfile, Offices, Products, Projects, Jobs, Communities, Affiliations, Promotions, Media).
- **Service Layer**: `CompanyService` (14 methods) handles complex DTO mapping while utilizing modern C# features. `ImageService` handles on-the-fly image resizing via ImageSharp.

### Memory Caching & gRPC Interface:
- **Cache Ownership**: The `LegacyCache` system and all concrete memory caches (e.g., `CompanyProfilesCache`, `SetsCache`) are **fully owned and managed exclusively** by the Backend API process. These singletons cannot be instantiated or mutated by external processes.
- **gRPC Interface**: To allow out-of-process background workers (from `/background`) to trigger cache-dependent operations (like `IndexCompany` or `IndexProductFacetSet`), the Backend exposes a **gRPC Service** (`CompanyGrpcService`). Background workers push commands via gRPC, ensuring that all complex indexing logic and cache synchronization executes natively within the API process where the cache singletons reside.
> 👉 **For a deep dive into the cache eviction policies, `SetsCache` side-effects, and `IKey<T>` EF Core mappings, see [CACHE_ARCHITECTURE.md](file:///C:/Bizsort/bizsort-web/backend/CACHE_ARCHITECTURE.md).**

### Data Structure Mapping:
| Legacy Concept | New Implementation | Location |
| :--- | :--- | :--- |
| `EntityId<T>`, `IdName<T>` | `EntityId<T>`, `IdName<T>` | `LegacyModels.cs` |
| `Company.Profile` | `Profile` DTO | `CompanyModels.cs` |
| `Office` | `Office` DTO | `CompanyModels.cs` |
| `Product.Profile` | `Profile` DTO | `ProductModels.cs` |
| `Project.Profile` | `Profile` DTO | `ProjectModels.cs` |
| `Job.Profile` | `Profile` DTO | `JobModels.cs` |
| `Promotion.Preview` | `Preview` DTO | `PromotionModels.cs` |
| `svc/company/profile` | `/api/company/profile` | `CompanyEndpoints.cs` |
| Image serving | `/api/image/get` | `ImageEndpoints.cs` |

### API Endpoints:
- **Company** (`/api/company`): profile/view, profile/getFeatured, profile/search, profile/getCommunities, profile/getAffiliations, profile/getProducts, profile/getProjects, profile/getJobs, profile/getPromotions, profile/getInfo, profile/newProfiles, product/getFeatured, product/view, job/view, project/view
- **Image** (`/api/image`): get (with resize), captcha

### Database:
- SQL Server (local instance, database `BizSort`)
- Connection configured in `appsettings.json`

## 3. Frontend (TypeScript/Lit/Web Awesome)

The frontend uses **Next.js** as the host framework but utilizes **Lit** for the core UI logic to facilitate a smooth port from the legacy Polymer components.

### UI Stack:
- **Next.js 16**: App Router, Dynamic Imports, and SSR.
- **Lit 3.3**: Used for porting legacy Polymer logic (`.ts` components).
- **Web Awesome 3.8** (`@awesome.me/webawesome`): Used for standardized, accessible UI components (`wa-card`, `wa-select`, `wa-tab-group`, etc.).

### Porting Process:
1. **Remove Tailwind**: To align with the custom Lit/Web Awesome styling strategy, Tailwind CSS was removed.
2. **Lit Components**: Legacy components (e.g., `company/profile.ts`) were rewritten as modern Lit classes.
3. **Web Awesome Integration**: Legacy `@material/web` (`md-`) and `paper-` components were replaced with Web Awesome (`wa-`) equivalents.
4. **React Wrapper**: Since Lit components are Custom Elements, they are integrated into the Next.js App Router via a React wrapper (`CompanyProfileWrapper.tsx`) that uses `next/dynamic` with `ssr: false` to ensure they only render on the client.

### Component Structure:
- `frontend/src/components/types.ts`: Shared TypeScript interfaces (`Company`, `Office`, `Location`, `Category`, `Offerings`).
- `frontend/src/components/company-profile.ts`: The core Lit component — company profile viewer with tabs, contact info, map, and office switching.
- `frontend/src/components/CompanyProfileWrapper.tsx`: The Client Component that bridges Lit custom elements into React.
- `frontend/src/components/company-header-layout.ts`: Reusable header layout with logo, title, and slots for navbar/dropdown/tabs.
- `frontend/src/components/layout-card.ts`: Generic card wrapper using `wa-card` with Material Design-like shadow styling.
- `frontend/src/components/page-menu.ts`: 3-dot dropdown menu component.
- `frontend/src/components/search-box.ts`: Search input styled for dark header backgrounds.
- `frontend/src/components/search-category-menu.ts`: Category action dropdown (stub).
- `frontend/src/app/page.tsx`: The Next.js entry point rendering the profile for a specific `companyId`.

## 4. Orchestration (.NET Aspire)

**.NET Aspire** is used to unify the development experience.

### Service Discovery:
The AppHost automatically allocates ports for the backend. In `apphost/Program.cs`, this URL is mapped to the `NEXT_PUBLIC_API_URL` environment variable:
```csharp
builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithReference(backend)
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("NEXT_PUBLIC_API_URL", backend.GetEndpoint("http"))
    .WithExternalHttpEndpoints();
```

### Running the Project:
1. Run `dotnet run --project apphost/BizSrt.AppHost.csproj`.
2. Access the Aspire Dashboard to view logs and endpoint URLs.

## 5. Background Workers & Data Processing

The heavy asynchronous processing formerly handled by the legacy monolithic `Engine` namespace has been decoupled into the `/background` worker project (`BizSrt.Worker`). 

### Polling & gRPC Architecture
- **No Direct Cache Access**: Because the memory caches (`LegacyCache`) are tightly coupled and instantiated strictly inside the `BizSrt.Api` process, background workers cannot instantiate or interact with them directly. 
- **Database Polling (`AsyncQueueWorker<T>`)**: Workers (such as `Company.Indexer` or `Product.FacetSetWorker`) execute timer sweeps querying the `AppDbContext` directly via Entity Framework Core to discover stale entities (e.g., polling `CompanyProfiles` where the `Indexed` timestamp is outdated). They do **not** pull work items from a traditional message broker for these scheduled synchronization tasks.
- **gRPC Pushing**: Once a worker determines that an entity requires indexing or recalculation, it constructs a payload and **pushes** a request to the `backend` API over **gRPC** (e.g., `_grpcClient.IndexCompanyAsync()`). The backend API then executes the complex business logic (such as facet generation, spatial math, and read-through cache invalidation) natively within the API process where all singletons reside.

## 6. Current Status & TODOs:
- **Company Profile**: The profile page (`About`, `Products and Services`, `Articles` tabs) is ported and functional against the live database.
- **Building Blocks**: Reusable components (`company-header-layout`, `layout-card`, `page-menu`, `search-box`, `search-category-menu`) are created but not yet composed into the main profile component.
- **SSL Trust**: In some CLI environments, `https` profiles may fail due to untrusted dev certificates. Use the `http` profile if necessary.
- **Remaining Pages**: Additional legacy pages (Jobs, Products, Projects, Promotions, News, Articles, Marketplace, Search, Home) need UI porting. Backend endpoints for these already exist.
- **Featured Sections**: Product slider, affiliations slider, and communities slider on the profile page are not yet ported.
- **Company Logo**: Currently uses placeholder text; should be wired to `/api/image/get`.
- **SEO**: Legacy JSON-LD, breadcrumbs, and meta tags need to be ported to Next.js `metadata` exports.
