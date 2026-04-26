# TasteBudz Backend Testing Strategy

This document defines how the TasteBudz backend should be tested during implementation and validation.
It is aligned with the functional requirements, backend architecture, domain model, API surface, and accepted backend decisions.

## 1. Purpose

TasteBudz is designed so the backend owns business correctness.
The test strategy must therefore prove server-enforced behavior such as event capacity, lifecycle state, authorization, privacy, moderation, and chat access.

This document exists to:

- define backend-specific test layers
- identify the highest-risk rules that must be proven
- set practical expectations for a capstone MVP
- support backend implementation when frontend and database work are owned by different teammates
- give contributors and AI agents a stable source of truth for test-related decisions

## 2. Project Reality and Boundaries

Current team reality:

- backend implementation is owned separately from frontend implementation
- SQLite runtime persistence is implemented for local development and integration tests
- Azure SQL / SQL Server is the production persistence target for Azure deployment
- backend documentation must stay aligned with the provider switch

Implications:

- backend work must not wait for frontend completion before testing begins
- backend business rules and HTTP contracts should be proven independently of frontend UI work
- repository and persistence boundaries should continue to isolate service logic from storage details
- persistence-sensitive workflows must be validated against the real SQLite relational path for automated tests, not against in-memory shortcuts alone
- provider-sensitive infrastructure changes should include focused validation that SQL Server configuration is selectable and that production startup does not auto-apply schema

This is a backend testing strategy, not a frontend QA plan and not a database performance tuning plan.

## 3. Testing Position

The backend should be tested as the system of record for product correctness.
The most important tests are not UI tests. They are:

- service and domain rule tests
- API and authorization tests
- persistence-backed workflow tests
- concurrency and integrity tests

If a business rule can be bypassed by the client, the backend is not correct.
If a rule exists only in docs and not in tests for a high-risk workflow, the backend is not sufficiently protected against regression.

## 4. Testing Principles

- Test server-owned rules where they actually live: services, domain logic, repositories, and API boundaries.
- Keep tests aligned with the architecture: thin endpoints, service-owned workflows, repository boundaries around persistence.
- Prefer risk-based coverage over broad shallow coverage.
- Keep the suite pragmatic for a capstone: small, readable, maintainable, and tied to real invariants.
- Every backend change that affects behavior should add or update tests.
- Do not treat handwritten fakes or EF Core in-memory shortcuts as proof of relational correctness for transaction-sensitive workflows.

## 5. Validation Levels

Because backend, database, and frontend work are split across teammates, backend progress should be tracked at two levels.

### 5.1 Backend-Logic Ready

A feature is backend-logic ready when:

- the business workflow is implemented in the correct layer
- unit tests prove the core rules and invariants
- API or host-level integration tests prove contract and authorization behavior when an endpoint exists
- fake or temporary repository implementations are only being used as test doubles, not as proof of relational behavior

### 5.2 Backend-Complete

A feature is backend-complete when:

- the real repository and persistence path are integrated
- persistence-sensitive behavior is verified against the real relational path
- concurrency-sensitive workflows have targeted race-condition coverage where required
- cross-module effects are validated for the implemented feature

These two levels prevent backend work from blocking early while still making it clear when final proof is still missing.

## 6. Test Layers

TasteBudz should use a layered backend test strategy.

| Layer | Primary purpose | Typical examples |
|---|---|---|
| Unit tests | Fast proof of service and domain rules | capacity math, ownership checks, invite rules, swipe matching, privacy decisions |
| Host/API integration tests | Realistic proof of endpoint behavior, auth, DI wiring, and HTTP contracts | `401` and `403` handling, validation errors, DTO shape, list envelopes |
| Persistence integration tests | Proof that repository and relational behavior support backend rules | unique participant behavior, required relations, query/filter correctness |
| Concurrency tests | Proof of correctness under race conditions | last-seat joins, invite accept with one seat remaining, near-`DecisionAt` contention |
| Cross-module backend workflow tests | Proof that adjacent modules interact correctly inside the backend | events plus profiles, groups plus messaging, moderation plus chat restrictions |

Do not create excessive duplication across layers.
If a rule is already well-proven in a lower layer, higher layers should focus on contract, wiring, authorization, and integration value.

## 7. Ownership-Based Strategy

### 7.1 Backend vs Frontend

Because frontend is owned separately, backend testing should strongly protect the API contract boundary.

This means backend tests should explicitly verify:

- correct status codes
- request validation behavior
- DTO envelopes and response shape
- access control outcomes
- hidden vs forbidden endpoint behavior when applicable

Frontend integration should consume these contracts, but frontend completion is not required before backend tests are valuable.

### 7.2 Backend vs Database

Because database implementation is owned separately, backend testing should not depend on final persistence being complete before rule testing starts.

This means:

- start with repository interfaces and test doubles for service-level rule testing
- define repository expectations clearly enough that real implementations can be verified later
- add persistence-backed integration tests once the real repository path exists
- reserve real DB proof for workflows where relational behavior matters materially

## 8. Risk-Based Priorities

The following areas are the highest priority for backend testing.

### P0 - Events and participation correctness

- host auto-joins and counts toward capacity
- capacity remains within the allowed range and active participants never exceed it
- open events allow instant join when seats are available
- event invites do not reserve seats
- `DecisionAt` locks participant changes except approved override paths
- lifecycle transitions remain server-controlled
- completed-event feedback is accepted only from joined participants
- event feedback upsert enforces one entry per participant, required text, and 1-5 rating

### P0 - Authorization, privacy, and blocking

- only authorized actors can edit, remove, moderate, or access protected resources
- only admins can issue password reset tokens, anonymous reset requests do not disclose account existence, and reset completion revokes existing sessions
- discovery-disabled users are excluded where required
- `DiscoveryVisibility` restrictions hide users from discovery/search where required
- blocking prevents new disallowed interaction paths
- launched-but-forbidden behavior returns the correct status code (for example `403`)
- hidden/not-launched feature-flagged endpoints return `404`
- role-owned later endpoints enforce the correct actor context (`GroupOwner`, `RestaurantAdmin`)
- event feedback visibility follows Open vs Closed event rules
- event-feedback media retrieval uses the same authorization boundary as feedback listing

### P1 - Groups and messaging access

- group owner remains canonical and active
- only the current group owner can associate an event with that group's `GroupId`
- group-linked event tests should prove public groups only link Open events and private groups only link Closed events
- only active group members can access group chat
- only joined event participants can access event chat
- only the supported user and admins can access support chat
- leaving or removal revokes access immediately
- direct chat remains hidden when disabled
- enabled direct chat is limited to connected Budz, current block state, and active `ChatSend` restrictions

### P1 - Restaurant operations

- default configuration exposes restaurant operation and slot endpoints
- explicitly disabled restaurant operation and slot endpoints return `404`
- assignment grant/revoke updates `RestaurantAdmin` role behavior correctly
- restaurant admins can mutate only assigned restaurants
- slot validation enforces time, capacity, cutoff, threshold, and discount-percentage rules
- event-host slot reservation enforces event/slot uniqueness, host ownership, active event status, and time/capacity fit
- MVC create-event coverage verifies slot-aware restaurant filtering/listing and create-then-reserve orchestration when a host chooses a slot from the restaurant picker
- discount simulation recalculates before cutoff, carries the configured slot percentage, and freezes after cutoff
- reserved-slot cancellation cancels the linked event through normal cancellation behavior

### P1 - Feature-flagged checkout simulation

- disabled checkout endpoints return `404`
- checkout creation requires a current joined event participant and selected restaurant
- simulated totals derive from restaurant price tier and active discount state
- checkout completion and cancellation are owner-only terminal transitions
- checkout remains simulation-only and has no external provider side effects

### P1 - Moderation and audit

- reports can be created and resolved
- report-evidence attachments respect reporter vs moderator/admin access boundaries
- event-feedback reports preserve the feedback author's user target and related event context
- restrictions prevent forbidden actions while active
- restriction scope values are validated against the documented API contract
- moderation and support actions create audit records where required

### P2 - Browse and support workflows

- restaurant and event filters return the correct result set
- admin restaurant catalog create/update/archive/restore keeps geocoded coordinates and browse visibility aligned
- discovery search hides one-sided outbound swipe targets until the target user decides back
- notifications are created for important workflow changes with expected type and required context fields
- paging and query contracts stay stable

## 9. Recommended Development Workflow Per Module

For each backend module:

1. Define module scope and relevant source documents.
2. List the key use cases, invariants, edge cases, and failure scenarios.
3. Write or outline the test plan before implementation starts.
4. Implement domain models and service logic.
5. Add unit tests for the core rules.
6. Add host/API integration tests for contracts and authorization when endpoints exist.
7. Add persistence-backed tests when the real repository path is available.
8. Add concurrency tests if the workflow is transaction-sensitive.
9. Re-check the module against architecture, domain, API, and accepted decisions.

This is test-first planning, not ceremony-heavy TDD for every line of code.
The goal is to define proof up front and then implement toward that proof.

## 10. Practical Test Project Structure

Use the existing test projects and keep the structure small.

```text
tests/
  TasteBudz.Backend.UnitTests/
    Auth/
    Profiles/
    Restaurants/
    Events/
    Groups/
    Discovery/
    Messaging/
    Moderation/
    Payments/
    Shared/
  TasteBudz.Backend.IntegrationTests/
    Api/
    Authorization/
    Workflows/
    Concurrency/
    Shared/
```

Do not split into many separate test projects unless the suite grows enough to justify it.
For the current capstone scope, two projects are sufficient.

## 11. Recommended Tooling Direction

Current repository state already includes xUnit-based test projects.
Recommended additions as the backend matures:

| Need | Suggested option | Why |
|---|---|---|
| Unit and integration runner | xUnit | Already present in the repo and suitable for .NET backend work |
| Integration host | `WebApplicationFactory<Program>` | Best fit for realistic ASP.NET Core API testing |
| Assertions | FluentAssertions | Improves readability of behavior-focused tests |
| Test doubles | simple fakes first, mocking only when needed | Keeps tests explicit and less brittle |
| Database reset | recreate temporary SQLite databases from canonical SQL scripts | Keeps persistence tests repeatable and aligned to the local/test runtime schema |
| Real DB test environment | temporary SQLite files per test or per fixture | Matches the implemented local/test runtime path and supports relational/concurrency proof |
| Provider-sensitive checks | configuration/startup tests and optional SQL Server smoke tests when infrastructure is available | Confirms Azure SQL can be selected without changing code and without startup migrations |

Avoid treating EF Core in-memory behavior as equivalent to SQLite relational behavior for transactional correctness.

## 12. Test Data and Determinism

- Use explicit builders or factories for users, events, groups, messages, and restrictions.
- Introduce a clock abstraction so `DecisionAt`, completion, and time-based restrictions can be tested deterministically.
- Seed restaurant and ZIP-coordinate data deterministically from the canonical SQLite seed script for local/test runs.
- Create helpers for authenticated test users with clear roles such as User, Host, GroupOwner, Moderator, and Admin.
- Keep scenario data compact and readable.
- Reset persistence state between integration tests by recreating the temporary SQLite database from canonical SQL assets.
- Treat SQL Server/Azure SQL scripts as manually applied release assets; automated tests should not require a developer-local SQL Server unless a provider-specific test fixture explicitly opts in.

## 13. Core Scenario Catalogue

These scenarios should anchor early backend testing work.

| ID | Scenario | Priority | Minimum proof |
|---|---|---|---|
| BT-01 | Register, authenticate, and access a protected endpoint | High | Auth and authorization behavior is correct |
| BT-02 | User updates profile, preferences, and privacy settings | High | Current-user boundaries and persistence flow behave correctly |
| BT-03 | Host creates an open event and is auto-counted as joined | High | Host participant and capacity math are correct |
| BT-04 | Two users race for the final seat in an event | Critical | Only one succeeds and stored state remains valid |
| BT-05 | Event invite is accepted after the event fills | Critical | Accept fails because invites do not reserve seats |
| BT-06 | `DecisionAt` passes and participant changes are blocked | Critical | Server enforces lifecycle timing rules |
| BT-07 | Removed participant immediately loses event-chat access | High | Chat authorization reflects current participation |
| BT-08 | Private group invite is accepted and membership is created | High | Private-group membership rules are enforced |
| BT-09 | Reciprocal Like creates one Bud connection | Medium | Matching logic respects the accepted MVP rule |
| BT-10 | Moderator applies a scoped restriction that blocks a forbidden action | High | Restriction enforcement is active and auditable |
| BT-11 | Non-owner cannot link an event to group context, and group-linked event type must match group visibility | High | Group-linked events stay owner-managed and visibility-consistent |
| BT-12 | Discovery search excludes a user with an active `DiscoveryVisibility` restriction | High | Discovery filtering respects moderation scope |
| BT-13 | Profile avatar upload replaces the previous avatar and serves stored bytes | High | Media storage and profile contracts stay aligned |
| BT-14 | Report evidence attachment is hidden from unrelated users but visible to moderators | High | Media authorization respects moderation context |
| BT-15 | Admin grants and revokes a restaurant admin assignment | High | Role and assignment authority stay aligned |
| BT-15A | Admin creates, updates, archives, and restores a restaurant catalog entry | High | Manual catalog maintenance keeps geocoded map/search state consistent |
| BT-16 | Event host reserves an open restaurant slot | High | Event restaurant selection, cuisine clearing, and reservation uniqueness are correct |
| BT-17 | Restaurant admin cancels a reserved slot | High | Reservation and linked event cancellation behavior is correct |
| BT-18 | Discount threshold crosses before and after cutoff | Medium | Simulation state carries configured percentage, recalculates before cutoff, and freezes after cutoff |
| BT-19 | Direct chat between Budz is enabled behind flag | High | Budz-only access, block checks, history retrieval, and hub delivery are correct |
| BT-20 | Joined participant creates and completes simulated checkout | High | Feature flag, selected-restaurant requirement, simulated totals, and terminal status transitions are correct |
| BT-21 | Admin issues a password reset token and user completes reset | High | Admin-only issue, one-time token use, password update, and session revocation are correct |
| BT-21A | User submits an anonymous password reset request and admin handles it | High | Public response does not disclose account existence and admin review can close the request directly or via token issuance |
| BT-22 | User and admin exchange support messages | High | Support-scope access is limited to the supported user and admins across REST and hub behavior |
| BT-23 | User swipes another user before reciprocal decision | Medium | The swiped user is hidden from that actor's people search until deciding back |
| BT-24 | Joined participant adds feedback to a completed event | High | Completed-only eligibility, one-entry upsert, Open/Closed visibility, photo authorization, and validation are correct |

## 14. Module-Specific Test Emphasis

| Module | Main proof to prioritize |
|---|---|
| Auth and Access | login, logout, auth boundaries, protected endpoint access, anonymous reset requests, admin-issued password reset |
| Profiles and Preferences | current-user isolation, availability behavior, privacy, blocks |
| Restaurants | browse and filter correctness, deterministic suggestions |
| Restaurant Operations | default-active behavior, kill-switch behavior, assignments, slot lifecycle, reservation invariants, discount simulation |
| Events | create, update, cancel, join, leave, invites, lifecycle, feedback, concurrency |
| Groups | create, join, leave, private invites, owner-only actions, group-owner-only later admin flows |
| Discovery and Budz | search, privacy/block/restriction filters, one-sided outbound swipe search filtering, swipe replacement, reciprocal-like connection creation |
| Messaging | membership-derived access, support-scope access, Budz-only direct chat, history retrieval, restriction-aware send behavior |
| Payments | feature-flag behavior, participant-owned checkout creation, simulated totals, completion/cancellation state rules |
| Notifications | workflow-triggered notifications, type contract, required context payload, read-state updates |
| Moderation and Audit | reports, restrictions, scope validation, role enforcement, audit entries |

## 15. Definition of Done for Backend Features

A backend feature is not done until the following evidence exists at the appropriate level:

| Condition | Minimum expected proof |
|---|---|
| Business rule exists in the correct layer | Unit or workflow test proves it |
| Protected behavior is enforced | Authorized and unauthorized cases are tested |
| Public contract changed intentionally | Integration/API tests reflect the intended result |
| Error semantics are part of the contract | Integration/API tests assert expected status outcomes for forbidden/hidden/invalid high-risk flows |
| Persistence-sensitive rule exists | Real relational-path test exists before calling the feature backend-complete |
| Concurrency-sensitive workflow exists | Targeted concurrency test exists before calling the feature backend-complete |
| Behavior changed from previous intent | Relevant docs and tests are updated together |

## 16. Maintenance and Document Alignment

This testing strategy is derived from and must remain aligned with:

- `docs/TasteBudz_Functional_Requirements.md`
- `docs/backend/backend-decisions.md`
- `docs/backend/backend-architecture.md`
- `docs/backend/domain-model.md`
- `docs/backend/api-endpoints.md`

If those documents change in a way that affects backend correctness, authorization, lifecycle rules, API shape, or module boundaries, this testing strategy should be reviewed and updated.
