---
name: azure-app-service-deployment
description: Deploy the TasteBudz single-host ASP.NET Core app to Azure App Service with Azure SQL. Use when the user asks to deploy TasteBudz to Azure, publish the MVC/API/SignalR host, configure Azure SQL, apply production SQL scripts, create or configure App Service resources, or troubleshoot TasteBudz Azure App Service deployment.
---

# Azure App Service Deployment

## Purpose

Deploy TasteBudz as one ASP.NET Core App Service host from `src/TasteBudz.Web.Mvc`, with MVC, backend API controllers, SignalR at `/hubs/chat`, backend services, and EF Core persistence in the same publish artifact.

## Guardrails

- Read `AGENTS.md`, `docs/deployment/azure-app-service.md`, and `src/TasteBudz.Database/sqlserver/README.md` before changing deployment behavior.
- Do not deploy `TasteBudz.Backend` as a separate web app.
- Do not auto-create or auto-migrate production schema at app startup.
- Apply Azure SQL scripts manually, in order, before starting the app against a new database.
- Keep `BackendApi__BaseUrl` blank for same-host App Service deployment unless the user explicitly requests a separate API topology.
- Keep SQL passwords and connection strings out of commits and final answers.
- Confirm subscription, resource names, region, and expected Azure costs before creating paid resources.
- Do not delete Azure resources unless the user explicitly asks.

## Workflow

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
