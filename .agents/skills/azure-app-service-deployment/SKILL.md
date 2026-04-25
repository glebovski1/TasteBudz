---
name: azure-app-service-deployment
description: Deploy or update the TasteBudz single-host ASP.NET Core app on Azure App Service with Azure SQL. Use when the user asks to deploy TasteBudz to Azure, publish the MVC/API/SignalR host, update production database scripts, run data migrations, verify production, rollback a failed release, configure Azure SQL, or troubleshoot deployment.
---

# Azure App Service Deployment

## Purpose

Deploy or update TasteBudz as one ASP.NET Core App Service host from `src/TasteBudz.Web.Mvc`, with MVC, backend API controllers, SignalR at `/hubs/chat`, backend services, and EF Core persistence in the same publish artifact.

## Guardrails

- Read `AGENTS.md`, `docs/deployment/azure-app-service.md`, and `src/TasteBudz.Database/sqlserver/README.md` before changing deployment behavior.
- Do not deploy `TasteBudz.Backend` as a separate web app.
- Do not auto-create or auto-migrate production schema at app startup.
- Apply Azure SQL scripts manually, in order, before starting the app against a new database.
- Use `scripts/update-published-app.ps1` for normal code-only updates to the existing App Service.
- Use `scripts/release-with-rollback.ps1` for combined database/data migration plus app releases.
- Do not use the update script to apply database schema changes. SQL scripts remain a manual release step.
- Database rollback is only automatic when explicit rollback SQL scripts are supplied. Without rollback SQL, the reliable release script requires `-AllowForwardOnlyDatabaseChange` and will only roll back the app package.
- Keep `BackendApi__BaseUrl` blank for same-host App Service deployment unless the user explicitly requests a separate API topology.
- Keep SQL passwords and connection strings out of commits and final answers.
- Confirm subscription, resource names, region, and expected Azure costs before creating paid resources.
- Do not delete Azure resources unless the user explicitly asks.

## Workflow

### Update Existing App

Use this path for code-only updates to the already published App Service.

1. Confirm whether the change includes database schema changes.
   - If yes, keep SQL deployment manual with `src/TasteBudz.Database/sqlserver` scripts.
   - If no, continue with the update script.
2. Run a dry run when you need to confirm defaults without changing Azure:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1 -DryRun`
3. Run the update script for the current production defaults:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1`
4. Use optional parameters only when needed:
   - `-Subscription <name-or-id>`
   - `-PublishRoot <path>`
   - `-KeepArtifacts`
   - `-DryRun`
5. Report restore, Release build, Release test, deployment, and smoke verification status. Do not print SQL passwords, publishing passwords, access tokens, or full connection strings.

### Reliable Database And App Release

Use this path when a release includes SQL schema patches, data migrations, or any uncertainty that requires rollback.

1. Prepare forward SQL scripts under `src/TasteBudz.Database/sqlserver/patches`.
2. Prepare rollback SQL scripts for the same data/schema change whenever the change is not purely additive and harmless.
3. Run a dry run:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\release-with-rollback.ps1 -ScriptPath <patch.sql> -DatabaseRollbackScriptPath <rollback.sql> -DryRun`
4. Run the reliable release script:
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\release-with-rollback.ps1 -ScriptPath <patch.sql> -DatabaseRollbackScriptPath <rollback.sql> -AllowClientIp`
5. If a database change is intentionally forward-only, pass `-AllowForwardOnlyDatabaseChange` and state that database rollback is not available.

The reliable release script:

- runs Release restore/build/tests and `git diff --check`
- snapshots the currently deployed App Service package from Kudu before changes
- validates the rollback snapshot, publish output, and zip package before changing production
- applies the supplied SQL migration scripts and verifies schema readiness through the SQL skill script
- publishes and deploys only `src/TasteBudz.Web.Mvc`
- uses synchronous Kudu zipdeploy for fallback deployment and app rollback
- verifies homepage, unauthenticated API, and SignalR responses
- optionally runs a post-deploy verification script
- on failure, applies rollback SQL scripts when supplied and redeploys the previous App Service package snapshot
- keeps generated artifacts after a failed release for diagnosis or manual recovery

Treat rollback verification failure as a production incident and report it immediately.

### Initial Provision

1. Confirm Azure CLI access:
   - `az login`
   - `az account show --output table`
   - `az account set --subscription "<subscription>"`
2. Choose or confirm resource names: resource group, App Service plan, web app, SQL server, SQL database, region, SQL admin user.
3. Create or reuse Azure resources.
4. Configure App Service:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `Persistence__Provider=SqlServer`
   - `BackendApi__BaseUrl=`
   - connection string named `TasteBudz`
   - WebSockets enabled for SignalR
5. Apply SQL Server scripts manually:
   - `src/TasteBudz.Database/sqlserver/000_schema_versions.sql`
   - `src/TasteBudz.Database/sqlserver/010_schema.sql`
   - `src/TasteBudz.Database/sqlserver/020_seed_reference_data.sql`
6. Build and verify:
   - `dotnet restore TasteBudz.sln`
   - `dotnet build TasteBudz.sln -c Release`
   - `dotnet test TasteBudz.sln -c Release --no-build`
7. Publish only the MVC host:
   - `dotnet publish src/TasteBudz.Web.Mvc/TasteBudz.Web.Mvc.csproj -c Release -o <publish-dir>`
8. Zip the publish directory contents and deploy the zip to App Service.
9. Verify:
   - homepage loads
   - unauthenticated `/api/v1/restaurants` returns `401`, not `404`
   - unauthenticated `/hubs/chat/negotiate?negotiateVersion=1` returns `401`, not `404`
   - App Service logs do not show schema validation errors.

## Detailed Commands

For the complete PowerShell/Azure CLI command sequence, read `references/tastebudz-azure-app-service.md`.
