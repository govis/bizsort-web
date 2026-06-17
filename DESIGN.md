# Web Application Design: BizSort Web

## Architecture
- **Backend**: .NET 10 Minimal API (`/backend`)
  - Ported from `bizsort-api/migrated-net-latest`.
  - Uses EF Core with SQL Server and NetTopologySuite.
  - Minimal API endpoints for Company and Image services.
- **Frontend**: Next.js 14+ (TypeScript) (`/frontend`)
  - React-based UI.
  - Vanilla CSS for styling.
  - Proxying/Connecting to Backend via the orchestrator.
- **AppHost**: TypeScript Node.js Orchestrator (`/apphost`)
  - Manages the lifecycle of both Backend and Frontend.
  - Handles environment variables and port management.
  - Inspired by .NET Aspire but implemented in TypeScript.

## Orchestration Strategy
The `apphost` will be a Node.js application that uses `concurrently` to run:
1. `dotnet run --project ../backend`
2. `npm run dev --prefix ../frontend`

It will also manage dependencies (e.g., waiting for the backend to be ready).

## Implementation Plan
1. **Port Backend**:
   - Recreate the project structure in `backend/`.
   - Update `appsettings.json` for the new environment.
2. **Scaffold Frontend**:
   - Initialize a Next.js TypeScript project.
   - Add a basic service to call the backend.
3. **Build AppHost**:
   - Setup `package.json` and a TS orchestrator script.
   - Configure it to run both services.
