# Backend Decisions Log

This file is the single repository location for backend architecture, implementation, and policy decisions for TasteBudz.

## How to Use This File

- Add a new entry for each meaningful backend decision.
- Keep entries short and concrete.
- Prefer superseding an older decision with a new entry instead of rewriting history.
- Record the date, status, decision, and consequences.

## Decision Template

```md
## [ADR-###] Short Decision Title

- Date: YYYY-MM-DD
- Status: Proposed | Accepted | Superseded | Deprecated
- Owners: Backend team

### Context
What problem or constraint led to this decision?

### Decision
What was decided?

### Consequences
- Positive impact
- Tradeoff
- Follow-up work
```

## Repository Meta Decisions

## [BD-001] Use This File as the Backend Decision Log

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
The project needs one stable place to document backend decisions so they do not get lost across chat, code comments, or unrelated docs.

### Decision
Store backend decisions in `docs/backend/backend-decisions.md` using short ADR-style entries.

### Consequences
- Backend decisions now have one documented source of truth.
- Future contributors should append entries here instead of creating ad hoc notes.

## [BD-002] Treat Messaging and Moderation as a High-Complexity Area

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Messaging becomes significantly harder when combined with blocking, moderation, access control, and notifications.

### Decision
Treat messaging plus moderation as a scope area that requires active complexity control during design and implementation.

### Consequences
- The team should review this area regularly during implementation.
- If schedule pressure rises, simplify message features before compromising correctness.

## Architecture Decision Records

## [ADR-001] Budz Creation Uses Mutual Like Only

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Older wording implied manual Bud requests in MVP.

### Decision
In MVP, Budz are created only when two users have reciprocal effective Like decisions.

### Consequences
- MVP does not need pending Bud-request workflow or UI.
- Later manual-request flow, if added, must be documented separately.

## [ADR-002] MVP Includes Event Chat and Group Chat

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Earlier docs treated event chat as MVP and group chat as later.

### Decision
MVP includes both event chat and group chat. Direct 1-on-1 chat remains later and feature-flagged.

### Consequences
- Messaging scope is broader in MVP.
- Both chat types should share one messaging core.

## [ADR-003] MVP Restaurant Source Is the Internal Catalog

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
External restaurant APIs add cost, rate limits, and unpredictable data quality when they sit in the user-facing browse/search path.

### Decision
MVP uses the internal restaurant catalog as the source of truth for restaurant selection. The catalog may be seeded from SQL scripts, may be populated through an admin-only OpenStreetMap/Overpass import, and may geocode admin-maintained restaurant addresses through an external provider during catalog saves, but user-facing restaurant browse/search continues to read from the local catalog instead of calling an external provider live.

### Consequences
- Testing is simpler because user-facing restaurant behavior is still local catalog behavior.
- External IDs should be provider-qualified, such as `osm:<id>`, so clients do not confuse OpenStreetMap identifiers with Google Place IDs.
- Admin-only geocoding remains acceptable because the result is persisted back into the local catalog instead of becoming a runtime dependency for user browse/search.
- External live search remains optional later work.

## [ADR-004] Notifications Are In-App Only in MVP

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Email/push/reminder jobs add infrastructure and scheduling complexity.

### Decision
MVP uses persisted in-app notifications only.

### Consequences
- Lower infrastructure complexity.
- Reminder jobs and external delivery channels stay out of the critical path.

## [ADR-005] MVP Restaurant Selection Uses Search and List UX

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Map-first restaurant selection increases implementation cost and external dependency pressure.

### Decision
MVP restaurant selection uses search and list over the internal catalog. Map presentation is optional only when it falls out naturally from stored coordinates.

### Consequences
- Simpler UI and lower integration risk.
- The backend should prioritize reliable search/filter endpoints.

## [ADR-006] Event and Group Chat Use SignalR

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
The approved MVP direction favors real-time coordination for chat.

### Decision
Event chat and group chat use SignalR/WebSockets for real-time delivery, with paged history retrieval over HTTP as needed.

### Consequences
- Better UX for chat participants.
- Higher implementation complexity than pure polling, so message features must stay minimal.

## [ADR-007] Event Host Counts Toward Capacity

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Capacity becomes ambiguous if the host is not counted.

### Decision
The event host counts toward event capacity and is represented as a joined participant.

### Consequences
- Capacity math stays simple.
- Event creation must automatically create the host participant record.

## [ADR-008] Event Capacity Range Is 2 to 8

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
The product is intended for small-group social dining rather than large meetup-style events.

### Decision
MVP event capacity is between 2 and 8 participants inclusive.

### Consequences
- Event sizing stays aligned to the social-dining focus.
- Validation and DB constraints should enforce this range.

## [ADR-009] Open Events Use Instant Join

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Host approval workflows add extra states and friction to open discovery.

### Decision
Open events allow instant join when seats are available. Closed events still rely on invite acceptance.

### Consequences
- Faster user flow for open events.
- Capacity enforcement becomes more important under concurrency.

## [ADR-010] Open vs Closed Is the Event Visibility Model

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
The product needs both discoverable and invite-only planning flows.

### Decision
Use event type as the canonical visibility model in MVP: Open events are discoverable/joinable and Closed events are invite-only.

### Consequences
- No second visibility model is needed for MVP events.
- Browse/search only needs to surface open events.

## [ADR-011] Groups Have No Hard Member Cap in MVP

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Groups represent recurring social circles and do not need the same cap as an event.

### Decision
Groups do not have a hard maximum member cap in MVP.

### Consequences
- Group membership and event participation stay distinct.
- Event capacity remains the mechanism that limits an actual dining plan.

## [ADR-012] Event Invitations Do Not Reserve Seats

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Seat reservation on invite creates wasted-capacity edge cases.

### Decision
Inviting a user to an event does not reserve a seat. Capacity is consumed only when the user actually joins/accepts.

### Consequences
- Accept/join operations must be transactional.
- Event invite acceptance can fail if the event is already full.

## [ADR-013] Leaving an Event Frees the Seat

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Locked-seat behavior is unnecessary for the MVP product.

### Decision
When a participant leaves an event, the seat becomes available again.

### Consequences
- History should be preserved while capacity reopens.
- Status recalculation must happen after leave/remove actions.

## [ADR-014] Blocking Separates Live Shared Contexts

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Users expect blocking to end direct social contact and live shared interaction, while completed event history still needs to remain available for MVP history, feedback, and moderation context.

### Decision
Blocking prevents new direct interaction paths such as messaging, invitations, and new Bud interactions. Creating a block also removes the active Budz connection, makes the blocker leave shared active groups and non-completed joined events, and removes the blocked user instead when the blocker is the group owner or event host. Completed shared events are preserved, but completed-event chat history filters messages between the blocked pair.

### Consequences
- Block cleanup must preserve historical records instead of physically deleting memberships, participations, messages, reports, or moderation actions.
- Unblocking does not restore Budz, group membership, or event participation automatically.
- Blocking filters must apply consistently in people discovery, private-contact paths, live event/group browse and join paths, and completed-event chat history.

## [ADR-015] Event Cancellation Is Status-Based

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Deleting cancelled events destroys useful history and makes notifications harder to reason about.

### Decision
Cancelling an event sets status to `CANCELLED` instead of deleting the event.

### Consequences
- Audit/history remains intact.
- Event detail/history UI can stay consistent.

## [ADR-016] Hosts May Edit Event Details Before Completion or Cancellation

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Some event details need to remain adjustable before the event is locked/finalized.

### Decision
Hosts may edit event details before the event is completed or cancelled. Material changes should trigger participant notifications.

### Consequences
- The API needs an explicit event-update contract.
- The backend must define what counts as a material change.

## [ADR-017] Leaving an Event Revokes Event-Chat Access

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Event chat should stay scoped to current participants.

### Decision
When a user leaves an event, event-chat access is revoked immediately.

### Consequences
- Event-chat authorization must derive from current participant state.
- Historical message retention is allowed, but live access is removed.

## [ADR-018] Events Auto-Complete by Time

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Manual completion creates extra host work and is easy to forget.

### Decision
Confirmed events automatically transition to `COMPLETED` after the scheduled event time passes according to server policy.

### Consequences
- Lifecycle processing needs a time-based completion rule.
- The event API should treat `COMPLETED` as terminal.

## [ADR-019] Group Ownership, Membership, and Invites Are Distinct Canonical Concepts

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Ownership, membership, and invitation can drift if they all act like competing sources of truth.

### Decision
`Group.OwnerUserId` is the canonical ownership source. `GroupMember` is the canonical membership record. `GroupInvite` is workflow-only. Group creation auto-creates the owner as an active member.

### Consequences
- Group rules are easier to reason about.
- Private-group invite acceptance remains straightforward.

## [ADR-020] Event Participation Uses One Effective Participant Record

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Hosts, invitees, joiners, removals, and re-entry all revolve around the same event/user relationship.

### Decision
`EventParticipant` is the canonical participation record. The host is auto-created in `JOINED`, and join/accept/reinvite/restore flows update or reactivate the same effective record instead of creating duplicates.

### Consequences
- Transaction logic is cleaner.
- Capacity, chat access, and lifecycle rules all reference the same effective participant state.

## [ADR-021] Group-Linked Event Association Is Owner-Only

- Date: 2026-03-09
- Status: Accepted
- Owners: Backend team

### Context
Allowing any active group member to attach an event to group context makes ownership and group-facing event history harder to reason about.

### Decision
In MVP, only the current group owner may create or update an event with that group's `GroupId`.

### Consequences
- Group-linked events remain an owner-managed context signal.
- Group membership stays distinct from event-host authority and event participation.

## [ADR-022] Discovery Restrictions Use the `DiscoveryVisibility` Scope

- Date: 2026-03-09
- Status: Accepted
- Owners: Backend team

### Context
People discovery must respect moderation restrictions, but earlier docs only listed chat and event scopes explicitly.

### Decision
MVP restriction scopes include `DiscoveryVisibility` so moderators can hide a user from discovery/search without changing broader account status.

### Consequences
- Discovery and swipe flows must filter out users with an active `DiscoveryVisibility` restriction.
- Moderation stays scoped and reversible instead of overloading account lifecycle state.

## [BD-003] Store Backend Testing Strategy in a Dedicated Document

- Date: 2026-03-08
- Status: Accepted
- Owners: Backend team

### Context
The repository now has detailed requirements, architecture, domain, and API documents, but the backend testing approach also needs a stable written source of truth. Testing decisions are especially important because backend, database, and frontend work are split across teammates.

### Decision
Store the backend testing strategy in `docs/backend/testing-strategy.md` and treat it as the authoritative source for backend validation approach, test layers, coverage priorities, and completion criteria.

### Consequences
- Contributors now have one stable place to look for backend testing expectations.
- Test-related guidance can evolve without overloading architecture or API documents.
- Future test strategy changes should be reconciled with requirements, architecture, domain, API, and accepted ADRs.

## [ADR-023] SQLite Is the Approved MVP Runtime Persistence Path

- Date: 2026-03-26
- Status: Accepted historically; superseded for production by ADR-027
- Owners: Backend team

### Context
The implemented MVP backend now needs a real relational runtime store for currently shipped modules without expanding scope into multi-provider persistence or migration-heavy infrastructure. Earlier repository process docs still pointed at SQL Server / Azure SQL, while runtime code and tests needed a simpler approved path for capstone delivery.

### Decision
Use SQLite as the only approved runtime persistence target for the implemented MVP backend modules. Keep the canonical schema and seed data in source-controlled SQLite SQL scripts under `src/TasteBudz.Database/`, and use EF Core repository implementations plus a shared `TasteBudzDbContext` only as the runtime access layer against that schema. Development and integration-test environments may initialize or recreate databases from those canonical SQL scripts; other environments must use an already prepared database.

### Consequences
- Runtime persistence is now relational and durable for the implemented backend slices without introducing a second provider.
- Module repository interfaces remain the persistence boundary, and service-layer business rules stay unchanged at the HTTP contract level.
- A generated `.sqlite` database file is a disposable artifact, not a schema authority.
- ADR-027 later changes provider strategy for Azure production. Local development and automated tests remain aligned to SQLite.

## [ADR-024] MVP Media Assets Use Database-Backed Image Storage

- Date: 2026-04-10
- Status: Accepted
- Owners: Backend team

### Context
The next approved backend slice needs one universal media-storage path that works immediately for profile avatars and moderation report evidence without adding external object storage infrastructure or provider-specific operational complexity.

### Decision
Store launched MVP media assets directly in the relational database as image bytes plus metadata. Each media record is owned by one user and linked to exactly one bounded context. The initial launched contexts are profile-avatar media and moderation-report evidence attachments.

### Consequences
- Local development and automated tests stay self-contained because media storage does not depend on external services.
- Media access must be enforced from the owning context instead of exposing database records directly.
- If Azure or production scale later makes database-only media storage too costly, the storage implementation can move behind the same module boundary with a new ADR.

## [ADR-025] Restaurant Operations Active MVP Launch With Kill-Switch Flags

- Date: 2026-04-11
- Status: Accepted
- Owners: Backend team

### Context
Restaurant-admin assignments, slots, reservations, and discount simulation were implemented as a feature-flagged restaurant-operations slice. They are now promoted into the active MVP/demo flow so restaurant partners can fill tables and event hosts can reserve concrete restaurant/time/capacity anchors.

### Decision
Run restaurant operations by default while retaining `FeatureFlags:RestaurantsOperationsEnabled`, `FeatureFlags:RestaurantsSlotsEnabled`, and `FeatureFlags:RestaurantsDiscountsEnabled` as kill switches. Global `Admin` users are the only actors that can grant or revoke `RestaurantAdminAssignment` records. Assignment grant adds the coarse `RestaurantAdmin` role, and revoke removes that role only after the user has no active restaurant-admin assignments left. Restaurant admins may mutate only restaurants for which they have an active assignment. Slot reservation is host-owned, requires an active event and open slot, enforces event time/capacity fit, and updates the event to use the slot restaurant while clearing cuisine target. Cancelling a reserved slot cancels the linked event through normal event-cancellation behavior. Discount handling is simulation-only: joined participants count toward the threshold, recalculation continues through cutoff, the final active/inactive result freezes after cutoff, and active discounts use the slot's configured whole-number discount percentage.

### Consequences
- Default MVP/demo behavior includes restaurant-admin assignment, restaurant-admin slot management, host slot reservation, reserved-slot cancellation, and discount simulation.
- If restaurant operation flags are explicitly disabled, affected endpoints return `404`.
- Active endpoints return normal `401`/`403` authorization results when the caller lacks permission.
- Restaurant operations add schema and service code without treating generated `.sqlite` files as schema authority.
- Payment simulation, checkout state, and payment-side effects remain out of scope for restaurant operations itself; simulation-only checkout is separately governed by ADR-026.

## [ADR-026] Direct Chat and Checkout Launch Behind Explicit Feature Flags

- Date: 2026-04-12
- Status: Accepted
- Owners: Backend team

### Context
Direct 1-on-1 chat and payment simulation/checkout were previously treated as later-scope features. They are now approved as feature-flagged backend slices, but neither should change the default MVP runtime behavior until intentionally enabled.

### Decision
Implement direct 1-on-1 chat behind `FeatureFlags:MessagingDirectChatEnabled`, disabled by default. Direct chat is allowed only between current connected Budz, uses the existing `ChatThread`/`ChatMessage` messaging core with `ChatScopeType.Direct`, respects blocking and active `ChatSend` restrictions, and uses the same SignalR plus paged-history model as event and group chat.

Implement simulation-only checkout behind `FeatureFlags:PaymentsCheckoutEnabled`, disabled by default. Checkout sessions belong to the requesting event participant, require the user to be a current `JOINED` participant on an event with a selected restaurant, derive simulated subtotal from restaurant price tier, apply an active discount simulation when available, and support `Pending`, `Completed`, and `Cancelled` states. Checkout must not call an external payment provider or imply real money movement.

### Consequences
- Default MVP behavior remains unchanged because disabled direct-chat and checkout endpoints return `404`.
- Direct chat and checkout add schema, service, API, and test coverage while continuing to use the SQLite schema scripts as the persistence authority.
- Blocking, Budz state, moderation restrictions, event participation, selected-restaurant state, and feature flags remain server-owned policy checks.
- Real payment provider integration, saved payment methods, tax, tips, settlement, refunds, and webhooks remain out of scope until a future ADR approves them.

## [ADR-027] Azure Production Uses SQL Server While Local Development Uses SQLite

- Date: 2026-04-12
- Status: Accepted
- Owners: Backend team

### Context
The MVP backend was previously standardized on SQLite only for runtime persistence in ADR-023. Azure App Service deployment now needs one release batch containing the MVC app, backend API controllers, SignalR hub, backend services, EF Core persistence wiring, and manually applied database scripts. Azure SQL is the production database target, but local development and automated tests still benefit from SQLite's self-contained workflow.

### Decision
Use one deployable ASP.NET Core web host for the modular monolith, with `TasteBudz.Web.Mvc` as the deployable host and `TasteBudz.Backend` as a referenced backend module. Runtime persistence is selected by `Persistence:Provider`:

- `Sqlite` for local development and integration tests
- `SqlServer` for Azure SQL / SQL Server production

Keep `TasteBudzDbContext` and module repository boundaries as the runtime persistence boundary. SQLite may still auto-initialize only in `Development` and `IntegrationTesting`; SQL Server/Azure SQL schema changes are applied manually from source-controlled scripts under `src/TasteBudz.Database/sqlserver`. The application validates required SQL Server tables and columns at startup but does not create or migrate production schema.

### Consequences
- ADR-023 remains the historical SQLite-only MVP decision, but this ADR supersedes it for production Azure deployment.
- Local development and integration tests stay simple and deterministic with SQLite.
- Development and integration-test SQLite startup may apply explicit additive compatibility updates to older local database files before required-schema validation.
- Azure production can use Azure SQL by configuration without changing application code.
- Database deployment is a manual release step, not application startup behavior.
- Repository and service boundaries remain unchanged; persistence entities still must not be exposed as API contracts.

## [ADR-028] Admin Password Reset Uses One-Time Tokens

- Date: 2026-04-16
- Status: Accepted
- Owners: Backend team

### Context
Admins need a support-safe way to help users recover account access without learning, setting, or transmitting the user's final password directly. The flow must keep password creation user-owned while still giving admins a bounded recovery tool.

### Decision
Admins may issue one-time password reset tokens for active users. The raw token is returned only when created, stored only as a hash, expires after a short window, and revokes any previous unused reset tokens for the same user. The user completes the reset through an anonymous password-reset endpoint that validates the token, writes the new password hash, marks the token used, and revokes existing sessions.

### Consequences
- Admins can start recovery but cannot choose the user's new password.
- Password reset completion is still user-entered and token-bound.
- Existing sessions are invalidated after a successful reset.
- Reset token issue/use is a transaction-sensitive auth workflow and should be tested at service and API levels.

## [ADR-029] Support Chat Uses a Messaging Support Scope

- Date: 2026-04-16
- Status: Accepted
- Owners: Backend team

### Context
Users need a simple Help/Support entry point to message admins. The project already has scoped chat infrastructure for event, group, and direct chat, so a separate support messaging subsystem would add avoidable complexity.

### Decision
Represent user-to-admin support chat as `ChatScopeType.Support` in the existing messaging core. The support scope id is the supported user's account id. The supported user and admins may read the thread. The supported user may send support messages subject to normal active-account and `ChatSend` restrictions; admins may reply through admin support endpoints.

### Consequences
- Support chat reuses existing `ChatThread`, `ChatMessage`, history, and SignalR behavior.
- Support authorization remains explicit and service-owned.
- Admin support thread listing is an admin-only view over support-scoped threads.
- Support chat is MVP scope and not feature-flagged as later direct chat is.

## [ADR-030] People Search Hides One-Sided Outbound Swipe Decisions

- Date: 2026-04-16
- Status: Accepted
- Owners: Backend team

### Context
After a user makes a swipe decision about another user, showing that same person again in people search creates repeated-decision noise until the other person has had a chance to decide back.

### Decision
People search excludes users for whom the current actor has an effective outbound swipe decision and the subject has not yet recorded a reciprocal effective decision about the actor. Once the subject decides back, the pair may be governed by the normal reciprocal Like/Budz result or by the latest directional decisions.

### Consequences
- Search results avoid one-sided repeat exposure after a swipe.
- The rule remains query-time discovery behavior and does not introduce a pending Bud-request state.
- Reciprocal effective Like decisions still create Budz directly in MVP.

## [ADR-031] Event Feedback Is Completed-Only and Participant-Authored

- Date: 2026-04-17
- Status: Accepted
- Owners: Backend team

### Context
Participants need a lightweight way to rate and describe their event experience after an event is over. Existing `RestaurantReviews` are restaurant-facing and should not be reused because this feature evaluates the event experience, host/participant context, and related behavior rather than the restaurant itself.

### Decision
Events own event feedback. Each joined participant may create or update one feedback entry per completed event, enforced by a unique `(EventId, AuthorUserId)` rule. Feedback requires a 1-5 rating and non-empty trimmed text capped at 1000 characters. Feedback is rejected for active, pending, confirmed, or cancelled events, and deleting an entire feedback entry is out of scope for the first slice.

Feedback visibility follows event visibility. Feedback for Open events is readable by authenticated users who can view the event. Feedback for Closed events is readable only by the host, joined participants, and Moderator/Admin roles for moderation review. Feedback photos are optional, capped at four per feedback entry, stored as database-backed `MediaAsset` records linked to the event and feedback entry, and reuse the same 2 MB image type validation as other media uploads.

Feedback reports use the existing moderation report flow with `TargetType=User`, the feedback author's user id as the target, and the event/user references as related context.

### Consequences
- Event feedback stays separate from restaurant reviews and does not affect event lifecycle, capacity, invites, chat, notifications, or browse/search ranking.
- The Events module owns feedback policy and repositories; the Media module stores bytes and delegates event-feedback image authorization to Events.
- Feedback image access must be checked through the same visibility rules as feedback listing.
- External object storage, chat attachments, realtime updates, and feedback notifications remain out of scope for this slice.

## [ADR-032] Group Announcements and Wallpapers Stay Owner-Managed in MVP

- Date: 2026-04-24
- Status: Accepted
- Owners: Backend team

### Context

Groups need a more complete hub experience with members, chat, event history, owner posts, automatic event updates, and lightweight visual personalization. Uploaded custom backgrounds would require additional moderation, media lifecycle, and storage policy work.

### Decision

Groups store a preset `GroupWallpaperTheme` selected by the current group owner. Group announcements are first-class group records, not pinned chat messages. Owner posts require current group ownership. Creating a group-linked event writes an `EventCreated` group announcement. Announcement visibility follows group detail visibility.

### Consequences

- The Groups module owns announcement persistence and owner authorization.
- The Events module may write a group event announcement as part of event creation after group-link authorization succeeds.
- Wallpaper customization is limited to enum-backed presets in MVP; uploaded group backgrounds can reuse media infrastructure in a later slice after moderation and lifecycle rules are defined.

## [ADR-033] Group-Linked Event Type Follows Group Visibility

- Date: 2026-04-24
- Status: Accepted
- Owners: Backend team

### Context

Group-linked events use `GroupId` as owner-managed context, but allowing a private group to create a public event or a public group to create an invite-only group event makes group visibility expectations unclear.

### Decision

When an event is linked to a group, event type is derived from group visibility. Public groups may create only Open linked events. Private groups may create only Closed linked events. Standalone events with no `GroupId` still let the host choose Open or Closed.

### Consequences

- MVC should lock the event type selector for group event creation.
- Backend event creation and update must reject group/event visibility mismatches.
- Event invitation behavior remains owned by the event host; private group membership does not automatically create event participation.

## [ADR-034] Event Hosts Can Invite Users to Open or Closed Events

- Date: 2026-04-24
- Status: Accepted
- Owners: Backend team

### Context

Open events are discoverable and directly joinable, but hosts still need a directed way to ask specific Budz or group members to attend. Restricting the invite workflow to Closed events makes public event coordination feel incomplete.

### Decision

Event invites are available to the current host for active Open and Closed events. Invites create or update `EventParticipant` records in `INVITED` state and notify invitees. Invites do not reserve seats; accepting an invite still runs the same capacity and `DecisionAt` checks as direct join/accept flows.

### Consequences

- MVC should show host invite controls for active Open and Closed events.
- Backend invite policy is based on host ownership, active lifecycle state, blocking, capacity-on-acceptance, and `DecisionAt`, not on event type alone.
- Open event browse and direct join remain unchanged.

## [ADR-035] Event Browse Shows Visible Event History While Quick Search Stays Joinable-Only

- Date: 2026-04-25
- Status: Accepted
- Owners: Backend team

### Context

The Events tab needs to support status review and filtering for completed, cancelled, full, group-linked, and ordinary events instead of acting only as an upcoming open-event discovery list. At the same time, Quick Search should remain a joinable-event shortcut and must not recommend events that are full or terminal.

### Decision

The normal event browse endpoint returns all events the caller is allowed to view, including Open events and Closed events where visibility is granted by host or participant/invite context. It supports status filtering plus group-linked versus ordinary filtering. Quick Search is represented by `recommended=true` and is constrained to Open events in `OPEN` status where joined participants are still below capacity.

### Consequences

- The Events tab can show full, completed, cancelled, group-linked, and ordinary event history without weakening Closed-event privacy.
- Quick Search remains focused on active Open events with available seats.
- Clients must not treat `recommended=true` as a generic browse mode for historical or full events.

## [ADR-036] OpenStreetMap Restaurant Import Is Preview-First and Duplicate-Safe

- Date: 2026-04-27
- Status: Accepted
- Owners: Backend team

### Context

Large OpenStreetMap imports can add hundreds or thousands of restaurants at once. A one-click import without geography context or duplicate review makes the demo catalog harder to trust and can slow admin workflows.

### Decision

Admin OpenStreetMap import uses a preview-first flow. Admins choose a Cincinnati-focused ZIP/radius region or explicit manual bounds, review candidates, and commit selected external ids. The backend re-runs the same Overpass query during commit and imports only selected candidates that are still non-duplicates. Duplicate detection checks provider-qualified and legacy OpenStreetMap ids, exact normalized name/address matches, exact normalized name within 0.1 miles, and simplified same-ZIP name matches within 0.25 miles. Duplicate candidates are skipped; merge, overwrite, and override behavior remain out of scope.

### Consequences

- Admins can review import scope and candidate quality before adding records.
- User-facing restaurant browse remains local catalog-backed and does not call OpenStreetMap live.
- Large catalogs remain manageable through paged admin search instead of full-page rendering.
- Future duplicate override or merge tooling must be approved separately.

## [ADR-037] Full Soft Bans Block Authentication and Admin Physical Delete Is Guarded

- Date: 2026-05-01
- Status: Accepted
- Owners: Backend team

### Context

The moderation MVP originally modeled bans as scoped `UserRestriction` rows instead of account lifecycle changes. That kept moderation reversible, but active full-soft-banned users could still authenticate and could still appear in some active user-facing social lists. Admins also need a deliberately confirmed way to remove selected test/demo accounts without weakening historical integrity for real participation, messaging, moderation, audit, or payment records.

### Decision

A full MVP soft ban is the active set of `DiscoveryVisibility`, `ChatSend`, `EventJoin`, and `EventCreate` restrictions for the same user. Applying that ban revokes all sessions. While the full ban is active, login, refresh, bearer-token authentication, and SignalR bearer authentication are rejected. Regular user-facing people surfaces hide that account where the application is presenting active social participants or contacts, including discovery, Budz, group member lists, and event participant lists. Staff moderation search/detail surfaces still show the account and restriction history for traceability.

Admins may soft-delete another user's account through admin user management. Admins may also permanently delete a user only when all of the following are true: the admin is not deleting themself, the account is already soft-deleted, the request confirmation is exactly `delete`, and the account has no protected historical dependencies. Protected dependencies include hosted events, event participation, event feedback, checkout sessions, owned groups, group announcements, chat messages, moderation reports/actions issued by the user, issued restrictions, actor audit entries, issued password reset artifacts, and context-linked media. Dependency-free permanent deletion may remove the account's private/profile/auth rows and non-historical relationship rows.

### Consequences

- Full soft-ban enforcement is centralized in auth/session boundaries instead of depending only on individual feature checks.
- Account lifecycle (`UserAccount.Status`) remains separate from scoped moderation restrictions, but full-soft-ban status has an authentication-level effect while active.
- Physical deletion remains an admin-only exception for dependency-free soft-deleted accounts, not a general cascade-delete model.
- Staff tools must continue to preserve enough moderation traceability to review bans, deletion attempts, and historical safety decisions.
