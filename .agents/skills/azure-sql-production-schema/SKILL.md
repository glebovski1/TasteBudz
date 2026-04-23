---
name: azure-sql-production-schema
description: Apply and verify TasteBudz production Azure SQL schema updates from the source-controlled SQL Server scripts. Use when asked to patch, migrate, bootstrap, or validate the production database before or after Azure App Service publish, especially when the current branch adds tables, columns, or schema-version changes that the app publish script will not apply.
---

# Azure SQL Production Schema

## Purpose

Apply the manual TasteBudz Azure SQL release step without changing the repository rule that production schema changes are explicit and separate from app publish.

Use this skill when the task is about production database schema deployment, not code-only App Service publish.

## Guardrails

- Read `AGENTS.md`, `docs/deployment/azure-app-service.md`, and `src/TasteBudz.Database/sqlserver/README.md` before applying schema changes.
- Keep production SQL deployment manual and explicit. Do not add startup migrations or fold schema updates into the app publish script.
- Prefer incremental patch scripts for existing production databases. Use the full ordered bootstrap (`000`, `010`, `020`) only for new databases or when re-applying those files is confirmed safe.
- Do not print SQL passwords, access tokens, or full connection strings.
- Verify the database after applying scripts. The skill bundles a readiness probe that checks the live database against the current backend-required tables and columns.
- Use dry runs before touching Azure when the target script set or firewall access is uncertain.

## Workflow

1. Determine whether the target database is new or existing.
2. Choose the script set:
   - Existing production database: pass one or more explicit patch scripts with `-ScriptPath`.
   - New database bootstrap: omit `-ScriptPath` and let the script apply `000_schema_versions.sql`, `010_schema.sql`, and `020_seed_reference_data.sql` in order.
3. Run a dry run first when confirming defaults, firewall handling, or script selection.
4. Run `scripts/apply-azure-sql-schema.ps1`.
5. Review the reported script list, schema versions, and readiness summary.
6. Only after readiness passes, run the Azure App Service publish skill for the app package.

## Commands

Dry run against the current production defaults:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1 -DryRun
```

Apply explicit patch scripts to the current production database:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1 `
  -ScriptPath .\src\TasteBudz.Database\sqlserver\patches\20260422_add_password_reset_requests.sql
```

Open a temporary local-client firewall rule while applying scripts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1 `
  -ScriptPath .\src\TasteBudz.Database\sqlserver\patches\20260422_add_password_reset_requests.sql `
  -AllowClientIp
```

Target a different subscription or App Service:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1 `
  -Subscription "<subscription>" `
  -ResourceGroup "<resource-group>" `
  -WebAppName "<web-app>"
```

## References

- Read `references/tastebudz-azure-sql-schema.md` for parameter details, firewall behavior, bootstrap vs patch guidance, and troubleshooting.
- The readiness probe lives at `scripts/schema-readiness-probe` and is run automatically unless `-SkipVerification` is passed.
