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

- Auth and Access: early `Backend-logic ready`
- Profiles: meaningful `Backend-logic ready` slice
- Restaurants: meaningful `Backend-logic ready` slice
- Events: meaningful but still partial `Backend-logic ready` slice
- Groups: not yet implemented at API/workflow level
- Discovery / Budz: not yet implemented at API/workflow level
- Messaging: not yet implemented
- Moderation and Audit: not yet implemented
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
| Auth and Access | Implemented slice | Register, login, refresh, logout, current-user auth pipeline, account deletion |
| Profiles | Implemented slice | Onboarding status, profile update/read, preferences, availability, privacy, blocks, dashboard summaries |
| Restaurants | Implemented slice | Browse, detail, deterministic suggestions, seeded catalog |
| Events | Implemented slice | Browse, create, detail, update, participants, join, leave/accept/decline, invite, cancel, lifecycle sync |
| Groups | Partial internals only | Domain/repository scaffolding exists, but no group controller/API workflow surface yet |
| Discovery / Budz | Internal scaffolding only | Repository interface exists, but no implemented API workflow slice yet |
| Notifications | Internal support only | Notification creation is used by workflows, but no public notification API exists yet |
| Messaging | Not started | No controllers, services, or chat workflow implementation yet |
| Moderation and Audit | Not started | No moderation workflow slice yet |

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

Not yet implemented from the target API shape:

- groups endpoints
- discovery / Budz endpoints
- messaging endpoints
- notifications endpoints
- moderation and audit endpoints

## 5. Test Status

Current automated test status as of 2026-03-09:

- 20 unit tests
- 9 integration tests
- 29 passing tests total

Current covered areas:

- password hashing
- auth registration and protected endpoint access
- profile update workflows
- recurring and one-off availability edge cases
- restaurant browse and suggestion behavior
- event host auto-join behavior
- closed-event invite acceptance capacity rule
- event capacity validation
- ProblemDetails behavior for selected failure cases

Important testing gaps still open:

- no real SQL-backed persistence integration tests
- no dedicated concurrency tests for last-seat joins or invite acceptance races
- no messaging authorization tests
- no group workflow tests
- no discovery / Budz workflow tests
- no moderation / restriction tests

## 6. Gaps To Backend-Complete

The largest remaining gaps are:

1. Replace in-memory persistence with the approved SQL Server / Azure SQL path.
2. Add persistence-backed integration tests for the implemented workflows.
3. Add concurrency-focused tests for event participation edge cases.
4. Implement the remaining major MVP modules:
   - groups
   - discovery / Budz
   - messaging
   - moderation and audit
5. Expand endpoint coverage to match the documented MVP API surface.

## 7. Suggested Next Focus

Recommended next implementation focus:

1. Events hardening to `Backend-complete`
   - real persistence
   - concurrency proof
   - persistence-backed lifecycle validation
2. Groups MVP workflow slice
3. Discovery / Budz MVP workflow slice
4. Messaging MVP workflow slice

Rationale:

- Events are the most transaction-sensitive current module.
- The implementation approach document explicitly calls out high-risk modules such as Events as the best candidate to finish deeply before spreading further across the backlog.

