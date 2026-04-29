# Azure App Service Deployment Notes

TasteBudz now publishes as one ASP.NET Core web host from `src/TasteBudz.Web.Mvc`.

The publish artifact contains:

- MVC frontend
- backend API controllers
- SignalR chat hub at `/hubs/chat`
- backend services and EF Core persistence wiring

## App Service Configuration

Set these application settings for Azure SQL:

```text
Persistence__Provider=SqlServer
BackendApi__BaseUrl=
```

Set the Azure SQL connection string named `TasteBudz`:

```text
ConnectionStrings__TasteBudz=<Azure SQL connection string>
```

`BackendApi__BaseUrl` can stay blank when the MVC UI calls the API in the same deployed host. Set it only if a future topology intentionally points the UI at a different API base URL.

## Visual Studio Launch Profiles

The MVC project includes two checked-in launch profiles:

- `TasteBudz SQLite Dev (Single Host)` runs MVC, API controllers, and SignalR together on the local SQLite database.
- `TasteBudz Azure Production` runs the same single host with `ASPNETCORE_ENVIRONMENT=Production` and `Persistence__Provider=SqlServer`. It intentionally does not check in a SQL connection string; provide `ConnectionStrings__TasteBudz` through App Service configuration, environment variables, or user secrets before running it locally.

## Database Deployment

Production database deployment remains manual. Apply these scripts in order before starting the app against a new Azure SQL database:

1. `src/TasteBudz.Database/sqlserver/000_schema_versions.sql`
2. `src/TasteBudz.Database/sqlserver/010_schema.sql`
3. `src/TasteBudz.Database/sqlserver/020_seed_reference_data.sql`

For an existing Azure SQL database, apply only the needed incremental patch scripts from `src/TasteBudz.Database/sqlserver/patches` before starting the updated app.

Startup validates required SQL Server tables and columns. It does not create or migrate production schema.

## Deployment Scripts

Human-usable deployment helpers live under `scripts/deployment`.

For code-only updates to the existing published app, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\deployment\update-published-app.ps1
```

For a release that also applies explicit SQL scripts and can run rollback steps, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\deployment\release-with-rollback.ps1 -ScriptPath src\TasteBudz.Database\sqlserver\patches\<patch-file>.sql
```

For manual Azure SQL schema application without deploying app code, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\deployment\apply-azure-sql-schema.ps1
```

The app update script validates Release restore/build/test, publishes only the MVC host, deploys a zip package, and verifies the homepage plus unauthenticated API and SignalR `401` responses. SQL schema deployment remains manual: when schema changes, apply the required bootstrap or patch scripts from `src/TasteBudz.Database/sqlserver` as a separate release step and keep production startup migrations disabled.
