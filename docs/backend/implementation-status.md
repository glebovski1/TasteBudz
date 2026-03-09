# TasteBudz Backend Implementation Status

This document tracks the current backend implementation state.

It is a progress tracker, not a source of product or architecture truth.
Use the primary backend documents for requirements, architecture, domain rules, API contracts, and testing policy.

Last verified: 2026-03-09

## 1. Overall State

Current overall state:

- foundation is implemented
- several core MVP backend slices are `Backend-logic ready`
- the backend is not yet `Backend-complete`

Using the definitions in `docs/backend/implementation-approach.md`:

- `Backend-logic ready` means service logic and API behavior are implemented and tested
- `Backend-complete` means real persistence and required concurrency-sensitive behavior are also proven

Current practical assessment:

- Auth and Access: `Backend-logic ready`
- Profiles: `Backend-logic ready`
- Restaurants: `Backend-logic ready`
- Events: `Backend-logic ready`
- Groups: `Backend-logic ready`
- Discovery / Budz: `Backend-logic ready`
- Notifications: `Backend-logic ready`
- Messaging: `Backend-logic ready`
- Moderation and Audit: `Backend-logic ready`
- Real SQL persistence: not yet implemented

## 2. Implemented Runtime Foundation

Implemented foundation pieces:

- ASP.NET Core controller-based API host
- centralized ProblemDetails-style exception handling
- custom bearer-token authentication backed by stored sessions
- modular service and repository structure
- in-memory repository implementations for current modules
- deterministic seeded restaurant catalog
- unit and integration test projects with `WebApplicationFactory<Program>`

Current runtime persistence note:

- the app still runs on in-memory storage
- SQL Server work in `database/sqlserver/` is planning-only and not yet wired into runtime behavior

## 3. Module Status

| Module | Current state | Notes |
|---|---|---|
| Auth and Access | Implemented slice | Register, login, refresh, logout, current-user auth pipeline, role-aware auth, account deletion |
| Profiles | Implemented slice | Onboarding status, profile update/read, preferences, availability, privacy, blocks, dashboard summaries |
| Restaurants | Implemented slice | Browse, detail, deterministic suggestions, seeded catalog |
| Events | Implemented slice | Browse, create, detail, update, participants, join, leave/accept/decline, invite, cancel, lifecycle sync, owner-only group link, restriction checks |
| Groups | Implemented slice | Browse/search, create/detail/update, join/leave, owner removal, private invites, linked-event listing |
| Discovery / Budz | Implemented slice | Search, swipe candidates, Like/Pass decisions, reciprocal Budz creation, privacy/block/restriction filtering |
| Notifications | Implemented slice | In-app notification center list/read API over existing workflow notifications |
| Messaging | Implemented slice | Shared SignalR chat hub plus paged event/group message history with scope-derived auth |
| Moderation and Audit | Implemented slice | Report submission, moderation queue/detail/resolve, scoped restrictions, admin audit-log query |

## 4. Implemented Endpoint Surface

Implemented controller surface as of 2026-03-09:

- `/api/v1/auth/*`
- `/api/v1/onboarding/status`
- `/api/v1/profiles/me`
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
- `/api/v1/restaurants/suggestions`
- `/api/v1/events`
- `/api/v1/events/{eventId}`
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
- `/api/v1/discovery/people`
- `/api/v1/discovery/swipe-candidates`
- `/api/v1/discovery/swipes`
- `/api/v1/budz`
- `/api/v1/notifications`
- `/api/v1/notifications/{notificationId}`
- `/api/v1/reports`
- `/api/v1/moderation/reports`
- `/api/v1/moderation/reports/{reportId}`
- `/api/v1/moderation/restrictions`
- `/api/v1/moderation/restrictions/{restrictionId}`
- `/api/v1/audit-logs`
- `/hubs/chat`

Not yet implemented from later/feature-flagged API shape:

- group ownership transfer and dissolution endpoints
- direct chat endpoints
- restaurant operations/discount endpoints

## 5. Test Status

Current automated test status as of 2026-03-09:

- 38 unit tests
- 23 integration tests
- 61 passing tests total

Current covered areas:

- password hashing
- auth registration, login, refresh, logout, duplicate-credential handling, and protected endpoint access
- profile update workflows
- recurring and one-off availability edge cases
- blocks and dashboard behavior
- restaurant browse and suggestion behavior
- restaurant detail/not-found behavior
- event host auto-join behavior
- closed-event invite acceptance capacity rule
- event capacity validation
- event last-seat concurrency guard
- event group-link authorization
- moderator participant removal after `DecisionAt`
- group create/join/invite/detail workflows
- discovery search/swipe/Budz workflows
- notification-center read/update behavior
- event chat and group chat authorization plus hub delivery
- report, restriction, role-enforcement, and audit-log workflows
- ProblemDetails behavior for selected failure cases

Important testing gaps still open:

- no real SQL-backed persistence integration tests
- no persistence-backed concurrency proof for event joins or invite acceptance races
- no SQL-backed messaging/group/discovery/moderation integration tests
- later/disabled features such as direct chat and group ownership transfer remain intentionally untested at runtime

## 6. Gaps To Backend-Complete

The largest remaining gaps are:

1. Replace in-memory persistence with the approved SQL Server / Azure SQL path.
2. Add persistence-backed integration tests for the implemented workflows.
3. Add persistence-backed concurrency proof for event participation and other race-prone workflows.
4. Keep later/feature-flagged modules disabled until explicitly promoted:
   - direct chat
   - group ownership transfer/dissolution
   - restaurant operations and discounts

## 7. Suggested Next Focus

Recommended next implementation focus:

1. SQL-backed persistence integration for the current `Backend-logic ready` slices
2. Persistence-backed concurrency proof for Events and other race-prone workflows
3. Broader persistence-path validation for messaging, moderation, and notifications

Rationale:

- The documented MVP backend slice surface is now implemented at the logic/API level.
- The remaining work to reach `Backend-complete` is persistence and persistence-sensitive proof rather than new MVP feature breadth.

