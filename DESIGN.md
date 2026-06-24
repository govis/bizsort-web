# Web Application Design: BizSort Web

## Architecture
- **Backend**: .NET 10 Minimal API (`/backend`)
  - Ported from `bizsort-api/migrated-net-latest`.
  - Uses EF Core with SQL Server and NetTopologySuite for geospatial data.
  - Minimal API endpoints for Company and Image services.
  - Image processing via ImageSharp.
- **Frontend**: Next.js 16 (TypeScript) (`/frontend`)
  - React-based shell with Lit 3.3 web components for UI logic.
  - Web Awesome (`@awesome.me/webawesome`) component library (`wa-` prefix).
  - Vanilla CSS for styling (no Tailwind).
  - Backend URL injected via `NEXT_PUBLIC_API_URL` environment variable.
- **AppHost**: .NET Aspire Orchestrator (`/apphost`)
  - Manages the lifecycle of both Backend and Frontend.
  - Handles service discovery, port allocation, and environment variable injection.
  - Provides the Aspire Dashboard for observability (logs, traces, endpoints).

## Orchestration Strategy
The `apphost` is a .NET Aspire project (`Aspire.AppHost.Sdk`) that:
1. Adds the backend as a .NET project reference (`BizSrt.Api`).
2. Adds the frontend as an NPM app (`AddNpmApp`) running `npm run dev`.
3. Injects `NEXT_PUBLIC_API_URL` pointing to the backend's HTTP endpoint.

## Running the Project
```
dotnet run --project apphost/BizSrt.AppHost.csproj
```
- API URL: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`
- Aspire Dashboard: see launch profile output for URL.
