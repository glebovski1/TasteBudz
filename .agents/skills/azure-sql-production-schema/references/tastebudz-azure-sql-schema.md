# TasteBudz Azure SQL Schema Deployment

Use these commands from the repository root.

## Scope

This skill covers the manual production database release step only.

- It applies SQL scripts to Azure SQL with `sqlcmd`.
- It can resolve the current production connection string from the deployed App Service.
- It can temporarily add a local-client Azure SQL firewall rule when the current workstation is not allowed.
- It verifies the live database against the current backend-required tables and columns after the scripts run.

It does not publish the web app package.

## Default Target

Unless overridden, the apply script uses:

- resource group `rg-tastebudz-prod`
- web app `tastebudz-prod-23df46c9`
- connection string `TasteBudz` from that App Service

## Script Modes

### Existing Production Database

Pass one or more explicit patch scripts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1 `
  -ScriptPath .\src\TasteBudz.Database\sqlserver\patches\20260422_some_patch.sql
```

Use this mode for production updates to an already populated database.

### New Database Bootstrap

If `-ScriptPath` is omitted, the script applies:

1. `src/TasteBudz.Database/sqlserver/000_schema_versions.sql`
2. `src/TasteBudz.Database/sqlserver/010_schema.sql`
3. `src/TasteBudz.Database/sqlserver/020_seed_reference_data.sql`

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1
```

Use this mode only for a new database or when re-applying those files is intentionally approved.

## Firewall Handling

If local `sqlcmd` access is blocked, let the script add a temporary firewall rule:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1 `
  -ScriptPath .\src\TasteBudz.Database\sqlserver\patches\20260422_some_patch.sql `
  -AllowClientIp
```

Behavior:

- The script resolves the current public client IP.
- It creates a firewall rule on the Azure SQL server.
- It deletes that temporary rule at the end unless `-KeepClientIpRule` is passed.

Override the SQL server resource group with `-SqlServerResourceGroup` if it differs from the App Service resource group.

## Verification

The script runs a bundled readiness probe after applying scripts unless `-SkipVerification` is passed.

The probe:

- reads the current backend-required table/column set from `TasteBudz.Backend`
- connects to Azure SQL with the same connection string source
- reports whether the database is ready for the current code
- prints a compact JSON summary with `ready`, `missingCount`, `schemaVersionCount`, and sample missing objects

Treat `ready=false` as a release blocker.

## Parameters

Useful parameters:

- `-Subscription <name-or-id>`
- `-ResourceGroup <resource-group>`
- `-WebAppName <web-app>`
- `-SqlServerResourceGroup <resource-group>`
- `-ScriptPath <one-or-more-sql-files>`
- `-AllowClientIp`
- `-KeepClientIpRule`
- `-SkipVerification`
- `-DryRun`

## Troubleshooting

If `sqlcmd` fails with login or firewall errors:

- confirm Azure CLI login with `az account show`
- rerun with `-AllowClientIp`
- confirm the App Service connection string named `TasteBudz` still points at the intended database

If verification reports missing tables or columns:

- do not publish the app yet
- identify whether the missing object should come from an existing bootstrap file or a new incremental patch script
- apply the missing patch and rerun verification

If the script list is wrong:

- stop and rerun with explicit `-ScriptPath` values
- do not rely on the bootstrap set for a live production patch unless that was the intended release plan
