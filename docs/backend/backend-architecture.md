# TasteBudz Backend Architecture

This document defines the target backend architecture for TasteBudz. The design stays practical for a capstone team while remaining strong enough to keep business rules correct and support later growth.

Core constraints:

- ASP.NET Core Web API backend
- single deployable modular monolith
- SQL Server / Azure SQL
- thin controllers, service-owned business rules
- repository boundary around persistence
- no microservices, event sourcing, or speculative enterprise patterns
- persistence implementation intentionally left open

## 1. Overview

TasteBudz should remain a single deployable modular monolith with a frontend-agnostic HTTP API and server-owned business rules.

The backend owns:

- auth and authorization
- onboarding and profile state
- preferences, allergies, availability, privacy, and blocking
- restaurant catalog and filtering
- events, participation, and lifecycle enforcement
- groups and ownership rules
- discovery, swipes, Budz, and safety filters
- messaging across event, group, and later direct-chat scopes
- notifications
- moderation, reports, restrictions, and audit logging

The stable architectural position is simple: the backend owns correctness. Clients can guide UX, but they do not decide event capacity, event status, moderation outcomes, privacy visibility, group ownership, or chat access.

## 2. Architecture Style

TasteBudz uses layered modular-monolith architecture.

```text
Frontend
-> Controllers
-> Services
-> Repositories
-> Database
```

Layer responsibilities:

| Layer | Responsibility |
|---|---|
| Controller | HTTP contract, auth, request validation, response mapping |
| Service | Business workflows, rules, orchestration, transaction ownership |
| Repository | Persistence access behind module-defined interfaces |
| Database | Durable storage and integrity safeguards |

Rules:

- Controllers stay thin.
- Services own business workflows and transaction-sensitive use cases.
- Repositories do not hold business policy.
- Infrastructure supports modules but does not define product behavior.

## 3. Core Architectural Principles

- Thin controllers
- Service-owned business logic
- Light domain model with explicit invariants
- Clear module boundaries
- Single deployable backend
- Persistence-neutral internals behind repositories
- Feature-flagged growth for later capabilities

## 4. Module Structure

### 4.1 Core Modules

1. Auth and Access
2. Profiles
3. Restaurants
4. Events
5. Groups
6. Discovery / Budz
7. Messaging
8. Notifications
9. Moderation and Audit

### 4.2 Internal Extension Areas

Later capabilities should grow inside existing modules rather than separate services.

- Restaurants.Catalog: seeded restaurant records, search, filtering, simple suggestions
- Restaurants.Operations: later restaurant admin accounts, slots, discount rules, and operational actions
- Messaging.EventChat: MVP event chat
- Messaging.GroupChat: MVP group chat
- Messaging.DirectChat: later 1-on-1 messaging behind flags

### 4.3 Boundary Rule

Each module owns:

- its endpoints
- its application services
- its rules and invariants
- its repository interfaces and implementations
- its DTO/contracts

Cross-module access happens through explicit services or internal interfaces, not by reaching into another module's controllers or storage details.

### 4.4 Dependency Direction

Treat Profiles, Restaurants, Events, Groups, Discovery, and Messaging as business modules.
Treat Auth, Notifications, and Moderation/Audit as supporting modules.

General rule:

- business modules may depend on supporting modules
- supporting modules should not depend on business modules for core policy decisions
- circular dependencies must be avoided

## 5. Responsibilities by Module

### 5.1 Auth and Access

Own registration, login, authenticated identity, coarse authorization, and current-user context.

Key responsibilities:

- account creation
- credential verification
- token/session issuance
- logout behavior
- password hashing
- role/claim loading
- current-user access for other modules

Suggested services:

- `AuthService`
- `CredentialService`
- `TokenOrSessionService`
- `CurrentUserAccessor`

### 5.2 Profiles

Own onboarding, profile state, preferences, availability, privacy, and blocking.

Key responsibilities:

- onboarding completion status
- profile CRUD and dashboard summary
- cuisine preferences, spice tolerance, dietary/allergy data
- recurring and one-off availability windows
- ZIP/location context for filtering and proximity rules
- privacy settings and discovery visibility
- blocking and unblock flows

Suggested services:

- `OnboardingService`
- `ProfileService`
- `PreferenceService`
- `AvailabilityService`
- `PrivacyService`
- `BlockingService`
- `ProfileDashboardQueryService`

### 5.3 Restaurants

Own the restaurant catalog, search/filtering, and simple suggestions now, plus restaurant operations later.

MVP responsibilities:

- seeded restaurant storage
- browse/search/filter by cuisine, price tier, and proximity-related inputs
- support restaurant selection during event creation
- expose simple suggestion endpoints using host ZIP/radius and optional coarse midpoint logic

Later responsibilities:

- restaurant admin accounts
- restaurant-managed profile updates
- slot creation/cancellation
- slot-linked reservations for events
- discount threshold rules
- restaurant-owned operational constraints

Suggested services:

- `RestaurantCatalogService`
- `RestaurantSearchService`
- `RestaurantRecommendationService`
- later `RestaurantAdminService`, `RestaurantSlotService`, `DiscountEligibilityService`

### 5.4 Events

Own the main dining coordination workflow: event creation, invites, participation, capacity enforcement, lifecycle transitions, and event-to-restaurant selection.

Key responsibilities:

- create open and closed events
- edit host-owned event details and notify participants of material changes
- store optional `GroupId`
- store selected restaurant from the internal catalog in MVP
- manage closed-event invites
- manage join/leave/accept/decline flows
- enforce capacity and `DecisionAt`
- support host removal of non-host participants before `DecisionAt`
- support moderator/admin participant removal as a safety/support override
- maintain server-controlled event status
- cancel events and automatically complete them after the scheduled time passes
- browse/search open events

Suggested services:

- `EventService`
- `EventParticipationService`
- `EventInviteService`
- `EventLifecycleService`
- `EventBrowseService`

Architectural note: Events remain the most transaction-sensitive module in the system. Capacity enforcement, invite acceptance, lifecycle evaluation, and automatic completion must stay server-controlled regardless of persistence style.

### 5.5 Groups

Own persistent groups, group membership, ownership rules, discoverability, group-linked event context, and group chat authorization context.

Key responsibilities:

- create groups
- auto-create owner membership on group creation
- join/leave groups
- owner-only group management
- public/private visibility
- owner member-removal actions
- ownership transfer later
- group dissolution later
- expose group-linked event context

Suggested services:

- `GroupService`
- `GroupMembershipService`
- `GroupOwnershipService`
- `GroupBrowseService`

Rules to preserve:

- `Group.OwnerUserId` is the canonical ownership source
- the owner must be an active member
- public groups allow direct join in MVP
- private groups require invitation in MVP
- group membership does not replace event participation

### 5.6 Discovery / Budz

Own people discovery, swipes, mutual Budz creation, Budz list retrieval, and discovery filtering.

Key responsibilities:

- user search by username/display name
- limited public profile previews
- swipe / Like / Pass flows
- mutual Budz creation
- list current Budz
- respect privacy, blocking, and moderation restrictions

Suggested services:

- `DiscoveryService`
- `SwipeService`
- `BudzService`
- `DiscoveryFilterService`

Canonical MVP rule: one effective directional swipe decision exists per actor/subject pair, and reciprocal effective Like decisions create a Budz connection. Pending Bud-request state is not part of MVP.

### 5.7 Messaging

Own chat threads, messages, and access control across event chat, group chat, and later direct-chat scopes.

Key responsibilities:

- event-linked chat threads
- group-linked chat threads for current members
- later direct 1-on-1 threads for approved users/Budz
- text-only message persistence
- message pagination and history retrieval
- SignalR-based real-time delivery for event and group chat
- scope-specific access enforcement

Rollout priority:

- event chat and group chat are both part of MVP and should share one messaging core
- direct chat remains feature-flagged until Budz, blocking, and moderation flows are stable enough to support it safely

MVP shared-chat rule:

- event chat access is derived from current event participation state
- group chat access is derived from current active group membership
- blocking alone does not split or hide a shared event/group chat if both users remain authorized in that shared context
- separation in shared chat requires host/owner/moderator action

Suggested model:

- `ChatThread`
- `ChatMessage`
- `ChatScopeType` = `Event`, `Group`, `Direct`
- `ChatScopeId`

Suggested services:

- `MessagingService`
- `EventChatAccessService`
- `GroupChatAccessService`
- `DirectChatAccessService`
- `ChatHub` for SignalR connection/auth plumbing

### 5.8 Notifications

Own persisted in-app notifications for important state changes.

Key responsibilities:

- create notification records
- expose notification-center APIs
- track read state
- support event, group, discovery, and moderation notifications as needed

Suggested services:

- `NotificationService`
- `NotificationComposer`

For MVP, notifications remain in-app only. Push/email can be added later without changing the core notification creation flow.

### 5.9 Moderation and Audit

Own reports, moderation decisions, scoped restrictions, and audit logging.

Key responsibilities:

- create reports
- review moderation queue
- resolve reports
- apply/remove restrictions
- enforce moderation-related restrictions via service checks
- write immutable audit logs for sensitive actions

Suggested services:

- `ReportService`
- `ModerationService`
- `RestrictionService`
- `AuditLogService`

## 6. Layer Rules

### Controllers

Controllers should:

- receive HTTP requests
- bind DTOs
- perform authentication and coarse policy checks
- call services
- map results to response DTOs
- return consistent error responses

Controllers should not:

- enforce capacity or lifecycle rules
- implement ownership transfer logic
- make moderation decisions
- contain persistence branching logic
- duplicate feature-flag logic in many places

### Services

Services should:

- enforce business rules
- coordinate workflows
- own transaction-sensitive use cases
- call repositories and cross-module services through explicit boundaries
- trigger notifications and audit logging
- apply feature gates at module entry points

### Domain Model

Use a light DDD-inspired model.

Domain objects may hold:

- invariant checks
- legal state transitions
- helper methods such as `CanTransitionTo`, `CanAcceptInvite`, `CanTransferOwnership`

Do not force a heavy persistence-shaped domain model.

### DTOs

DTOs are the stable contract between backend and frontend.

They should:

- be explicit
- avoid exposing persistence entities directly
- include server-computed permissions/state when useful
- stay stable when persistence technology changes

### Repositories

Use module-level repositories behind module-defined interfaces.

Rules:

- one repository layer per module is fine for MVP
- read and write operations may live together for MVP simplicity
- repositories may use EF Core, SQL-first code, stored procedures, or a hybrid mix internally
- controllers do not contain data access
- repositories do not contain business rules

### Infrastructure

Infrastructure contains:

- password hashing
- token/session plumbing
- current-user access
- time abstractions
- feature flag plumbing
- logging
- background hosted services
- transaction helpers

Infrastructure supports modules. It does not own business policy.

## 7. Key Business Rules and Where They Live

### 7.1 Event Capacity Enforcement

Lives in `EventParticipationService` plus persistence-level atomic protection.

### 7.2 Join / Leave / Invite Accept / Decline

Lives in `EventParticipationService` and `EventInviteService`.

These services own:

- open vs closed event rules
- invite acceptance/decline rules
- duplicate join prevention
- leave restrictions after `DecisionAt`
- event status recalculation after participant changes

### 7.3 Event Lifecycle Transitions

Lives in `EventLifecycleService` with light transition rules on `Event`.

The backend must control:

- `OPEN`, `FULL`, `CONFIRMED`, `CANCELLED`, and `COMPLETED` status changes
- `DecisionAt` evaluation
- cancellation flows
- automatic completion after the scheduled event time passes

### 7.4 Group Ownership Transfer and Dissolution

Lives in `GroupOwnershipService` or `GroupService` when those flows are enabled.

The service owns:

- validating current owner permissions
- validating target membership
- explicit confirmation requirement
- discovery removal when dissolved
- audit logging and timestamps

### 7.5 Privacy and Blocking Behavior

Lives in `PrivacyService`, `BlockingService`, and related access/query filters.

Soft blocking prevents new direct interaction paths such as direct/private messaging, new Bud interactions, and event/group invitations between the pair. It does not automatically remove users from already shared contexts or split existing shared-context chat.

### 7.6 Moderation and Reports

Lives in `ReportService`, `ModerationService`, and `RestrictionService`.

### 7.7 Notification Triggering

Lives in the services that complete the business action, which call `NotificationService` directly.

For this project, direct service calls are better than a full event bus.

### 7.8 Chat Access Rules

Lives in scope-specific messaging access services and the SignalR hub plumbing.

- event chat access: `EventChatAccessService`
- group chat access: `GroupChatAccessService`
- direct chat access later: `DirectChatAccessService`
- real-time transport hub: `ChatHub`

### 7.9 Restaurant Slot and Discount Rules (Later)

Lives in Restaurants.Operations services.

Events still own event state. Restaurants own restaurant operational rules.

## 8. Security and Authorization Approach

Core global roles:

- `User`
- `Moderator`
- `Admin`
- later `RestaurantAdmin`

Contextual permissions such as host and group owner are derived from records, not stored as permanent global roles.

Use:

- endpoint-level auth and coarse authorization policies
- fine-grained ownership/membership checks inside services

Sensitive data handling rules:

- avoid exposing exact ZIP publicly
- avoid exposing allergy/private preference detail outside allowed workflows
- keep limited public profile previews for discovery
- respect blocks in both reads and writes
- apply least-privilege access to the database

Audit expectations:

- moderation actions
- restrictions
- group ownership transfer/dissolution when enabled
- restaurant-admin operational overrides when later added

## 9. Feature Flag Strategy

Recommended flags:

- `Messaging.DirectChatEnabled`
- `Messaging.GroupChatEnabled`
- `Notifications.PushEnabled`
- `Restaurants.OperationsEnabled`
- `Restaurants.SlotsEnabled`
- `Restaurants.DiscountsEnabled`
- `Discovery.ExperimentalSuggestionsEnabled`

Recommended behavior:

- hidden/not-launched feature: prefer `404`
- launched but caller lacks permission: use `403`
- feature exists but operation is invalid: return a normal domain/business error

Flags should be checked at module entry points rather than scattered throughout the stack.

## 10. Persistence Approach

There is no single required persistence mechanism.

The architecture remains valid with:

- EF Core
- SQL-first or Dapper-style repositories
- stored procedures
- a hybrid mix

Stable requirements regardless of persistence style:

- ownership boundaries of core data
- required transactions
- concurrency-sensitive operations
- uniqueness rules
- status invariants
- audit requirements
- privacy and access rules

Recommended MVP pattern:

- module-level repositories
- one repository interface per module or feature area
- both read and write behavior in the same module repository when that keeps the design simple
- no generic one-size-fits-all repository abstraction

Required transaction boundaries include:

- event participation and invite-state updates
- moderation decision + restriction + audit log
- later group ownership changes and slot reservations

Required concurrency protections include:

- multiple users taking the last event seat
- duplicate join attempts
- invite acceptance when one seat remains
- operations at or around `DecisionAt`
- later slot reservation contention

Database safeguards should backstop service logic, for example:

- unique participant constraint per event/user
- unique normalized Budz pair constraint
- event capacity check constraints where helpful
- foreign keys for critical relationships
- append-only handling for audit tables

## 11. Testing Strategy

Primary focus: service-layer rules plus real integration coverage for the chosen persistence path.

### Unit Tests

Use unit tests for:

- event lifecycle rules
- join/leave/invite rules
- group ownership rules
- privacy/blocking behavior
- feature-gate decisions
- later restaurant slot/discount rules

### Integration / API Tests

Use integration tests for:

- auth + onboarding
- profile CRUD and privacy behavior
- event create/join/leave/invite flows
- group create/join/leave flows
- discovery and blocking behavior
- event chat and group chat endpoints/hub auth
- moderation endpoints and policy enforcement

### Concurrency Tests

These are required.

Focus on:

- last-seat contention
- duplicate joins
- invite-accept contention
- `DecisionAt` edge cases
- later slot reservation contention

### Security and Policy Tests

Add focused tests for:

- owner-only group actions
- moderator/admin-only flows
- blocked-user restrictions
- disabled-feature behavior (`404` vs `403`)

## 12. MVP vs Later Boundaries

### MVP

Build and ship:

- auth and current-user access
- onboarding and profile completion status
- profile CRUD + account deletion
- cuisine preferences, spice tolerance, dietary flags, allergies
- recurring and one-off availability windows
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
- group chat
- in-app notifications for state changes and event updates
- reports, moderation queue, restrictions, and audit logging

### MVP+ / Later

Add when core flows are stable:

- group ownership transfer and dissolution
- richer browse/feed layers
- advanced RSVP/cutoff controls
- push notifications
- restaurant-admin operations
- slots and slot-linked reservations
- discount thresholds

### MVP++ / Feature-Flagged

Design ready for later activation:

- direct 1-on-1 messaging
- restaurant-admin accounts across multiple restaurants
- operational slot cancellation flows
- smarter restaurant recommendation strategies

Priority rule: if time is tight, do not cut correctness in event participation atomicity, lifecycle/status rules, blocking/privacy enforcement, moderation consistency, or group ownership permissions.

## 13. Revised Implementation Order

### Phase 1 - Foundation

1. module skeleton and shared error handling
2. auth and current-user plumbing
3. feature flag plumbing
4. database/schema baseline and source-controlled SQL path support

### Phase 2 - Onboarding and Profiles

5. onboarding status + completion flow
6. profile CRUD + account deletion
7. preferences, dietary flags, allergies, spice tolerance
8. recurring and one-off availability windows
9. privacy settings and blocking
10. dashboard/profile summary

### Phase 3 - Restaurants and Core Events

11. restaurant catalog and filter/search endpoints
12. simple restaurant suggestion service boundary
13. event create/read flows
14. participant model, unique constraints, and transactional join/leave logic
15. closed invite flow
16. lifecycle and `DecisionAt` processing

### Phase 4 - Groups and Social Layer

17. group create/join/leave/manage
18. owner-only actions
19. discovery search + swipe + Budz core
20. basic browse/search for open events and public groups

### Phase 5 - Communication and Safety

21. messaging core
22. event chat
23. group chat (SignalR + history retrieval)
24. notifications center
25. report creation
26. moderation queue and resolution
27. restrictions
28. audit logging

### Phase 6 - Hardening

29. integration tests across main flows
30. concurrency tests for event participation
31. authorization and blocked-user policy review
32. disabled-feature behavior review
33. architecture cleanup and documentation refresh

### Phase 7 - Later Extensions

34. group ownership transfer and dissolution
35. direct chat behind flag
36. restaurant-admin operations
37. slots / reservations / discounts
38. smarter restaurant recommendation logic

## 14. Final Recommendation

Keep TasteBudz as a simple modular monolith with:

- thin controllers
- service-owned business logic
- light domain rules
- clear module boundaries
- persistence-neutral internals
- feature-flagged later growth

This gives the team the right balance:

- simple enough for student implementation
- strong enough to keep business rules correct
- flexible enough to survive frontend and persistence changes
- clean enough to grow into direct chat and restaurant operations later without major redesign
