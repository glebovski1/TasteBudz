# SQLite Database Guide

This document explains how SQLite is used in the source-only repository.
SQLite is the local development and automated test provider. Azure SQL / SQL Server is the production target for Azure deployment.

For authoritative backend policy and architecture, see:

- `docs/backend/backend-decisions.md`
- `docs/backend/backend-architecture.md`

Important rules:

- the SQL files under `src/TasteBudz.Database/sqlite/` are the SQLite source of truth for local/test schema and seed data
- the SQL files under `src/TasteBudz.Database/sqlserver/` are the manual Azure SQL production deployment scripts
- generated `.sqlite`, `*.sqlite-shm`, and `*.sqlite-wal` files are local artifacts and must not be committed

## Runtime Behavior

`src/TasteBudz.Web.Mvc/appsettings.json` defaults to:

```json
"ConnectionStrings": {
  "TasteBudz": "Data Source=TasteBudz.sqlite;Foreign Keys=True;Pooling=False"
},
"Persistence": {
  "Provider": "Sqlite",
  "InitializeSqliteOnStartup": false,
  "SeedTestDataOnStartup": false
}
```

That default expects an already prepared SQLite database next to the running app and validates required tables on startup.

`src/TasteBudz.Web.Mvc/appsettings.Development.json` points local development at `.codex-temp\\TasteBudz.local.sqlite` and enables:

- `Persistence:InitializeSqliteOnStartup=true`
- `Persistence:SeedTestDataOnStartup=true`

The normal local command is:

```powershell
.\start-dev.ps1 -ResetDatabase
```

The script sets the SQLite connection string, recreates `.codex-temp\TasteBudz.local.sqlite` when requested, applies the source-controlled schema and seed scripts, and starts the single MVC/API/SignalR host.

## Startup Flow

On application startup:

1. `src/TasteBudz.Web.Mvc/Program.cs` reads `Persistence:Provider` and `ConnectionStrings:TasteBudz`.
2. `SqliteConnectionStringHelper.Normalize(...)` resolves relative SQLite paths against the web host content root.
3. `SqliteDatabaseBootstrapper.EnsureInitializedAsync(...)` decides whether schema initialization is allowed.
4. In `Development` or `IntegrationTesting`, when initialization is enabled, startup applies:
   - `dbTasteBudz.sqlite.sql`
   - `dbTasteBudz.sqlite.seed.sql`
   - `dbTasteBudz.sqlite.testdata.sql` only when the database does not already contain users
5. Startup validates that all required tables exist.

Practical result:

- local development and integration tests bootstrap from source-controlled SQL
- non-development SQLite environments must point to an already prepared database
- Azure production uses `Persistence:Provider=SqlServer` and the SQL Server scripts manually

## Integration Tests

Integration tests create temporary SQLite files per test factory and initialize those files from the canonical SQL assets. They do not depend on any shared repository database file.

## Files Involved

- `src/TasteBudz.Web.Mvc/appsettings.json`
- `src/TasteBudz.Web.Mvc/appsettings.Development.json`
- `src/TasteBudz.Web.Mvc/Program.cs`
- `src/TasteBudz.Backend/Infrastructure/Persistence/Sqlite/SqliteConnectionStringHelper.cs`
- `src/TasteBudz.Backend/Infrastructure/Persistence/Sqlite/SqliteDatabaseBootstrapper.cs`
- `src/TasteBudz.Database/sqlite/dbTasteBudz.sqlite.sql`
- `src/TasteBudz.Database/sqlite/dbTasteBudz.sqlite.seed.sql`
- `src/TasteBudz.Database/sqlite/dbTasteBudz.sqlite.testdata.sql`
- `src/TasteBudz.Database/sqlserver/000_schema_versions.sql`
- `src/TasteBudz.Database/sqlserver/010_schema.sql`
- `src/TasteBudz.Database/sqlserver/020_seed_reference_data.sql`
- `src/TasteBudz.Database/init_sqlite.py`

## Seed Data

Reference seed data includes cuisines, ZIP coordinates, and restaurant catalog rows used by local/test workflows. Development/test scenario data adds demo users, preferences, Budz relationships, groups, events, restaurant slots, chat threads, notifications, moderation records, and audit examples.

All seeded scenario accounts use password `TasteBudz123!`. Common users include `alex`, `brooke`, `casey`, `devon`, `emery`, `gina`, and `jordan`.

## Manual Local Database Initialization

The preferred local path is `.\start-dev.ps1 -ResetDatabase`. If a standalone local database file is needed for inspection, this helper creates an ignored SQLite file from the source SQL scripts:

```powershell
python src\TasteBudz.Database\init_sqlite.py --with-test-data
```

The generated database is a local artifact. Do not commit it.
