---
name: port_legacy_backend
description: Mandatory workflow for porting any legacy C# endpoints, services, or data queries to the modern .NET 8 backend.
---

# 🛑 CRITICAL MANDATORY WORKFLOW: Porting Legacy Backend 🛑

You have been tasked with porting legacy C# code. **DO NOT WING IT.** You are strictly forbidden from writing new EF Core LINQ queries, caching logic, or inventing new API payloads from scratch.

You MUST follow this exact, step-by-step workflow. If you skip a step, you will break the modernized frontend that relies on legacy schemas.

## Step 1: Prove It Needs Porting
1. Check the exhaustively generated tracker: `C:\Bizsort\bizsort-web\.agents\LEGACY_TRACKER.md`.
2. Locate the target namespace and class. If it is already marked `[x]`, **STOP IMMEDIATELY**. Do not port it again. Use the existing modern equivalent.

## Step 2: Investigate the Legacy Cache 
Before writing *any* data access logic in `backend/Service/` or `backend/Data/`:
1. Use `grep_search` to search `C:\Bizsort\legacy\server\Data\Cache` to see if a cache exists for the entity you are dealing with (e.g., `FeaturedCompaniesCache`, `CompanyProfilesCache`).
2. If a legacy cache exists, **YOU MUST NOT HIT EF CORE DIRECTLY**.
3. You must scaffold the modern Cache class (inheriting from `ReadManyExpirationCache`, etc.), define the `Cached*` object, map the EF entity into it, and inject it as a Singleton in `Program.cs`.

## Step 3: Extract the Legacy LINQ Query
If you must hit the database (because no cache exists, or you are populating the cache):
1. Navigate to the legacy implementation (e.g. `C:\Bizsort\legacy\server\Data\`).
2. Extract the EXACT `dbContext` or LINQ query that was used.
3. Do not invent new `Where` or `Join` clauses. The constraints already exist in the legacy logic.

## Step 4: Strict API Semantics
When porting endpoints (`backend/Endpoint/`):
1. Check the legacy MVC controller (e.g., `C:\Bizsort\legacy\server\Service\Company\Profile.cs`).
2. You must perfectly replicate the exact HTTP Verb, Route (`[Route("svc/...")]` becomes `/api/...`), query parameters, and JSON payload structures. The modernized frontend expects the exact same JSON format!

## Step 5: Update the Tracker
When finished, update `generate_tracker_matrix.py` with your new class in the `known_ports` dictionary and run the script so `LEGACY_TRACKER.md` stays up-to-date.
