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
- final database implementation is owned separately from backend implementation
- backend documentation is ahead of the current codebase

Implications:

- backend work must not wait for frontend completion before testing begins
- backend business rules and HTTP contracts should be proven independently of frontend UI work
- repository and persistence boundaries should allow backend logic to be developed before the final database path is complete
- persistence-sensitive workflows still require later validation against the real relational path

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
- closed-event invites do not reserve seats
- `DecisionAt` locks participant changes except approved override paths
- lifecycle transitions remain server-controlled

### P0 - Authorization, privacy, and blocking

- only authorized actors can edit, remove, moderate, or access protected resources
- discovery-disabled users are excluded where required
- `DiscoveryVisibility` restrictions hide users from discovery/search where required
- blocking prevents new disallowed interaction paths
- launched-but-forbidden behavior returns the correct status code

### P1 - Groups and messaging access

- group owner remains canonical and active
- only the current group owner can associate an event with that group's `GroupId`
- only active group members can access group chat
- only joined event participants can access event chat
- leaving or removal revokes access immediately

### P1 - Moderation and audit

- reports can be created and resolved
- restrictions prevent forbidden actions while active
- moderation and support actions create audit records where required

### P2 - Browse and support workflows

- restaurant and event filters return the correct result set
- notifications are created for important workflow changes
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
| Database reset | Respawn or controlled cleanup strategy | Keeps persistence tests repeatable |
| Real DB test environment | local SQL Server test DB or containerized SQL Server | Needed for realistic relational and concurrency proof |

Avoid treating EF Core in-memory behavior as equivalent to SQL Server behavior for relational correctness.

## 12. Test Data and Determinism

- Use explicit builders or factories for users, events, groups, messages, and restrictions.
- Introduce a clock abstraction so `DecisionAt`, completion, and time-based restrictions can be tested deterministically.
- Seed restaurant data deterministically.
- Create helpers for authenticated test users with clear roles such as User, Host, GroupOwner, Moderator, and Admin.
- Keep scenario data compact and readable.
- Reset persistence state between integration tests.

## 13. Core Scenario Catalogue

These scenarios should anchor early backend testing work.

| ID | Scenario | Priority | Minimum proof |
|---|---|---|---|
| BT-01 | Register, authenticate, and access a protected endpoint | High | Auth and authorization behavior is correct |
| BT-02 | User updates profile, preferences, and privacy settings | High | Current-user boundaries and persistence flow behave correctly |
| BT-03 | Host creates an open event and is auto-counted as joined | High | Host participant and capacity math are correct |
| BT-04 | Two users race for the final seat in an event | Critical | Only one succeeds and stored state remains valid |
| BT-05 | Closed invite is accepted after the event fills | Critical | Accept fails because invites do not reserve seats |
| BT-06 | `DecisionAt` passes and participant changes are blocked | Critical | Server enforces lifecycle timing rules |
| BT-07 | Removed participant immediately loses event-chat access | High | Chat authorization reflects current participation |
| BT-08 | Private group invite is accepted and membership is created | High | Private-group membership rules are enforced |
| BT-09 | Reciprocal Like creates one Bud connection | Medium | Matching logic respects the accepted MVP rule |
| BT-10 | Moderator applies a scoped restriction that blocks a forbidden action | High | Restriction enforcement is active and auditable |
| BT-11 | Non-owner cannot link an event to group context | High | Group-linked events stay owner-managed |
| BT-12 | Discovery search excludes a user with an active `DiscoveryVisibility` restriction | High | Discovery filtering respects moderation scope |

## 14. Module-Specific Test Emphasis

| Module | Main proof to prioritize |
|---|---|
| Auth and Access | login, logout, auth boundaries, protected endpoint access |
| Profiles and Preferences | current-user isolation, availability behavior, privacy, blocks |
| Restaurants | browse and filter correctness, deterministic suggestions |
| Events | create, update, cancel, join, leave, invites, lifecycle, concurrency |
| Groups | create, join, leave, private invites, owner-only actions |
| Discovery and Budz | search, privacy/block/restriction filters, swipe replacement, reciprocal-like connection creation |
| Messaging | membership-derived access, history retrieval, restriction-aware send behavior |
| Notifications | workflow-triggered notifications and read-state updates |
| Moderation and Audit | reports, restrictions, role enforcement, audit entries |

## 15. Definition of Done for Backend Features

A backend feature is not done until the following evidence exists at the appropriate level:

| Condition | Minimum expected proof |
|---|---|
| Business rule exists in the correct layer | Unit or workflow test proves it |
| Protected behavior is enforced | Authorized and unauthorized cases are tested |
| Public contract changed intentionally | Integration/API tests reflect the intended result |
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
