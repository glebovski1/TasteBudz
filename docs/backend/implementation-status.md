# TasteBudz Backend Implementation Status

This document tracks the current backend implementation state.

It is a progress tracker, not a source of product or architecture truth.
Use the primary backend documents for requirements, architecture, domain rules, API contracts, and testing policy.

Last verified: 2026-04-26

## 1. Overall State

Current overall state:

- foundation is implemented
- the currently implemented MVP backend slices are `Backend-complete`
- restaurant operations are active in the MVP/demo flow with feature flags retained as kill switches
- feature-flagged direct chat and checkout simulation are implemented but disabled by default
- remaining later backend slices remain intentionally out of scope

Using the definitions in `docs/backend/implementation-approach.md`:

- `Backend-logic ready` means service logic and API behavior are implemented and tested
- `Backend-complete` means real persistence and required concurrency-sensitive behavior are also proven

Current practical assessment:

- Auth and Access: `Backend-complete`
- Profiles: `Backend-complete`
- Restaurants: `Backend-complete`
- Restaurant Operations: `Backend-complete`, active MVP/demo slice with kill-switch flags
- Events: `Backend-complete`
- Groups: `Backend-complete`
- Discovery / Budz: `Backend-complete`
- Media: `Backend-complete`
- Notifications: `Backend-complete`
- Messaging: `Backend-complete`, with support chat implemented and direct chat implemented as a disabled-by-default feature-flagged slice
- Payments / Checkout: feature-flagged implemented slice, disabled by default, simulation-only
- Moderation and Audit: `Backend-complete`
- Real SQLite persistence: implemented for the current MVP slice surface

## 2. Implemented Runtime Foundation

Implemented foundation pieces:

- ASP.NET Core controller-based API host
- centralized ProblemDetails-style exception handling
- custom bearer-token authentication backed by stored sessions
- modular service and repository structure
- SQLite-backed repository implementations for current modules
- shared `TasteBudzDbContext`, SQLite transaction runner, and startup bootstrap validation
- canonical SQLite schema and seed scripts in `src/TasteBudz.Database`
- deterministic seeded restaurant catalog
- unit and integration test projects with `WebApplicationFactory<Program>`
- integration-test factory that recreates temporary SQLite databases from canonical SQL assets

Current runtime persistence note:

- the app now runs on SQLite for the implemented backend modules
- schema and seed authority live in the repository SQL scripts, not in a checked-in database binary
- Development and IntegrationTesting may initialize SQLite databases from canonical SQL scripts when configured

## 3. Module Status

| Module | Current state | Notes |
|---|---|---|
| Auth and Access | Implemented slice | Register, login, refresh, logout, current-user auth pipeline, role-aware auth, account deletion, anonymous password reset requests, admin review/closure, and admin-issued password reset tokens |
| Profiles | Implemented slice | Onboarding status, profile update/read, public profile note, preferences, availability, privacy, blocks, dashboard summaries |
| Restaurants | Implemented slice | Browse, detail, deterministic suggestions, seeded catalog |
| Restaurant Operations | Implemented slice | Admin-managed restaurant admin assignments, managed restaurant profile edits, slot CRUD/cancel, event-host slot reservations, reserved-slot cancellation, and per-slot discount percentage simulation; active by default with kill-switch flags |
| Events | Implemented slice | Browse, create, detail, update, participants, join, leave/accept/decline, invite, cancel, lifecycle sync, owner-only group link, restriction checks |
| Groups | Implemented slice | Browse/search, create/detail/update, join/leave, owner removal, private invites, linked-event listing |
| Discovery / Budz | Implemented slice | Search, swipe candidates, Like/Pass decisions, one-sided outbound swipe search filtering, reciprocal Budz creation, privacy/block/restriction filtering |
| Media | Implemented slice | Database-backed image storage, profile-avatar upload/replacement, report-evidence attachments, context-based media access |
| Notifications | Implemented slice | In-app notification center list/read API over existing workflow notifications |
| Messaging | Implemented slice | Shared SignalR chat hub plus paged event/group/support message history with scope-derived auth; feature-flagged direct chat for Budz-only 1-on-1 messages |
| Payments / Checkout | Feature-flagged implemented slice | Simulation-only checkout sessions for joined event participants; disabled by default |
| Moderation and Audit | Implemented slice | Report submission, moderation queue/detail/resolve, scoped restrictions, admin audit-log query |

## 4. Implemented Endpoint Surface

Implemented controller surface as of 2026-04-26:

- `/api/v1/auth/*`
- `/api/v1/auth/password-reset-requests`
- `/api/v1/auth/password-reset`
- `/api/v1/admin/users/password-reset-requests`
- `/api/v1/admin/users/password-reset-tokens`
- `/api/v1/onboarding/status`
- `/api/v1/profiles/me`
- `/api/v1/profiles/me/avatar`
- `/api/v1/preferences/me`
- `/api/v1/availability/recurring`
- `/api/v1/availability/one-off`
- `/api/v1/privacy-settings/me`
- `/api/v1/blocks`
- `/api/v1/me/dashboard`
- `/api/v1/me/events`
- `/api/v1/me/groups`
- `/api/v1/me/event-invites`
- `/api/v1/account/deletion`
- `/api/v1/restaurants`
- `/api/v1/restaurants/{restaurantId}`
- `/api/v1/restaurants/{restaurantId}/slots`
- `/api/v1/restaurants/suggestions`
- `/api/v1/admin/restaurants/{restaurantId}/admin-assignments`
- `/api/v1/events`
- `/api/v1/events/{eventId}`
- `/api/v1/events/{eventId}/slot-reservations`
- `/api/v1/events/{eventId}/checkout-sessions` (feature-flagged)
- `/api/v1/events/{eventId}/participants`
- `/api/v1/events/{eventId}/participants/me`
- `/api/v1/events/{eventId}/participants/{userId}/removal`
- `/api/v1/events/{eventId}/invites`
- `/api/v1/events/{eventId}/cancellation`
- `/api/v1/events/{eventId}/messages`
- `/api/v1/groups`
- `/api/v1/groups/{groupId}`
- `/api/v1/groups/{groupId}/events`
- `/api/v1/groups/{groupId}/members`
- `/api/v1/groups/{groupId}/members/me`
- `/api/v1/groups/{groupId}/members/{userId}/removal`
- `/api/v1/groups/{groupId}/invites`
- `/api/v1/groups/invites/{inviteId}`
- `/api/v1/groups/{groupId}/messages`
- `/api/v1/support/messages`
- `/api/v1/admin/support/threads`
- `/api/v1/admin/support/threads/{userId}/messages`
- `/api/v1/direct-chats` (feature-flagged)
- `/api/v1/direct-chats/{directChatId}/messages` (feature-flagged)
- `/api/v1/checkout-sessions/{checkoutSessionId}/completion` (feature-flagged)
- `/api/v1/checkout-sessions/{checkoutSessionId}/cancellation` (feature-flagged)
- `/api/v1/discovery/people`
- `/api/v1/discovery/swipe-candidates`
- `/api/v1/discovery/swipes`
- `/api/v1/budz`
- `/api/v1/notifications`
- `/api/v1/notifications/{notificationId}`
- `/api/v1/reports`
- `/api/v1/reports/{reportId}/attachments`
- `/api/v1/media/{mediaAssetId}`
- `/api/v1/moderation/reports`
- `/api/v1/moderation/reports/{reportId}`
- `/api/v1/moderation/restrictions`
- `/api/v1/moderation/restrictions/{restrictionId}`
- `/api/v1/audit-logs`
- `/api/v1/admin/users/password-reset-tokens`
- `/api/v1/restaurant-admin/restaurants`
- `/api/v1/restaurant-admin/restaurants/{restaurantId}`
- `/api/v1/restaurant-admin/restaurants/{restaurantId}/slots`
- `/api/v1/restaurant-admin/slots/{slotId}`
- `/api/v1/restaurant-admin/slots/{slotId}/cancellation`
- `/hubs/chat`

Not yet implemented from later/feature-flagged API shape:

- group ownership transfer and dissolution endpoints
- feed endpoint and richer feed/search projection behavior

## 5. Test Status

Current automated test status as of 2026-04-26:

- 118 backend unit tests
- 76 backend integration tests
- 75 MVC integration tests
- 269 passing solution tests total

Current covered areas:

- password hashing
- auth registration, login, refresh, logout, duplicate-credential handling, anonymous password reset requests, admin-issued password reset, and protected endpoint access
- profile update workflows
- profile-avatar upload/replacement and media retrieval behavior
- recurring and one-off availability edge cases
- blocks and dashboard behavior
- restaurant browse and suggestion behavior
- restaurant detail/not-found behavior
- event host auto-join behavior
- closed-event invite acceptance capacity rule
- event capacity validation
- event last-seat concurrency guard
- event final-seat race coverage against SQLite
- closed-event invite-acceptance race coverage against SQLite
- event lifecycle behavior when requests arrive after `DecisionAt`
- event group-link authorization
- moderator participant removal after `DecisionAt`
- group create/join/invite/detail workflows
- discovery search/swipe/Budz workflows, including one-sided outbound swipe search filtering
- notification-center read/update behavior
- event chat, group chat, support chat, and feature-flagged direct chat authorization plus hub delivery
- direct chat service, block enforcement, and MVC route behavior
- report, restriction, role-enforcement, and audit-log workflows
- report-evidence attachment upload/list/download authorization
- restaurant-admin assignment grant/revoke role behavior
- restaurant-admin assignment authorization checks
- restaurant operation default-on and explicit-disabled flag behavior
- restaurant slot validation, per-slot discount percentage, and cancellation behavior
- event-host slot reservation invariants and same-slot conflict behavior
- MVC restaurant picker slot filtering/listing and create-then-reserve slot orchestration
- discount activation, configured percentage projection, and cutoff freeze behavior
- checkout simulation creation, joined-participant and selected-restaurant requirements, owner-only completion, per-slot discount application, completion/cancellation, terminal-state conflicts, disabled-flag behavior, and MVC service route coverage
- ProblemDetails behavior for selected failure cases
- persistence-backed API and workflow coverage for the implemented module set via temporary SQLite databases rebuilt from canonical SQL assets

Important testing gaps still open:

- group ownership transfer/dissolution and richer feed/search projection behavior remain intentionally unimplemented and untested at runtime
- direct chat and checkout simulation need final launch-readiness review before default flags are enabled
- checkout remains simulation-only; real provider/payment behavior is intentionally unimplemented
- no dedicated browser-level e2e project exists; current frontend proof is MVC host and service integration

## 6. Gaps To Backend-Complete

For the currently implemented MVP backend slices, the main `Backend-complete` gaps have been closed.

Remaining gaps apply to later or intentionally deferred scope:

1. Keep later/feature-flagged modules disabled until explicitly promoted:
   - group ownership transfer/dissolution
   - direct chat until explicitly enabled for launch
   - checkout simulation until explicitly enabled for launch
   - feed/search projections beyond the basic query endpoints
2. If a future persistence-provider change is approved, document it explicitly and re-prove relational and concurrency behavior for that provider.

## 7. Suggested Next Focus

Recommended next implementation focus:

1. Preserve the SQLite-backed MVP path while later-scope modules are added behind explicit decisions and flags
2. Extend persistence-backed integration coverage alongside any new module slice
3. Keep concurrency proof current for every new transaction-sensitive workflow

Rationale:

- The documented MVP backend slice surface is implemented and runtime-persistent for the currently shipped modules.
- The remaining work is now about later-scope expansion, not replacing the current persistence path.

