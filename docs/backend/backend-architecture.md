# TasteBudz / TasteMatch Backend Architecture

This document defines the target backend architecture for **TasteBudz / TasteMatch**. It keeps the system practical for a capstone team, preserves the approved feature direction, and stays valid even if the final persistence implementation changes.

The design remains intentionally simple:

- **backend only**
- **ASP.NET Core Web API**
- **modular monolith**
- **SQL Server / Azure SQL**
- **thin controllers, business rules in services/domain logic**
- **no microservices, no event sourcing, no speculative enterprise patterns**
- **persistence mechanism intentionally left open**

---

## 1. Overview

TasteBudz should remain a **single deployable modular monolith** with a **frontend-agnostic HTTP API** and **server-owned business rules**.

The backend is responsible for:

- auth and authorization
- onboarding and profile state
- preferences, allergies, availability, and privacy
- restaurant catalog and filtering
- events, participation, and lifecycle enforcement
- groups and group ownership rules
- discovery, swipes, Budz, and blocking
- messaging and access control across event, group, and direct chat scopes
- notifications
- moderation, reports, bans, and audit logging

The architecture should remain stable even if:

- the frontend becomes MVC, Razor, Blazor, React, or another client
- persistence uses EF Core, native SQL, stored procedures, or a hybrid mix

The key architectural position is unchanged: **the backend owns correctness**. Clients may guide UX, but they do not decide event capacity, event status, moderation outcomes, privacy visibility, group ownership, or chat access.

---

## 2. Architecture Style

TasteBudz uses a **layered modular monolith architecture**.

Primary request flow:

```text
Frontend
↓
Controllers
↓
Services
↓
Repositories
↓
Database
```

### Layer responsibilities

| Layer | Responsibility |
|---|---|
| Controller | HTTP contract, authentication, request validation, response mapping |
| Service | Business workflows, business rules, orchestration, transaction ownership |
| Repository | Database access and persistence implementation behind module-defined interfaces |
| Database | Durable data storage and integrity safeguards |

Controllers must remain **thin** and delegate business rules to services.

Repositories must not contain business rules. They own persistence access, not business policy.

The architecture uses a repository-based layering rule at the module boundary. Repository interfaces keep the backend flexible if different persistence implementations are needed later.

## 3. Core Architectural Principles

The backend architecture follows these guiding principles:

- **Thin Controllers**
- **Service-Owned Business Logic**
- **Light Domain Model**
- **Clear Module Boundaries**
- **Single Deployable Backend**
- **Repository-Based Persistence Boundaries**
- **Feature-Flagged Growth for Later Capabilities**

The system should remain **simple to implement, easy to test, and stable under later growth**.

---

## 4. Module Structure

The top-level module set remains intentionally small.

### 4.1 Core Modules

1. **Auth & Access**
2. **Profiles**
3. **Restaurants**
4. **Events**
5. **Groups**
6. **Discovery / Budz**
7. **Messaging**
8. **Notifications**
9. **Moderation & Audit**

### 4.2 Internal Extension Areas

Later capabilities should grow inside existing modules rather than creating new distributed systems.

- **Restaurants.Catalog** — seeded restaurant records, search, filtering, simple suggestions
- **Restaurants.Operations** *(later)* — restaurant admin accounts, slots, reservation windows, discount rules, restaurant-driven operational actions
- **Messaging.EventChat** — MVP event chat
- **Messaging.GroupChat** *(later / feature-gated if needed)* — group-linked chat
- **Messaging.DirectChat** *(later, feature-flagged)* — 1-on-1 messaging for Budz / approved direct threads

### 4.3 Module boundary rule

Each module should own:

- its API endpoints
- its use-case services
- its business rules
- its repository interfaces and implementations
- its DTOs/contracts

Cross-module access should happen through explicit services or internal application interfaces, not by directly reaching into another module’s controllers or storage details.

### 4.4 Dependency direction

Treat **Profiles, Restaurants, Events, Groups, Discovery, and Messaging** as the main business modules.
Treat **Auth, Notifications, and Moderation & Audit** as supporting modules.

General rule:

- business modules may depend on supporting modules
- supporting modules should not depend on business modules for core policy decisions
- circular dependencies must be avoided

Valid examples:

- `EventService → NotificationService`
- `EventService → AuditLogService`
- `MessagingService → ModerationService`

Invalid example:

- `EventService → GroupService → EventService`

---

## 5. Responsibilities of Each Module

### 5.1 Auth & Access

**Purpose**  
Own registration, login, authenticated identity, coarse authorization, and current-user context.

**Main responsibilities**

- account creation
- credential verification
- token/session issuance
- logout behavior
- password hashing
- role/claim loading
- current-user access for other modules

**Key services**

- `AuthService`
- `CredentialService`
- `TokenOrSessionService`
- `CurrentUserAccessor`

---

### 5.2 Profiles

**Purpose**  
Own onboarding, profile state, preferences, dietary/allergy data, availability, privacy, and blocking.

**Main responsibilities**

- first-run onboarding completion
- profile CRUD
- dashboard/profile summary
- dietary preferences, cuisine tags, spice tolerance
- allergy warnings and dietary flags
- availability window management
- ZIP/location context storage for filtering and proximity decisions
- social goal / dining intent fields already in scope
- privacy settings and discovery visibility
- user blocking and unblock

**Key services**

- `OnboardingService`
- `ProfileService`
- `PreferenceService`
- `AvailabilityService`
- `PrivacyService`
- `BlockingService`
- `ProfileDashboardQueryService`

---

### 5.3 Restaurants

**Purpose**  
Own the restaurant catalog, search/filtering, simple suggestions now, and restaurant operational features later.

**Main responsibilities (MVP)**

- seeded restaurant storage
- browse/search/filter by cuisine, price tier, and proximity-related inputs
- support restaurant selection during event creation
- simple suggestion endpoints

**Future responsibilities (later)**

- restaurant admin accounts
- restaurant-managed profile updates
- slot creation and cancellation
- slot-linked reservation support for events/groups
- discount threshold rules
- restaurant-driven operational constraints

**Key services**

- `RestaurantCatalogService`
- `RestaurantSearchService`
- `RestaurantRecommendationService`
- later: `RestaurantAdminService`, `RestaurantSlotService`, `DiscountEligibilityService`

---

### 5.4 Events

**Purpose**  
Own the main dining coordination workflow: event creation, invites, participation, capacity enforcement, lifecycle transitions, and event-to-restaurant selection.

**Main responsibilities**

- create open and closed events
- store optional `GroupId`
- store selected restaurant in MVP
- later store optional slot/reservation reference
- manage closed-event invites
- manage join/leave/accept/decline flows
- enforce capacity and `DecisionAt`
- maintain server-controlled event status
- cancel or complete events
- browse/search open events

**Key services**

- `EventService`
- `EventParticipationService`
- `EventInviteService`
- `EventLifecycleService`
- `EventBrowseService`

**Architectural note**  
Events remain the most transaction-sensitive module in the system. Capacity enforcement, invite acceptance, and lifecycle evaluation must stay server-controlled regardless of persistence style.

---

### 5.5 Groups

**Purpose**  
Own persistent social groups, group membership, ownership rules, discoverability, group-linked events, and later group chat authorization context.

**Main responsibilities**

- create groups
- join/leave groups
- owner-only group management
- public/private visibility
- owner member-removal actions
- ownership transfer
- group dissolution / deletion
- expose group-linked event context

**Key services**

- `GroupService`
- `GroupMembershipService`
- `GroupOwnershipService`
- `GroupBrowseService`

**Ownership transfer rules**

- only the current group owner may transfer ownership
- ownership may transfer only to a current active group member
- ownership transfer must be timestamped and auditable
- transfer should be atomic: new owner assignment and old owner demotion happen together

**Dissolution rules**

- only the current owner or an admin override path may dissolve/delete the group
- dissolve/delete requires explicit confirmation in the command contract
- dissolution should immediately remove the group from public discovery
- if there are active future group-linked events, the backend should block dissolution until those events are cancelled or unlinked
- dissolution should be timestamped and auditable

**Architectural note**  
Ownership transfer and dissolution are part of the base group lifecycle design and belong in the Groups module, not in scattered controllers or admin scripts. They are architected now, planned for MVP+ by default, and may be implemented during MVP if schedule allows. If they slip to MVP+, no architectural redesign should be required later.

---

### 5.6 Discovery / Budz

**Purpose**  
Own people discovery, swipes, Budz matching, pending connection state, and discovery filtering.

**Main responsibilities**

- user search by username/display name
- limited public profile previews
- swipe / like / pass flows
- mutual Budz creation
- pending request or connection state
- respect privacy, blocking, and moderation restrictions

**Key services**

- `DiscoveryService`
- `SwipeService`
- `BudzService`
- `DiscoveryFilterService`

---

### 5.7 Messaging

**Purpose**  
Own chat threads, messages, and access control across event chat, group chat, and direct 1-on-1 chat scopes.

**Planned responsibilities**

- event-linked chat threads
- group-linked chat threads for current group members
- direct 1-on-1 threads for Budz or approved direct threads
- text-only message persistence
- message pagination
- scope-specific access enforcement

**Rollout priority**

- event chat is the first chat scope to implement
- group chat should follow from the same messaging core
- direct chat remains feature-flagged until Budz, blocking, and moderation flows are stable enough to support it safely

**Recommended model**

Use a small shared messaging core:

- `ChatThread`
- `ChatMessage`
- `ChatScopeType` = `Event`, `Group`, `Direct`
- `ChatScopeId`

**Key services**

- `MessagingService`
- `EventChatAccessService`
- `GroupChatAccessService`
- `DirectChatAccessService`

---

### 5.8 Notifications

**Purpose**  
Own persisted in-app notifications for important state changes.

**Main responsibilities**

- create notification records
- expose notification center APIs
- track read state
- support event/group/discovery/moderation notifications as needed

**Key services**

- `NotificationService`
- `NotificationComposer`

**Architectural note**  
For MVP, notifications remain in-app only. Push/email can be added later behind the same notification creation flow.

---

### 5.9 Moderation & Audit

**Purpose**  
Own reports, moderation decisions, soft bans, and audit logging.

**Main responsibilities**

- create reports
- review moderation queue
- resolve reports
- apply/remove soft bans
- enforce moderation-related restrictions via service checks
- write immutable audit logs for sensitive actions

**Key services**

- `ReportService`
- `ModerationService`
- `BanService`
- `AuditLogService`

---

## 6. Layer Responsibilities

### Controllers / Endpoints

Controllers should:

- receive HTTP requests
- bind DTOs
- perform authentication and coarse policy checks
- call application services
- map results to response DTOs
- return consistent error responses

Controllers should not:

- enforce capacity or lifecycle rules
- implement ownership transfer logic
- make moderation decisions
- contain persistence branching logic
- duplicate feature-flag logic in many places

### Services

Services are the main home for use-case logic.

They should:

- enforce business rules
- coordinate workflows
- own transaction-sensitive use cases
- call repositories
- trigger notifications and audit logging
- call cross-module services through explicit boundaries
- apply feature checks at module entry points where needed

### Domain Models / Core Business Objects

Use a **light DDD-inspired approach**.

Domain objects may hold:

- invariant checks
- legal state transitions
- small helper methods such as `CanTransitionTo`, `CanAcceptInvite`, `CanTransferOwnership`

Do not force a heavy DDD model or persistence-specific entity design.

### DTOs / Contracts

DTOs are the stable contract between backend and frontend.

They should:

- be explicit
- avoid exposing persistence entities directly
- include server-computed permissions/state when useful
- stay stable when persistence technology changes

Examples:

- event detail DTO includes `status`, `canJoin`, `canLeave`, `canChat`
- group detail DTO includes `isOwner`, `canManage`, `canTransferOwnership`, `canDissolve`
- onboarding status DTO includes `isComplete`, `missingRequiredFields`

### Repositories

Each module contains a repository layer responsible for persistence operations behind module-defined repository interfaces.

Examples:

- `IEventRepository` / `EventRepository`
- `IGroupRepository` / `GroupRepository`
- `IUserRepository` / `UserRepository`

Repository responsibilities include both:

- read operations
- write operations

For MVP, the backend does **not** split query and command repositories. A single repository per module may contain both read and write behavior where that keeps the design simple.

Repositories may internally use EF Core, native SQL, stored procedures, or a hybrid implementation. The important rules are:

- controllers do not contain data-access logic
- repositories do not contain business rules
- transaction-sensitive workflows stay coordinated at service level
- persistence implementation can change behind repository interfaces without rewriting core business rules

### Infrastructure

Infrastructure contains technical plumbing such as:

- password hashing
- token/session plumbing
- current-user access
- time/clock abstractions
- feature flag plumbing
- logging
- background hosted services
- transaction helpers

Infrastructure supports modules. It does not own business policy.

---

## 7. Key Business Rules and Where They Live

### 7.1 Event Capacity Enforcement

**Lives in:** `EventParticipationService` plus persistence-level atomic protection.

The service decides whether the user may join. The chosen persistence path must make the seat claim atomic.

### 7.2 Join / Leave / Invite Accept / Decline

**Lives in:** `EventParticipationService` and `EventInviteService`.

These services own:

- open vs closed event rules
- invite acceptance/decline rules
- duplicate join prevention
- leave restrictions after `DecisionAt`
- event status recalculation after participant changes

### 7.3 Event Lifecycle Transitions

**Lives in:** `EventLifecycleService` with light domain transition rules on `Event`.

The backend must control:

- `OPEN`, `FULL`, `CONFIRMED`, `CANCELLED`, and related status changes
- `DecisionAt` evaluation
- cancellation and completion flows

### 7.4 Group Ownership Transfer

**Lives in:** `GroupOwnershipService`.

This service owns:

- validating current owner permissions
- validating target membership
- atomic owner swap
- audit logging and timestamps

### 7.5 Group Dissolution

**Lives in:** `GroupOwnershipService` or `GroupService`.

This service owns:

- explicit confirmation requirement
- discovery removal
- validation against active future linked events
- final state change or deletion behavior
- audit logging

### 7.6 Privacy / Blocking Behavior

**Lives in:** `PrivacyService`, `BlockingService`, and relevant access/query filters.

### 7.7 Moderation / Report Handling

**Lives in:** `ReportService`, `ModerationService`, `BanService`.

### 7.8 Notification Triggering

**Lives in:** the application services that complete the business action, calling `NotificationService` directly.

For this project, direct service calls are better than a full event bus.

### 7.9 Chat Access Rules

**Lives in:** scope-specific messaging access services.

- event chat access: `EventChatAccessService`
- group chat access: `GroupChatAccessService`
- direct chat access: `DirectChatAccessService`

### 7.10 Restaurant Slot / Discount Rules (Later)

**Lives in:** `Restaurants.Operations` services.

Events still own event state. Restaurants own restaurant operational rules.


### 7.11 Workflow / Orchestration Services

When a workflow spans multiple modules, one service should own the use case and orchestrate calls to other services and repositories.

Named examples that fit this architecture include:

- `EventCreationWorkflowService`
- `OwnershipTransferService`
- `GroupDissolutionService`

Controllers must not orchestrate complex workflows directly.


---

## 8. Security / Authorization Approach

### Core roles

Keep global roles simple:

- `User`
- `Moderator`
- `Admin`
- later, when enabled: `RestaurantAdmin`

Do not turn event host or group owner into permanent global roles. Those are contextual permissions derived from the relevant record.

### Authorization model

Use:

- authentication + coarse authorization policies at the endpoint level
- fine-grained ownership/membership checks inside services

### Sensitive data handling

The backend should:

- avoid exposing exact ZIP publicly
- avoid exposing allergy/private preference detail outside allowed workflows
- keep limited public profile previews for discovery
- respect blocks in both reads and writes
- apply least-privilege principles for database access

### Audit expectations

Sensitive actions should create audit records, including:

- moderation actions
- bans
- group ownership transfer
- group dissolution
- restaurant-admin operational overrides when added later

---

## 9. Feature Flag Strategy

Feature flags should be explicit and simple.

### Recommended flags

- `Messaging.DirectChatEnabled`
- `Messaging.GroupChatEnabled`
- `Notifications.PushEnabled`
- `Restaurants.OperationsEnabled`
- `Restaurants.SlotsEnabled`
- `Restaurants.DiscountsEnabled`
- `Discovery.ExperimentalSuggestionsEnabled`

### Where flags live

For a capstone-ready monolith, start with configuration-based flags:

- `appsettings.*`
- environment variables
- optional later admin/config store if needed

### How flags should be applied

- apply feature checks at the module entry point
- keep one gate per feature area where possible
- let disabled features fail fast before business logic/persistence runs

Recommended behavior:

- hidden/not launched feature: prefer `404`
- launched but caller lacks permission: use `403`
- feature exists but operation invalid: use normal domain/business error response

Schema/table support may exist before a feature is enabled, but unfinished capabilities should not be exposed through public endpoints or normal DTOs.

---

## 10. Persistence Approach

### 10.1 Persistence position

There is **no single primary persistence mechanism mandated by this architecture**.

The backend must remain valid if the final implementation uses:

- EF Core
- native SQL / ADO.NET / Dapper-style queries
- stored procedures
- a hybrid mix

The stable architectural rule is the **repository boundary**, not a single mandated data-access technology.

### 10.2 What the backend must define regardless of persistence style

The backend must still define clearly:

- ownership boundaries of core data
- required transactions
- concurrency-sensitive operations
- uniqueness rules
- status invariants
- audit requirements
- access and privacy rules

### 10.3 Repository interfaces and implementation flexibility

Each module should expose repository interfaces and keep persistence technology behind those interfaces.

That allows a module to begin with one implementation and change later without changing service-level business logic.

Examples:

- EF Core-backed repositories
- native SQL-backed repositories
- stored-procedure-backed repositories
- hybrid repositories that mix approaches where appropriate

### 10.4 AppDbContext guidance

If EF Core is chosen for all or part of the system, a **single `AppDbContext`** is the recommended default for MVP simplicity.

That `AppDbContext` may handle:

- entity mapping
- transaction management
- database connection handling

It should be registered during startup and injected into repository implementations through dependency injection.

However, the architecture must remain flexible enough to support different implementations behind repository interfaces if some modules later use SQL-first or stored-procedure-heavy approaches.

### 10.5 Practical repository pattern

Use module-level repositories, not generic one-size-fits-all abstractions.

Recommended pattern:

- one repository per module
- clear repository interfaces
- both read and write operations in the same module repository for MVP
- no forced query/command split in MVP

Avoid:

- a universal generic repository for everything
- leaking ORM-specific query abstractions across modules
- putting business rules into raw SQL or stored procedures with no service boundary

### 10.6 Required transaction boundaries

Important transaction boundaries include:

- event participation and invite state updates
- group ownership changes
- moderation decision + ban + audit log
- later slot reservation + event slot link

### 10.7 Required concurrency protections

Concurrency-sensitive flows include:

- multiple users taking the last event seat
- duplicate join attempts for the same user
- invite acceptance when one seat remains
- operations at or around `DecisionAt`
- later slot reservation contention

The protection mechanism may vary by persistence style, but the guarantee must not.

### 10.8 Database-level safeguards

Use database safeguards to backstop service logic.

Examples:

- unique constraint for participant uniqueness per event
- unique normalization for Budz pairs
- check constraints for event capacity limits
- row version / concurrency token when useful
- foreign keys for critical relationships
- audit tables treated as append-only

### 10.9 Stored procedure viability

Stored procedures are reasonable when they improve:

- atomicity
- clarity for multi-step DB workflows
- performance for operational logic
- deployment stability for known workflows

If stored procedures or SQL scripts are used, they must be source-controlled, versioned with schema changes, and deployable alongside application changes.

## 11. Testing Strategy

Testing focuses primarily on the **service layer**, plus real integration coverage for the chosen persistence path.

### Unit tests

Use unit tests primarily for service-layer business rules, typically with mocked repositories or test doubles behind repository interfaces.

Use unit tests for:

- event lifecycle rules
- join/leave/invite rules
- group ownership transfer rules
- group dissolution preconditions
- privacy/blocking decisions
- feature-gate decisions
- restaurant slot/discount rules when later added

### Integration / API tests

Integration/API tests should exercise the full request pipeline:

```text
HTTP Request
→ Controller
→ Service
→ Repository
→ Database
```

Use integration tests for:

- auth + onboarding flow
- profile CRUD and privacy behavior
- event create/join/leave/invite flows
- group create/join/leave/transfer/dissolve flows
- discovery and blocking behavior
- event chat endpoints
- moderation endpoints and policies

### Concurrency tests

These are required.

Focus on:

- last-seat contention for events
- duplicate join attempts
- invite acceptance contention
- `DecisionAt` edge cases
- later slot reservation contention

### Persistence-path tests

If the final backend uses mixed persistence styles, integration tests should hit the real chosen path for each module.

### Security and policy tests

Add focused tests for:

- owner-only group actions
- moderator/admin-only flows
- blocked-user restrictions
- disabled feature behavior (`404` vs `403` expectations)

---

## 12. MVP vs Later Boundaries

### MVP

Build and ship:

- auth and current-user access
- first-run onboarding and profile completion contract
- profile CRUD + account deletion
- cuisine preferences, spice tolerance, dietary flags, allergies
- availability windows
- privacy settings and blocking
- seeded restaurant catalog + filtering + simple suggestions
- people discovery search + swipe + Budz core
- open and closed events
- closed invites by username
- atomic join/leave and `DecisionAt` lock handling
- event lifecycle processing
- persistent groups with owner/member model
- basic group management
- event chat
- in-app notifications for state changes
- reports, moderation queue, soft bans, audit logging

### MVP+ / later

Add when core flows are stable:

- group ownership transfer and dissolution, architected in the base design now and planned for MVP+ by default; implement in MVP only if schedule allows, otherwise deliver in MVP+ without architectural change
- group chat, if it does not fit comfortably inside MVP delivery
- richer browse/feed layers
- advanced RSVP / cutoff controls
- push notifications
- restaurant-admin operations
- slots and slot-linked reservations
- discount thresholds

### MVP++ / feature-flagged / backend-ready

Design ready for later activation:

- direct 1-on-1 messaging
- restaurant-admin accounts across multiple restaurants
- operational slot cancellation flows
- smarter restaurant recommendation strategies

### Priority rule

If time is tight, do not cut correctness in these areas:

- event participation atomicity
- lifecycle/status correctness
- blocking/privacy enforcement
- moderation consistency
- group ownership permissions

---

## 13. Revised Implementation Order

### Phase 1 — foundation

1. module skeleton and shared error handling
2. auth and current-user plumbing
3. feature flag plumbing
4. database/schema baseline and source-controlled SQL path support

### Phase 2 — onboarding and profiles

5. onboarding status + completion flow
6. profile CRUD + account deletion
7. preferences, dietary flags, allergies, spice tolerance
8. availability windows
9. privacy settings and blocking
10. dashboard/profile summary

**Implementation-order note**

Events remain a core backend foundation because participation, lifecycle, and concurrency rules shape several other areas. However, if the team wants stronger alignment with the product’s socially/group-oriented experience, a minimal slice of Groups and Discovery may be pulled slightly earlier in parallel without changing the architecture or weakening the event-first backend foundation.

### Phase 3 — restaurants and core events

11. restaurant catalog and filter/search endpoints
12. simple restaurant suggestion service boundary
13. event create/read flows
14. participant model, unique constraints, and transactional join/leave logic
15. closed invite flow
16. lifecycle and `DecisionAt` processing

### Phase 4 — groups and social layer

17. group create/join/leave/manage
18. owner-only actions
19. ownership transfer
20. dissolution rules
21. discovery search + swipe + Budz core
22. basic browse/search for open events and public groups

### Phase 5 — communication and safety

23. messaging core
24. event chat
25. group chat
26. notifications center
27. report creation
28. moderation queue and resolution
29. soft bans
30. audit logging

### Phase 6 — hardening

31. integration tests across main flows
32. concurrency tests for event participation
33. authorization and blocked-user policy review
34. disabled-feature behavior review
35. architecture cleanup and documentation refresh

### Phase 7 — later extension layers

36. direct chat behind flag
37. restaurant-admin operations
38. slots / reservations / discounts
39. smarter restaurant recommendation logic

---

## 14. Final Recommendation

Keep TasteBudz as a **simple modular monolith** with:

- **thin controllers**
- **service-owned business logic**
- **light domain rules**
- **clear module boundaries**
- **persistence-neutral internals**
- **feature-flagged later growth**

That gives the team the right balance:

- simple enough for student implementation
- strong enough to keep business rules correct
- flexible enough to survive frontend and persistence changes
- clean enough to grow into group chat, direct chat, and restaurant operations later without major redesign
