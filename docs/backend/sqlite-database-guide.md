# SQLite Database Guide

This is a simple, current-state explanation of how SQLite works in this repo today.

For authoritative backend policy and architecture, see:

- `docs/backend/backend-decisions.md`
- `docs/backend/backend-architecture.md`

Important rule:

- the SQL files under `src/TasteBudz.Database/` are still the source of truth for schema and seed data
- the tracked `src/TasteBudz.Database/TasteBudz.sqlite` file is a convenient shared snapshot, not the schema authority

## How the Database Connection Works

### 1. Default runtime behavior

`src/TasteBudz.Backend/appsettings.json` uses:

```json
"ConnectionStrings": {
  "TasteBudz": "Data Source=TasteBudz.sqlite;Foreign Keys=True;Pooling=False"
}
```

That is the safe default for non-development environments:

- the database file lives next to the running app
- startup does **not** auto-create the database by default
- startup only validates that the required tables already exist

This keeps publish/deploy output portable.

### 2. Development behavior

`src/TasteBudz.Backend/appsettings.Development.json` overrides the connection string to:

```json
"ConnectionStrings": {
  "TasteBudz": "Data Source=..\\TasteBudz.Database\\TasteBudz.sqlite;Foreign Keys=True;Pooling=False"
}
```

In Development:

- the backend points to `src/TasteBudz.Database/TasteBudz.sqlite`
- `InitializeSqliteOnStartup` is `true`
- `SeedTestDataOnStartup` is `true`

That means local development uses the repo-level SQLite file instead of creating a throwaway DB inside the backend output folder.

### 3. What startup does

On application startup:

1. `Program.cs` reads `ConnectionStrings:TasteBudz`
2. `SqliteConnectionStringHelper.Normalize(...)` resolves relative paths against the backend content root
3. `SqliteDatabaseBootstrapper.EnsureInitializedAsync(...)` decides whether schema initialization is allowed
4. if the environment is `Development` or `IntegrationTesting` and initialization is enabled:
   - it applies `dbTasteBudz.sqlite.sql`
   - it applies `dbTasteBudz.sqlite.seed.sql`
   - it applies `dbTasteBudz.sqlite.testdata.sql` only when the database does not already contain users
5. it validates that all required tables exist

Practical result:

- Development and integration tests can bootstrap from source-controlled SQL
- non-development environments must point to an already prepared database
- the dev test-data seed is not re-applied over an existing user store

### 4. Integration test behavior

Integration tests do **not** use the shared tracked SQLite file.

They create temporary SQLite files per test factory and initialize those from the same canonical SQL assets. That keeps test runs isolated while still validating the real SQLite path.

### 5. Git behavior right now

The repo now tracks one canonical SQLite snapshot:

- `src/TasteBudz.Database/TasteBudz.sqlite`

Git rules:

- the canonical snapshot file is allowed to be committed
- SQLite sidecar files such as `*.sqlite-wal` and `*.sqlite-shm` are still ignored
- `.gitattributes` marks the tracked snapshot as binary

This means:

- everyone who pulls the branch gets the same committed snapshot
- changes to the snapshot still behave like binary changes in git
- if two branches both modify the SQLite file, a manual choice is still required during merge

### 6. Files involved

- `src/TasteBudz.Backend/appsettings.json`
- `src/TasteBudz.Backend/appsettings.Development.json`
- `src/TasteBudz.Backend/Program.cs`
- `src/TasteBudz.Backend/Infrastructure/Persistence/Sqlite/SqliteConnectionStringHelper.cs`
- `src/TasteBudz.Backend/Infrastructure/Persistence/Sqlite/SqliteDatabaseBootstrapper.cs`
- `src/TasteBudz.Database/dbTasteBudz.sqlite.sql`
- `src/TasteBudz.Database/dbTasteBudz.sqlite.seed.sql`
- `src/TasteBudz.Database/dbTasteBudz.sqlite.testdata.sql`
- `src/TasteBudz.Database/init_sqlite.py`
- `src/TasteBudz.Database/TasteBudz.sqlite`

## Current Seeded Data

The current tracked snapshot was rebuilt from:

- schema: `dbTasteBudz.sqlite.sql`
- reference seed: `dbTasteBudz.sqlite.seed.sql`
- development/test scenario seed: `dbTasteBudz.sqlite.testdata.sql`

At the moment, the shared snapshot contains:

- 35 tables
- 13 cuisines
- 7 ZIP coordinate rows
- 8 restaurants
- 6 user accounts
- 2 groups
- 3 events
- 3 chat threads
- 4 notifications
- 1 moderation report
- 1 moderation action
- 1 active restriction
- 1 audit log entry

## Reference Seed Data

### Cuisines

- American
- Indian
- Italian
- Japanese
- Mediterranean
- Mexican
- Noodles
- Pizza
- Sushi
- Tacos
- Thai
- Vegetarian
- Vietnamese

### ZIP coordinates

- 41011
- 45202
- 45206
- 45208
- 45212
- 45219
- 45220

### Restaurants

- Campus Noodles: ZIP `45219`, cuisines `Noodles`, `Thai`
- Garden Falafel: ZIP `45206`, cuisines `Mediterranean`, `Vegetarian`
- Late Night Pizza Co: ZIP `45212`, cuisines `Italian`, `Pizza`
- Little Saigon Table: ZIP `45208`, cuisine `Vietnamese`
- Maki Social: ZIP `45220`, cuisines `Japanese`, `Sushi`
- Over-the-Rhine Tacos: ZIP `45202`, cuisines `Mexican`, `Tacos`
- Queen City Curry: ZIP `45202`, cuisine `Indian`
- Riverfront Grill: ZIP `41011`, cuisine `American`

## Development/Test Scenario Seed Data

### User accounts

The current snapshot contains 6 scenario users.

| Username | Display name | Roles | Home ZIP | Social goal | Discovery |
|---|---|---|---|---|---|
| alex | Alex Mercer | User | 45220 | Friends | Enabled |
| brooke | Brooke Lane | User | 45202 | Dating | Enabled |
| casey | Casey Harper | User | 45206 | Networking | Enabled |
| devon | Devon Brooks | User, Moderator | 45219 | Networking | Enabled |
| emery | Emery Stone | User, Admin | 41011 | Networking | Enabled |
| fin | Fin Carter | User | 45212 | Friends | Disabled |

All seeded scenario accounts use the same development password:

- `TasteBudz123!`

### User preference data

- alex: cuisines `Japanese`, `Sushi`; spice `Medium`; allergy `Peanuts`
- brooke: cuisines `Indian`, `Pizza`; spice `Hot`; dietary flag `Vegetarian`; allergy `Shellfish`
- casey: cuisine `Mediterranean`; spice `Mild`; dietary flag `Halal`
- devon: cuisine `Thai`; spice `Medium`
- emery: cuisine `American`; spice `Medium`; dietary flag `Gluten-Aware`
- fin: cuisine `Mexican`; spice `Hot`; allergy `Dairy`

### Availability

Recurring windows:

- alex: Friday dinner, `18:00` to `21:30`
- brooke: Late dinner, `19:00` to `22:00`
- casey: Weekend social, Saturday `17:00` to `20:00`

One-off windows:

- devon: Moderator evening availability, `2026-03-29T17:00:00Z` to `2026-03-29T21:00:00Z`
- fin: Open for tacos, `2026-03-30T18:00:00Z` to `2026-03-30T22:00:00Z`

### Discovery, Budz, and blocking

Swipe decisions:

- alex liked brooke
- alex passed fin
- brooke liked alex
- casey liked alex

Budz:

- alex and brooke are connected

Blocks:

- brooke blocked fin

### Groups

- Clifton Supper Club
  - owner: alex
  - visibility: Public
  - active members: alex, brooke

- Quiet Table
  - owner: casey
  - visibility: Private
  - active members: casey, devon

Group invites:

- Quiet Table: devon invited by casey, status `Accepted`
- Quiet Table: emery invited by casey, status `Pending`

### Events

- Friday Sushi Crawl
  - host: alex
  - type: Open
  - status: Open
  - capacity: 4
  - min participants: 2
  - restaurant: Maki Social
  - group: Clifton Supper Club
  - participants: alex joined, brooke joined, devon invited

- Quiet Table Planning Dinner
  - host: casey
  - type: Closed
  - status: Open
  - capacity: 3
  - min participants: 2
  - cuisine target: Mediterranean
  - group: Quiet Table
  - participants: casey joined, devon invited

- Last Week Pizza Night
  - host: brooke
  - type: Open
  - status: Completed
  - capacity: 4
  - min participants: 2
  - restaurant: Late Night Pizza Co
  - participants: brooke joined, alex joined, fin left

### Chat seed data

Threads:

- 1 event chat thread for Friday Sushi Crawl
- 1 group chat thread for Clifton Supper Club
- 1 group chat thread for Quiet Table

Messages:

- alex: `Booked Maki Social for Friday. Who is in?`
- brooke: `Count me in. I can be there around seven.`
- brooke: `Anyone want to plan something for next week too?`
- casey: `Keeping this one small. Reply when you confirm.`

### Notifications

- brooke: `You joined Friday Sushi Crawl.`
- devon: `Casey invited you to Quiet Table Planning Dinner.`
- brooke: `You and Alex are now Budz.`
- emery: `Casey invited you to Quiet Table.`

### Moderation and audit

Report:

- brooke filed a Safety report for repeated unwanted contact after a block
- status: Resolved

Moderation action:

- devon applied a discovery visibility restriction during review

Restriction:

- fin has an active `DiscoveryVisibility` restriction issued by devon

Audit entry:

- one `RestrictionCreated` audit log entry exists for that restriction

## Refreshing the Shared Snapshot

To rebuild the tracked SQLite snapshot from the SQL files:

```powershell
python src\TasteBudz.Database\init_sqlite.py --with-test-data
```

That command:

- deletes the current `src/TasteBudz.Database/TasteBudz.sqlite`
- recreates it from the canonical schema
- reapplies the shared reference seed
- reapplies the development/test scenario seed

Use that command when the SQL seed files change and you want the tracked snapshot to match them again.
