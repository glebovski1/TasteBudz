# TasteBudz Submission Summary

TasteBudz is submitted as a source-first ASP.NET Core modular monolith. The repository intentionally keeps application source, automated tests, canonical database scripts, UI assets used by the app, local/deployment scripts, and curated documentation.

## What Is Kept

- `README.md` and `AGENTS.md`
- functional requirements in `docs/TasteBudz_Functional_Requirements.md`
- backend authority docs in `docs/backend/`
- database and deployment docs in `docs/database/` and `docs/deployment/`
- application and test projects under `src/` and `tests/`
- canonical SQLite scripts under `src/TasteBudz.Database/sqlite/`
- canonical SQL Server / Azure SQL scripts under `src/TasteBudz.Database/sqlserver/`
- human-usable deployment helpers under `scripts/deployment/`
- intentional MVC static assets under `src/TasteBudz.Web.Mvc/wwwroot/`

## Source-Only Policy

Generated build outputs, Playwright captures, local runtime files, coverage/TRX output, screenshots, `.sqlite` database files, and agent-only wrappers are excluded from the submission. Database schema authority is source SQL, not a checked-in database file.

SQLite remains the local development and automated test provider. Azure SQL / SQL Server remains the production deployment target.

## How To Run

Start the local single-host MVC/API/SignalR app with a regenerated local SQLite database:

```powershell
.\start-dev.ps1 -ResetDatabase
```

Run the required validation commands:

```powershell
dotnet restore TasteBudz.sln
dotnet build TasteBudz.sln -c Release --no-restore
dotnet test TasteBudz.sln -c Release --no-build
```

Deployment helpers:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\deployment\update-published-app.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\deployment\release-with-rollback.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\deployment\apply-azure-sql-schema.ps1
```

## Verification

Cleanup verification performed:

- `dotnet restore TasteBudz.sln`: passed
- `dotnet build TasteBudz.sln -c Release --no-restore`: passed with 0 warnings
- `dotnet test TasteBudz.sln -c Release --no-build`: passed 310 tests
- `dotnet build scripts\deployment\schema-readiness-probe\SchemaReadinessProbe.csproj -c Release`: passed with 0 warnings
- deployment PowerShell entrypoints parsed successfully
- `update-published-app.ps1 -DryRun` and `release-with-rollback.ps1 -DryRun -AllowForwardOnlyDatabaseChange` resolved the repository root and stopped before side effects
- artifact scans found no tracked generated files after cleanup patterns were applied
- root `tastebudz-*.png` screenshot scan returned 0 files

Known gaps:

- Azure deployment was not executed as part of submission cleanup.
- `apply-azure-sql-schema.ps1` was syntax-checked but not run because it requires an Azure App Service connection string and SQL target.
