# TasteBudz SQLite Scripts

These scripts remain the local development and integration-test schema source.

Apply order:

1. `dbTasteBudz.sqlite.sql`
2. `dbTasteBudz.sqlite.seed.sql`
3. `dbTasteBudz.sqlite.testdata.sql` only for development/test scenario data

The application may initialize SQLite automatically only in `Development` or `IntegrationTesting` when `Persistence:InitializeSqliteOnStartup=true`.
