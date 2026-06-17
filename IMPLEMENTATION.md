# BizSort Web Implementation Documentation

This document describes the modernization of the BizSort Web application, migrating from a legacy Polymer/WCF/Web API stack to a modern architecture using .NET 10, Next.js, Lit, Shoelace, and .NET Aspire.

## 1. System Architecture

The application is structured as a distributed system orchestrated by **.NET Aspire**.

### Components:
- **AppHost (`/apphost`)**: The .NET Aspire orchestrator. It manages the lifecycle of the backend and frontend, handles port allocation, and injects service discovery environment variables.
- **Backend (`/backend`)**: A .NET 10 Minimal API. It handles business logic, data access via Entity Framework Core (SQL Server), and image processing via ImageSharp.
- **Frontend (`/frontend`)**: A Next.js 16 application. It uses Lit for web components and Shoelace for UI elements, maintaining compatibility with the legacy component-based logic while utilizing modern React-based routing and SSR capabilities.

## 2. Backend Migration (C#)

The backend was ported from the legacy `BizSrt.Api` to a modern **Minimal API** structure.

### Key Porting Strategies:
- **Legacy Model Retention**: Models from the legacy `Model` and `Data` directories were re-implemented in `backend/Models/` to ensure the JSON payloads remain identical to the legacy system. This prevents breaking changes for the ported UI components.
- **Minimal API Mapping**: Legacy `ApiController` routes (e.g., `svc/company/profile/view`) were mapped to `app.MapGet` and `app.MapPost` in `backend/Endpoints/CompanyEndpoints.cs`.
- **Entity Framework Core**: The `AppDbContext` was expanded to support complex relationships like `CompanyProfile` and its associated `Offices`.
- **Service Layer**: `CompanyService` was updated to handle complex DTO mapping (e.g., `Profile` and `Office` structures) while utilizing modern C# features like primary constructors and collection expressions.

### Data Structure Mapping:
| Legacy Concept | New Implementation | Location |
| :--- | :--- | :--- |
| `EntityId<T>`, `IdName<T>` | `EntityId<T>`, `IdName<T>` | `LegacyModels.cs` |
| `Company.Profile` | `Profile` DTO | `CompanyModels.cs` |
| `Office` | `Office` DTO | `CompanyModels.cs` |
| `svc/company/profile` | `/api/company/profile` | `CompanyEndpoints.cs` |

## 3. Frontend Migration (TypeScript/Lit/Shoelace)

The frontend uses **Next.js** as the host framework but utilizes **Lit** for the core UI logic to facilitate a smooth port from the legacy Polymer components.

### UI Stack:
- **Next.js 16**: App Router, Dynamic Imports, and SSR.
- **Lit 3.3**: Used for porting legacy Polymer logic (`.ts` components).
- **Shoelace 2.20**: Used for standardized, accessible UI components (`sl-card`, `sl-select`, etc.).

### Porting Process:
1. **Remove Tailwind**: To align with the custom Lit/Shoelace styling strategy, Tailwind CSS was removed.
2. **Lit Components**: Legacy components (e.g., `company/profile.ts`) were rewritten as modern Lit classes.
3. **Shoelace Integration**: Legacy `@material/web` and `paper-` components were replaced with Shoelace equivalents.
4. **React Wrapper**: Since Lit components are Custom Elements, they are integrated into the Next.js App Router via a React wrapper (`CompanyProfileWrapper.tsx`) that uses `next/dynamic` with `ssr: false` to ensure they only render on the client.

### Component Structure:
- `frontend/src/components/company-profile.ts`: The core Lit component logic and template.
- `frontend/src/components/CompanyProfileWrapper.tsx`: The Client Component that registers and exports the Lit custom element for use in React.
- `frontend/src/app/page.tsx`: The Next.js entry point rendering the profile for a specific `companyId`.

## 4. Orchestration (.NET Aspire)

**.NET Aspire** is used to unify the development experience.

### Service Discovery:
The AppHost automatically allocates ports for the backend. In `apphost/Program.cs`, this URL is mapped to the `NEXT_PUBLIC_API_URL` environment variable:
```csharp
builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithReference(backend)
    .WithEnvironment("NEXT_PUBLIC_API_URL", backend.GetEndpoint("http"));
```

### Running the Project:
1. Run `dotnet run --project apphost/BizSrt.AppHost.csproj`.
2. Access the Aspire Dashboard to view logs and endpoint URLs.

## 5. Known Limitations & TODOs:
- **Mock Data**: Currently, backend endpoints return placeholder/empty data. Seeding logic in `Program.cs` or a real database connection is required for full validation.
- **SSL Trust**: In some CLI environments, `https` profiles may fail due to untrusted dev certificates. Use the `http` profile if necessary.
- **Remaining Components**: Additional legacy components (Jobs, Products, Projects) are prepared in the backend models but still need UI porting in the frontend.
