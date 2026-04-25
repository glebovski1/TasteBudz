# TasteBudz Backend Architecture

This document defines the target backend architecture for TasteBudz. The design stays practical for a capstone team while remaining strong enough to keep business rules correct and support later growth.

Core constraints:

- ASP.NET Core modular monolith hosted by one deployable ASP.NET Core web app
- MVC frontend, backend API controllers, SignalR hub, backend services, and EF Core persistence wiring in one release artifact
- Azure SQL / SQL Server production persistence with SQLite retained for local development and automated tests
- thin controllers, service-owned business rules
- repository boundary around persistence
- no microservices, event sourcing, or speculative enterprise patterns
- production schema managed manually through source-controlled SQL Server/Azure SQL scripts; local SQLite schema remains source-controlled for development/test bootstrap

## 1. Overview

TasteBudz should remain a single deployable modular monolith with a frontend-agnostic HTTP API, MVC frontend, SignalR chat hub, and server-owned business rules in one ASP.NET Core host.

The backend owns:

- auth and authorization
- admin-issued password reset-token workflows
- onboarding and profile state
- preferences, allergies, availability, privacy, and blocking
- restaurant catalog and filtering
- events, participation, lifecycle enforcement, and completed-event feedback
- groups and ownership rules
- discovery, swipes, Budz, and safety filters
- messaging across event, group, support, and feature-flagged direct-chat scopes
- database-backed media assets and context-based access control
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
- Single deployable web host for MVC, backend API, and SignalR
- Persistence-neutral module boundaries behind repositories
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
8. Media
9. Notifications
10. Moderation and Audit
11. Payments

### 4.2 Internal Extension Areas

Feature-gated capabilities should grow inside existing modules where the boundary fits, and otherwise use a small module boundary rather than a separate deployable service.

- Restaurants.Catalog: seeded restaurant records, search, filtering, simple suggestions
- Restaurants.Operations: feature-flagged restaurant admin accounts, assignments, slots, slot reservations, discount rules, and operational actions
- Messaging.EventChat: MVP event chat
- Messaging.GroupChat: MVP group chat
- Messaging.SupportChat: MVP user-to-admin support chat
- Messaging.DirectChat: 1-on-1 messaging behind flags
- Payments.Checkout: simulation-only checkout sessions behind flags

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
Treat Auth, Media, Notifications, and Moderation/Audit as supporting modules.

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
- admin-issued one-time password reset tokens
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

Own the restaurant catalog, search/filtering, simple suggestions, and feature-flagged restaurant operations.

MVP responsibilities:

- seeded restaurant storage
- browse/search/filter by cuisine, price tier, and proximity-related inputs
- support restaurant selection during event creation
- expose simple suggestion endpoints using host ZIP/radius and optional coarse midpoint logic

Feature-flagged restaurant operations responsibilities:

- restaurant admin assignments controlled by global admins
- restaurant-managed profile updates
- slot creation/cancellation
- slot-linked reservations for events
- discount threshold simulation rules
- restaurant-owned operational constraints

Suggested services:

- `RestaurantCatalogService`
- `RestaurantSearchService`
- `RestaurantRecommendationService`
- `RestaurantAdminAssignmentService`
- `ManagedRestaurantService`
- `RestaurantSlotService`
- `EventSlotReservationService`
- `DiscountEligibilityService`

### 5.4 Events

Own the main dining coordination workflow: event creation, invites, participation, capacity enforcement, lifecycle transitions, and event-to-restaurant selection.

Key responsibilities:

- create open and closed events
- edit host-owned event details and notify participants of material changes
- store optional `GroupId` when the current user is the linked group's owner
- store selected restaurant from the internal catalog in MVP
- store and expose completed-event feedback entries authored by joined participants
- enforce event-feedback visibility for open and closed events
- manage closed-event invites
- manage join/leave/accept/decline flows
- enforce capacity and `DecisionAt`
- support host removal of non-host participants before `DecisionAt`
- support moderator/admin participant removal as a safety/support override
- maintain server-controlled event status
- cancel events and automatically complete them after the scheduled time passes
- browse/search events visible to the caller, with Quick Search constrained to active open events that still have available seats

Suggested services:

- `EventService`
- `EventParticipationService`
- `EventInviteService`
- `EventLifecycleService`
- `EventFeedbackService`
- `EventBrowseService`

Architectural note: Events remain the most transaction-sensitive module in the system. Capacity enforcement, invite acceptance, lifecycle evaluation, and automatic completion must stay server-controlled regardless of persistence style.

### 5.5 Groups

Own persistent groups, group membership, ownership rules, discoverability, group-linked event context, group announcements, owner-selected preset wallpapers, and group chat authorization context.

Key responsibilities:

- create groups
- auto-create owner membership on group creation
- join/leave groups
- owner-only group management
- public/private visibility
- owner member-removal actions
- owner-authored group announcements
- automatic group announcements for newly linked group events
- owner-selected preset food wallpaper theme
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
- private-group invites are owner-initiated in MVP
- only the current group owner may associate new events with the group's context
- only the current group owner may write owner announcements or change the group wallpaper theme
- group membership does not replace event participation

### 5.6 Media

Own database-backed image storage plus context-aware access checks for media linked to other modules.

Current responsibilities:

- store image bytes and metadata directly in the relational database
- support one active profile-avatar asset per user
- support report-evidence attachments owned by the reporting user
- support event-feedback photo assets linked to their event and feedback entry
- enforce context-derived read rules for profile avatars, report evidence, and event-feedback photos

Later responsibilities:

- broader event and group media beyond feedback photos
- external object storage abstraction if database-only storage becomes too limiting

Suggested services:

- `MediaService`
- `MediaAccessService` if access rules grow more complex later

### 5.7 Discovery / Budz

Own people discovery, swipes, mutual Budz creation, Budz list retrieval, and discovery filtering.

Key responsibilities:

- user search by username/display name
- limited public profile previews
- swipe / Like / Pass flows
- mutual Budz creation
- list current Budz
- respect privacy, blocking, and moderation restrictions such as `DiscoveryVisibility`
- hide one-sided outbound swipe targets from the actor's search results until the subject decides back

Suggested services:

- `DiscoveryService`
- `SwipeService`
- `BudzService`
- `DiscoveryFilterService`

Canonical MVP rule: one effective directional swipe decision exists per actor/subject pair, and reciprocal effective Like decisions create a Budz connection. Pending Bud-request state is not part of MVP.

### 5.8 Messaging

Own chat threads, messages, and access control across event chat, group chat, support chat, and feature-flagged direct-chat scopes.

Key responsibilities:

- event-linked chat threads
- group-linked chat threads for current members
- support chat threads between a user and admins
- direct 1-on-1 threads for connected Budz when enabled
- text-only message persistence
- message pagination, history retrieval, and admin support-thread listing
- SignalR-based real-time delivery for event, group, support, and enabled direct chat
- scope-specific access enforcement

Rollout priority:

- event chat, group chat, and support chat are part of MVP and should share one messaging core
- direct chat is implemented behind `FeatureFlags:MessagingDirectChatEnabled` and remains disabled by default until launch approval

MVP shared-chat rule:

- event chat access is derived from current event participation state
- group chat access is derived from current active group membership
- support chat access is derived from the supported user id and admin role
- blocking alone does not split or hide a shared event/group chat if both users remain authorized in that shared context
- separation in shared chat requires host/owner/moderator action

Suggested model:

- `ChatThread`
- `ChatMessage`
- `ChatScopeType` = `Event`, `Group`, `Support`, `Direct`
- `ChatScopeId`

Suggested services:

- `MessagingService`
- scope-specific access checks inside `MessagingService`
- `ChatHub` for SignalR connection/auth plumbing

MVP hub contract:

- one shared `ChatHub`
- `JoinScope(scopeType, scopeId)` authorizes and subscribes the caller to an event/group/support/direct channel
- `SendMessage(request)` persists and broadcasts one text message
- `MessageReceived` is the broadcast event name for new chat messages

### 5.9 Notifications

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

### 5.10 Payments

Own simulation-only checkout sessions for event participants.

Key responsibilities:

- create checkout sessions for joined participants on events with a selected restaurant
- calculate simulated subtotals from selected restaurant price tier
- apply an active discount simulation when available
- enforce checkout ownership on completion and cancellation
- keep the feature disabled by default behind `FeatureFlags:PaymentsCheckoutEnabled`

Suggested services:

- `CheckoutSessionService`

The Payments module must not call an external provider or imply real money movement until a future ADR explicitly approves that scope.

### 5.11 Moderation and Audit

Own reports, moderation decisions, scoped restrictions, and audit logging.

Key responsibilities:

- create reports
- review moderation queue
- resolve reports
- apply/remove restrictions
- enforce moderation-related restrictions via service checks
- write immutable audit logs for sensitive actions
- expose authenticated report submission, moderator/admin restriction workflows, and admin-only audit-log review

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

Auth boundary:

- any authenticated user may submit a report
- moderation queue and restriction management require `Moderator` or `Admin`
- audit-log review requires `Admin`

### 7.7 Notification Triggering

Lives in the services that complete the business action, which call `NotificationService` directly.

For this project, direct service calls are better than a full event bus.

### 7.8 Chat Access Rules

Lives in `MessagingService` and the SignalR hub plumbing.

- event chat access derives from current joined event participation
- group chat access derives from current active group membership
- support chat access is limited to the supported user and admins; the support scope id is the supported account id
- direct chat access derives from current connected Budz, current block state, and `FeatureFlags:MessagingDirectChatEnabled`
- real-time transport hub: `ChatHub`

### 7.9 Restaurant Slot and Discount Rules (Feature-Flagged)

Lives in Restaurants.Operations services.

Events still own event state. Restaurants own restaurant operational rules.

Restaurant operation services must enforce active assignment checks before restaurant profile or slot mutation. Slot reservation updates the event's selected restaurant to the slot restaurant and clears cuisine target, but event lifecycle/status remains event-owned. Cancelling a reserved slot cancels the linked event through normal event cancellation behavior.

### 7.10 Checkout Simulation Rules (Feature-Flagged)

Lives in the Payments module.

Checkout sessions are participant-owned simulation records. Creation requires an enabled checkout flag, a current `JOINED` event participant, and a selected restaurant. Subtotal comes from the restaurant price tier, an active discount simulation may reduce the total, and completion/cancellation are owner-only terminal transitions. No external provider, saved payment method, tax, tip, refund, settlement, or webhook behavior is part of this slice.

## 8. Security and Authorization Approach

Core global roles:

- `User`
- `Moderator`
- `Admin`
- `RestaurantAdmin`

Contextual permissions such as host and group owner are derived from records, not stored as permanent global roles.

Use:

- endpoint-level auth and coarse authorization policies
- fine-grained ownership/membership checks inside services

Sensitive data handling rules:

- avoid exposing exact ZIP publicly
- avoid exposing allergies or private availability detail outside allowed workflows; public person cards may include cuisine tags and dietary flags
- keep limited public profile previews for discovery
- respect blocks in both reads and writes
- apply least-privilege access to the database

Audit expectations:

- moderation actions
- restrictions
- group ownership transfer/dissolution when enabled
- restaurant-admin operational overrides when enabled

## 9. Feature Flag Strategy

Recommended flags:

- `FeatureFlags:MessagingDirectChatEnabled`
- `FeatureFlags:MessagingGroupChatEnabled`
- `FeatureFlags:NotificationsPushEnabled`
- `FeatureFlags:RestaurantsOperationsEnabled`
- `FeatureFlags:RestaurantsSlotsEnabled`
- `FeatureFlags:RestaurantsDiscountsEnabled`
- `FeatureFlags:PaymentsCheckoutEnabled`
- `FeatureFlags:DiscoveryExperimentalSuggestionsEnabled`

Clarification:

- `Messaging.GroupChatEnabled` is rollout control for an MVP feature, not a later-scope boundary signal.

Recommended behavior:

- hidden/not-launched feature: prefer `404`
- launched but caller lacks permission: use `403`
- feature exists but operation is invalid: return a normal domain/business error

Flags should be checked at module entry points rather than scattered throughout the stack.

## 10. Persistence Approach

The approved production persistence target for Azure deployment is Azure SQL / SQL Server. SQLite remains the approved local development and automated integration-test provider.

Current architecture requirements:

- `Persistence:Provider=Sqlite` uses the SQLite scripts under `src/TasteBudz.Database/sqlite`
- `Persistence:Provider=SqlServer` uses SQL Server/Azure SQL through EF Core and an externally prepared database
- EF Core plus `TasteBudzDbContext` are used as runtime repository plumbing for both providers
- the backend may auto-initialize or recreate SQLite databases from scripts only in Development and IntegrationTesting
- production SQL Server/Azure SQL schema is manually managed through source-controlled scripts under `src/TasteBudz.Database/sqlserver`
- production startup must validate required SQL Server tables and columns but must not create or migrate schema automatically
- module repository interfaces remain the stable persistence boundary

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
- separate persistence entities and mappings rather than exposing EF entities as API/domain contracts

Required transaction boundaries include:

- event participation and invite-state updates
- event feedback photo asset creation plus feedback-photo link creation
- moderation decision + restriction + audit log
- auth registration/session rotation/account deletion
- admin password reset token issue/use
- Bud connection creation from reciprocal swipe decisions
- later group ownership changes
- enabled slot reservations

Required concurrency protections include:

- multiple users taking the last event seat
- duplicate join attempts
- invite acceptance when one seat remains
- operations at or around `DecisionAt`
- enabled slot reservation contention

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
- feature-flagged restaurant slot/discount rules
- feature-flagged direct chat and checkout rules

### Integration / API Tests

Use integration tests for:

- auth + onboarding
- profile CRUD and privacy behavior
- event create/join/leave/invite flows
- group create/join/leave flows
- discovery and blocking behavior
- event chat, group chat, and support chat endpoints/hub auth
- direct chat and checkout endpoint behavior when flags are enabled
- moderation endpoints and policy enforcement

### Concurrency Tests

These are required.

Focus on:

- last-seat contention
- duplicate joins
- invite-accept contention
- `DecisionAt` edge cases
- enabled slot reservation contention

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
- completed-event feedback with optional database-backed photos
- persistent groups with owner/member model
- basic group management
- event chat
- group chat
- support chat
- in-app notifications for state changes and event updates
- reports, moderation queue, restrictions, and audit logging

### MVP+ / Later

Add when core flows are stable:

- group ownership transfer and dissolution
- richer browse/feed layers
- advanced RSVP/cutoff controls
- push notifications

### MVP++ / Feature-Flagged

Keep disabled by default until explicitly launched:

- direct 1-on-1 messaging
- restaurant-admin accounts and assignment-managed operations
- restaurant slots and slot-linked reservations
- discount threshold simulation
- payment simulation and checkout sessions
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
24. support chat
25. notifications center
26. report creation
27. moderation queue and resolution
28. restrictions
29. audit logging

### Phase 6 - Hardening

30. integration tests across main flows
31. concurrency tests for event participation
32. authorization and blocked-user policy review
33. disabled-feature behavior review
34. architecture cleanup and documentation refresh

### Phase 7 - Feature-Flagged and Later Extensions

35. group ownership transfer and dissolution
35. direct chat behind flag
36. restaurant-admin operations behind flags
37. slots / reservations / discount simulation behind flags
38. payment simulation / checkout behind flag
39. smarter restaurant recommendation logic

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
- clean enough to keep feature-gated direct chat, restaurant operations, and checkout isolated without major redesign
