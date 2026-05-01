# TasteBudz Azure SQL Scripts

These scripts are for manual Azure SQL / SQL Server deployment. The application does not create or migrate production SQL Server databases on startup.

Apply scripts in this order for a new Azure SQL database:

1. `000_schema_versions.sql`
2. `010_schema.sql`
3. `020_seed_reference_data.sql`

For an existing production database, apply only the explicit patch scripts needed from `src/TasteBudz.Database/sqlserver/patches`.
Current incremental repair scripts include:

- `patches/20260422_password_reset_requests_and_restaurant_catalog.sql`
- `patches/20260425_group_announcements_wallpaper.sql`
- `patches/20260426_restaurant_slot_discount_percent.sql`
- `patches/20260501_add_devon_moderator_account.sql`

Optional demo-data helpers live under `src/TasteBudz.Database/sqlserver/demo`:

- `20260426_feature_seed_inventory.sql` reports row counts for each implemented feature surface.
- `20260426_feature_seed_topup.sql` adds a small, deterministic feature-coverage data set only when you explicitly choose to top up a demo database.
- `20260426_feature_seed_topup_rollback.sql` removes that deterministic demo top-up data.
- `20260430_discount_slot_seed_topup.sql` adds extra deterministic restaurant discount slots plus two slot-linked demo events.
- `20260430_discount_slot_seed_topup_rollback.sql` removes only that extra discount-slot demo data.

Do not include demo top-up scripts in normal production bootstrap or routine startup paths.

Operational checklist:

1. Create the Azure SQL database.
2. Connect with an account allowed to create tables, constraints, and indexes.
3. Apply the bootstrap scripts in order for a new database, or the required patch script set for an existing database.
4. Confirm `dbo.SchemaVersions` contains the applied versions.
5. Configure the App Service with `Persistence:Provider=SqlServer` and the `ConnectionStrings:TasteBudz` Azure SQL connection string.
6. Start or restart the app. Startup validates required tables and columns but does not apply schema changes.

The SQL Server schema is derived from the current implemented SQLite schema, including restaurant operations, direct chat, checkout, media, moderation, and messaging tables.
