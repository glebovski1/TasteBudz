# TasteBudz Technical Overview

TasteBudz is a capstone-level ASP.NET Core application built as a single-deployable modular monolith. The deployed host is `TasteBudz.Web.Mvc`, which serves the MVC frontend, API controllers, SignalR chat hub, backend services, and persistence wiring in one ASP.NET Core app.

## Stack

- .NET 9 and ASP.NET Core
- MVC frontend in `src/TasteBudz.Web.Mvc`
- Backend module library in `src/TasteBudz.Backend`
- Controller-based REST API under `/api/v1`
- SignalR chat hub at `/hubs/chat`
- EF Core runtime persistence with SQLite for local development and integration tests
- SQL Server / Azure SQL support for Azure production deployment
- Source-controlled database scripts in `src/TasteBudz.Database`
- xUnit-based unit and integration tests under `tests`

## Architecture

The backend follows a layered modular-monolith style:

```text
MVC / API Controllers
-> Services
-> Repositories
-> Database
```

Controllers handle HTTP contracts, authentication, request binding, and response mapping. Services own business workflows and server-enforced rules. Repositories isolate persistence access behind module boundaries. API DTOs are explicit contracts and should not expose persistence entities directly.

Core backend modules include Auth, Profiles, Restaurants, Events, Groups, Discovery/Budz, Messaging, Media, Notifications, Moderation/Audit, Restaurant Operations, and feature-flagged Payments/Checkout.

## Persistence

The application uses relational persistence through `TasteBudzDbContext`. SQLite is the local development and automated test provider, initialized from canonical SQL scripts when allowed. Azure production uses SQL Server / Azure SQL, with schema changes applied manually from source-controlled SQL Server scripts rather than automatic production migrations.

The checked-in database scripts are authoritative for schema and seed data. A checked-in `.sqlite` database file is not treated as the source of truth.

## API and Realtime Behavior

The public backend surface is REST-oriented, with protected endpoints requiring authenticated user context. Server-owned lifecycle state, such as event status, capacity, participation state, moderation outcomes, and chat access, is controlled by backend services rather than clients.

Messaging uses one shared SignalR hub plus HTTP history endpoints. Event chat, group chat, support chat, and feature-flagged direct chat share the same scoped messaging model, with access derived from current event participation, group membership, support-user context, or Budz connection state.

## Testing

The test strategy prioritizes service rules, API contracts, authorization, persistence-backed workflows, and concurrency-sensitive behavior. Unit tests cover domain and service rules, while integration tests use realistic host/API flows and temporary SQLite databases rebuilt from canonical SQL assets.

High-risk areas include event capacity, invite acceptance, `DecisionAt` locking, blocking/privacy behavior, chat access, moderation restrictions, restaurant slot reservations, and feature-flagged checkout behavior.

## Deployment

The production target is Azure App Service running the single ASP.NET Core host. Azure SQL is selected by configuration through the persistence provider setting. Production startup validates expected schema but does not create or migrate the database automatically.

