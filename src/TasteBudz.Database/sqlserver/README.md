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

Operational checklist:

1. Create the Azure SQL database.
2. Connect with an account allowed to create tables, constraints, and indexes.
3. Apply the bootstrap scripts in order for a new database, or the required patch script set for an existing database.
4. Confirm `dbo.SchemaVersions` contains the applied versions.
5. Configure the App Service with `Persistence:Provider=SqlServer` and the `ConnectionStrings:TasteBudz` Azure SQL connection string.
6. Start or restart the app. Startup validates required tables and columns but does not apply schema changes.

The SQL Server schema is derived from the current implemented SQLite schema, including restaurant operations, direct chat, checkout, media, moderation, and messaging tables.
