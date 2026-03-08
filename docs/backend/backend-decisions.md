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
External restaurant APIs add cost, rate limits, and unpredictable data quality.

### Decision
MVP uses a seeded internal restaurant catalog as the source of truth for restaurant selection.

### Consequences
- Testing is simpler and data is predictable.
- External search remains optional later work.

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
- Closed-event invite acceptance can fail if the event is already full.

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

## [ADR-014] Blocking Is a Soft Block

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
Hard hiding/removal behavior across shared contexts would add large safety and UX complexity.

### Decision
Blocking prevents new direct interaction paths such as messaging, invitations, and new Bud interactions, but does not automatically remove users from already shared events/groups or split existing shared-context chat.

### Consequences
- Shared-context separation requires host/owner/moderator action.
- Blocking filters must apply consistently in discovery and private-contact paths.

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
