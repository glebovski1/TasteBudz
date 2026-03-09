# TasteBudz - Functional Requirements (FR) with Acceptance Criteria

## 0. MVP Build Checklist

Implement the following MVP items first. Each item references the owning requirement(s):

- Account auth + sessions + first-run onboarding (FR-001, FR-002)
- Profile CRUD + dashboard summary + account deletion (FR-002)
- Preferences + availability windows (FR-003, FR-004)
- Privacy controls + blocking (FR-005, FR-024)
- Seeded restaurant catalog + Restaurant entity (FR-006)
- Restaurant browse + filtering + simple suggestions (FR-007)
- People discovery core: search + swipe + Budz list (FR-018, FR-019, FR-020)
- Basic browse/search for open events and public groups (FR-022)
- Create events (Open + Closed) + closed invites by username (FR-008)
- Join/leave with atomic capacity enforcement + DecisionAt lock (FR-009, FR-010)
- Groups: create/join/leave + owner management (FR-011, FR-012, FR-013)
- Event status lifecycle + DecisionAt evaluation (FR-014)
- In-app notifications for state changes and material event updates (FR-016)
- Event + group chat, real-time and text-only (FR-017, FR-017A)
- Safety stack: report -> moderation queue -> scoped soft bans -> audit log (FR-025, FR-026, FR-027, FR-028)

> MVP decisions locked for the capstone:
> - Restaurants use a seeded internal catalog in MVP with no external API dependency.
> - Notifications are in-app only in MVP; no scheduled reminder jobs are required.
> - People discovery in MVP includes search + swipe + mutual Budz; direct 1-on-1 messaging remains out of MVP UI.
> - Basic query-based browse/search for open events and public groups is in scope; richer feed/caching is later.
> - Event chat and group chat are both in scope for MVP and share the same basic messaging core.

## 1. System Overview

TasteBudz is a web-based social dining coordination platform that connects people who want to try restaurants together based on cuisine preferences, dietary compatibility, location proximity, and availability. The product focuses on helping users discover compatible people, plan small dining events, and coordinate safely.

For MVP UX, the product is organized around three core surfaces:

- Profile and onboarding
- Budz and groups
- Events

Core value flow:

User wants food -> discovers Budz, a group, or an event -> restaurant is selected or suggested -> participants confirm -> dinner happens.

### 1.1 Roles and Permissions (MVP)

| Role | Allowed actions (MVP, non-exhaustive) |
|---|---|
| User | Register/login/logout (FR-001), update profile/preferences/availability/privacy (FR-002 to FR-005), browse/filter restaurants (FR-007), search/swipe people and view Budz (FR-018 to FR-020), browse/search open events and public groups (FR-022), join/leave Open events and accept/decline Closed invites (FR-008 to FR-009), use event chat when participating and group chat when a current member (FR-017 to FR-017A), block/report users (FR-024 to FR-025) |
| Host | Create Open/Closed events (FR-008), invite users to Closed events (FR-008), edit event details before cancellation/completion (FR-014), cancel own event with reason (FR-014), view participants and event details (FR-008 to FR-014) |
| Group Owner | Create group, manage name/description/visibility (FR-011 to FR-012), remove group members (FR-012), transfer ownership or dissolve group later (FR-012A), create/view group-linked events (FR-013), use group chat (FR-017A) |
| Moderator | View report queue, resolve reports, apply/expire scoped restrictions, and rely on audit logging (FR-026 to FR-028) |
| Admin | All Moderator actions plus support overrides for safety/correctness cases, event cancellation support, and audit-log review (FR-014, FR-026 to FR-028) |

## 2. Functional Requirements Catalogue

Priority legend:

- MVP: required for initial release
- MVP+: optional improvement if time permits
- MVP++: backend-ready or feature-flagged for later

### 2.1 User Stories

#### MVP User Stories

- US-001: As a user, I want to register so that I can use TasteBudz.
- US-002: As a user, I want to log in and out so that my account stays secure.
- US-003: As a user, I want to edit my profile so that other people understand my vibe and location area.
- US-004: As a user, I want to manage cuisine preferences, spice tolerance, and dietary/allergy flags so recommendations fit me.
- US-005: As a user, I want to define when I am available so I can find events I can actually attend.
- US-006: As a user, I want to control whether people can discover me.
- US-007: As a user, I want to browse and filter restaurants by cuisine, price, and distance.
- US-008: As a user, I want the app to suggest a restaurant so my group can decide faster.
- US-009: As a user, I want to create an open event.
- US-010: As a user, I want to create a closed event and invite specific people.
- US-011: As a user, I want to join and leave events safely.
- US-012: As a user, I want events to prevent overfilling.
- US-013: As a user, I want to create and join persistent groups.
- US-014: As a group owner, I want to manage group settings and members.
- US-015: As a user, I want to link an event to a group.
- US-016: As a user, I want clear event statuses.
- US-018: As a user, I want notifications so I do not miss important changes.
- US-019: As an event participant, I want an event chat for coordination.
- US-020: As a group member, I want a group chat for group coordination.
- US-021: As a user, I want to block someone.
- US-022: As a user, I want to report inappropriate behavior.
- US-023: As a moderator, I want a queue of reports to review.
- US-024: As an admin/moderator, I want sensitive actions to be audit-logged.
- US-025: As a user, I want to search people by username/display name.
- US-026: As a user, I want to Like/Pass discovery candidates quickly.
- US-027: As a user, I want mutual Like to create a Budz connection.
- US-029: As a user, I want to browse and search events and groups.
- US-036: As a user, I want my profile page to show active events, groups, and Budz.

#### MVP+ User Stories

- US-017: As a user, I want RSVP/cutoff controls so events are more reliable.
- US-037: As a group owner, I want to transfer ownership or dissolve a group.

#### MVP++ User Stories

- US-028: As a connected user, I want 1-on-1 chat when enabled.
- US-030: As a user, I want a Tonight/This Week feed.
- US-031: As a restaurant admin, I want to manage my restaurant profile.
- US-032: As a restaurant admin, I want to create slots with capacity/timing.
- US-033: As a user, I want to reserve a restaurant slot for an event.
- US-034: As a group, I want a discount to activate once enough people confirm.
- US-035: As a restaurant admin, I want to cancel a slot and handle linked events correctly.

### FR-001 Authentication (Register / Login / Logout)

**Priority:** MVP

**Description:** The system shall allow users to create accounts and authenticate.

**Acceptance Criteria**

- Users can register with required fields including username or email plus password; ZIP code may be collected during registration or first-run onboarding.
- Users can log in with valid credentials.
- Users can log out and invalidate the active client session/token.
- Invalid credentials return an error without revealing whether the account exists.

### FR-002 Account and Profile Management

**Priority:** MVP

**Description:** Users shall be able to view and update their profile.

**Acceptance Criteria**

- Users can edit profile fields including display name/username, bio, ZIP code, and social goal.
- Users can view a personal dashboard with profile info plus summaries of active events, groups, and Budz.
- Users can request account deletion.
- Profile changes only affect the current user's data.

### FR-003 Food Preferences, Dietary Flags, and Allergies

**Priority:** MVP

**Description:** Users shall be able to store cuisine preferences, spice tolerance, and dietary compatibility information.

**Acceptance Criteria**

- Users can select one or more cuisine tags.
- Users can set spice tolerance.
- Users can set dietary flags and allergy warnings.
- Preferences are available for matching and discovery filters.

### FR-004 Availability Windows

**Priority:** MVP

**Description:** Users shall be able to define recurring and one-off time windows when they are available for dining.

**Acceptance Criteria**

- Users can create, edit, and delete recurring weekly availability windows.
- Users can create, edit, and delete one-off availability windows.
- Availability windows can be used as filters for event matching and event search.

### FR-005 Privacy Settings

**Priority:** MVP

**Description:** Users shall be able to control basic discovery/contact visibility.

**Acceptance Criteria**

- Users can disable discovery so they are hidden from people discovery/search.
- Users can block other users as defined in FR-024.
- Privacy rules are enforced by the backend, not only the UI.

### FR-006 Restaurant Entity with Optional External PlaceId

**Priority:** MVP

**Description:** The system shall store restaurants internally and may optionally link them to an external provider identifier.

**Acceptance Criteria**

- Restaurants are stored with name, city/state/ZIP, cuisine tags, and price tier.
- Restaurants may optionally store latitude/longitude and an external PlaceId.
- Restaurant records can be referenced by events and later slot entities.

### FR-007 Restaurant Discovery and Filtering

**Priority:** MVP

**Description:** Users shall be able to browse/search restaurants and apply basic filters.

**Acceptance Criteria**

- Users can filter restaurants by cuisine, price tier, and distance.
- MVP restaurant discovery uses the seeded internal catalog only.
- Restaurant selection is reusable during event creation and may be shown in search/list form; map presentation is optional when coordinates exist.
- Midpoint or group-aware suggestion logic remains lightweight service behavior over the internal catalog.

### FR-008 Create Events (Open and Closed)

**Priority:** MVP

**Description:** Users shall be able to create dining events.

**Acceptance Criteria**

- An event includes optional title, event type (Open/Closed), start time, capacity, and exactly one of selected restaurant or cuisine target.
- The host automatically becomes a `JOINED` participant and counts toward capacity.
- Open events are discoverable and joinable by eligible users.
- Closed events are invite-only.
- Closed-event invites remain actionable until `DecisionAt` in MVP.
- Closed-event invites do not reserve seats.

**Closed Event Invite Flow**

- Host creates a closed event.
- Host invites users by exact username.
- The system creates or updates one `EventParticipant` record per invited user in `INVITED` state.
- Invitees can accept (`JOINED`) or decline (`DECLINED`) until `DecisionAt`.
- Capacity is enforced on accept/join, not on invite creation.

### FR-009 Event Participation (Join / Leave / Remove)

**Priority:** MVP

**Description:** Eligible users shall be able to join or leave events under server-controlled rules.

**Acceptance Criteria**

- Joining creates or reactivates a participant record.
- Leaving preserves history while freeing capacity.
- Duplicate joins are prevented.
- Capacity is enforced safely under concurrent joins/accepts.
- `ActiveParticipants` never exceeds `Capacity`.
- After `DecisionAt`, join/leave changes are blocked except admin/support override.
- A host may remove a non-host participant before `DecisionAt`.
- A moderator/admin may remove a participant at any time as a safety/support override.

### FR-010 Event Size Defaults and Limits

**Priority:** MVP

**Description:** The system shall support small-group dining with explicit event defaults and hard capacity limits.

**Acceptance Criteria**

- Typical recommended event size is 4-6 participants.
- Event capacity must be between 2 and 8 inclusive.
- The host counts toward capacity.
- Groups do not have a hard maximum member cap in MVP.

### FR-011 Persistent Groups (Create / Join / Leave)

**Priority:** MVP

**Description:** The system shall support persistent groups in addition to event-based coordination.

**Acceptance Criteria**

- Users can create a group with name, description, and visibility.
- Groups support `Public` and `Private` visibility.
- Public groups allow direct join when active.
- Private groups require invitation in MVP.
- Group members can view basic group details and the current member list.

### FR-012 Group Roles

**Priority:** MVP

**Description:** Groups shall have a simple owner/member model in MVP.

**Acceptance Criteria**

- Each group has exactly one owner.
- The owner is auto-created as an active member.
- Owners can manage group settings and remove members.
- Owners initiate private-group invites in MVP.
- `Group.OwnerUserId` is the canonical ownership source; membership is tracked separately.

### FR-012A Group Ownership Transfer and Dissolution

**Priority:** MVP+

**Description:** Groups may support explicit ownership transfer and dissolution for long-term maintainability.

**Acceptance Criteria**

- A group owner can transfer ownership to another current active member.
- A group owner can dissolve a group with explicit confirmation.
- Ownership transfer and dissolution are timestamped and auditable.

### FR-013 Link Events to Groups

**Priority:** MVP

**Description:** Events may optionally be associated with a group.

**Acceptance Criteria**

- An event may store an optional `GroupId`.
- Only the current group owner can associate an event with that group's context in MVP.
- Group-linked events are viewable in group context.
- Group membership does not replace event participation rules.

### FR-014 Event Status Lifecycle

**Priority:** MVP

**Description:** Each event shall follow a server-controlled status lifecycle that reflects capacity and ensures events do not occur if there are not enough participants.

**MVP Summary**

- Event status is server-controlled; clients cannot set status directly.
- `DecisionAt` locks participation changes and determines whether the event proceeds.
- `OPEN` and `FULL` reflect current capacity.
- At `DecisionAt`, the event becomes `CONFIRMED` or `CANCELLED`.
- `CANCELLED` and `COMPLETED` are terminal statuses.
- Hosts may edit event details before an event is cancelled/completed; material changes notify participants.
- Confirmed events auto-complete after the scheduled time passes according to server policy.

**Definitions**

- `DecisionAt`: default is `EventStartAt - 15 minutes` for open events and `EventStartAt - 24 hours` for closed events.
- `MinParticipantsToRun`: default is `2`.
- `ActiveParticipants`: count of participants in `JOINED` state.

**Canonical statuses**

- `OPEN`
- `FULL`
- `CONFIRMED`
- `CANCELLED`
- `COMPLETED`

**Acceptance Criteria**

- Server updates event status deterministically.
- Joining/leaving toggles between `OPEN` and `FULL` safely under concurrency.
- At `DecisionAt`, `OPEN` or `FULL` becomes `CONFIRMED` when minimum participants is met; otherwise it becomes `CANCELLED`.
- Hosts can cancel events with a reason.
- Hosts can edit material event details before completion/cancellation; participants receive an update notification.
- Events automatically become `COMPLETED` after the scheduled time passes according to server policy.

### FR-015 Advanced Reliability Controls (RSVP, Cutoffs, Auto-Cancel)

**Priority:** MVP+

**Description:** The system may introduce advanced reliability controls to reduce last-minute collapses.

**Acceptance Criteria**

- The backend may store `CutoffAt`, RSVP requirements, and a minimum confirmed threshold per event.
- RSVP state can be tracked per participant when this feature is enabled.
- Auto-confirm/auto-cancel may occur at `CutoffAt` based on confirmed count.
- Capacity enforcement remains concurrency-safe.
- Admin/support override may extend cutoff or force a decision when enabled.

### FR-016 Notifications and Reminders

**Priority:** MVP

**Description:** The system shall notify users about important event activity and state changes.

**Acceptance Criteria**

- Notifications are persisted and retrievable via API.
- Users can mark notifications as read.
- MVP notification types include event invite received, joined/left, confirmed/cancelled, material event updates, group invites, and Bud matches.
- MVP notifications are in-app only.
- Event timestamps are exposed so clients can show countdowns without scheduled reminder jobs.
- Email, push, and scheduled reminders remain optional later layers.

### FR-017 Event Chat

**Priority:** MVP

**Description:** Event participants shall be able to communicate in an event-linked chat thread.

**Acceptance Criteria**

- Each event has an associated chat thread.
- Only current `JOINED` participants can read/write event chat.
- Leaving or removal revokes event-chat access immediately.
- MVP event chat is text-only.
- MVP transport uses SignalR/WebSockets for real-time delivery plus paged history retrieval.
- Direct 1-on-1 messaging remains outside MVP UI.

### FR-017A Group Chat

**Priority:** MVP

**Description:** Group members shall be able to communicate in a group-linked chat thread without sharing phone numbers.

**Acceptance Criteria**

- Each group has an associated chat thread.
- Only current active group members can read/write group chat.
- Leaving or removal revokes group-chat access immediately.
- MVP group chat is text-only and uses the same basic SignalR plus history-retrieval model as event chat.

### FR-018 People Discovery (Search)

**Priority:** MVP

**Description:** Users shall be able to discover other users via search.

**Acceptance Criteria**

- Users can search by username/display name.
- Discovery exposes only a limited public profile preview.
- Search respects privacy settings, blocks, and active discovery-visibility moderation restrictions.
- Users can block/report from discovery.

### FR-019 People Discovery (Swipe / Like / Pass)

**Priority:** MVP

**Description:** Users shall be able to discover people through a swipe-based flow.

**Acceptance Criteria**

- The system can present candidate profiles in a swipe queue.
- Users can Like or Pass.
- One effective directional swipe decision exists per actor/subject pair.
- Mutual Like produces the Budz outcome defined in FR-020.

### FR-020 Mutual Connections ("Budz")

**Priority:** MVP

**Description:** The system shall support a Budz connection model for the social layer.

**Acceptance Criteria**

- The system stores directional swipe decisions and resulting mutual Budz connections.
- A Budz connection is created only when both users have an effective Like decision toward each other.
- Budz connections are mutual, not directional.
- Users can view a list of their current Budz.
- MVP does not expose pending Bud requests or pending Bud connection state.

### FR-021 1-on-1 Messaging (Feature-Flagged)

**Priority:** MVP++

**Description:** The backend may implement direct messaging structures/endpoints while keeping the feature disabled until enabled.

**Acceptance Criteria**

- Direct chats exist as separate threads/messages.
- Direct messaging is allowed only when policy allows it, such as between Budz.
- Feature flags can disable creation/sending in production.
- Blocking, moderation, and reporting policies apply.

### FR-022 Event and Group Browse/Search

**Priority:** MVP

**Description:** Users shall be able to browse and search open events and public groups using basic query-based filters.

**Acceptance Criteria**

- Users can browse open events that match cuisine, time window, distance, price tier, and availability filters.
- Users can filter events by status.
- Users can browse/search public groups by name and visibility.
- MVP implementation may be pure database queries without a dedicated cached feed.

### FR-023 Feed Support ("Tonight / This Week")

**Priority:** MVP++

**Description:** The system may support both query-based feeds and optional cached feeds later.

**Acceptance Criteria**

- Feed output can be generated from filtered event queries.
- The backend may maintain optional cached/indexed feed views later.

### FR-024 Block Users

**Priority:** MVP

**Description:** Users shall be able to block other users.

**Acceptance Criteria**

- Blocking is directional.
- Blocked users do not appear in people discovery for each other.
- Blocking prevents new direct interaction paths such as new Bud interactions, direct/private messaging, and event/group invitations between the pair.
- Blocking does not automatically remove users from already shared groups/events or split existing shared-context chat.
- Blocking is reversible by the blocker.

### FR-025 Report Users/Content

**Priority:** MVP

**Description:** Users shall be able to report inappropriate behavior or content.

**Acceptance Criteria**

- Users can submit a report with category/reason and optional explanation.
- Reports can target a user and may include related event/message context.
- Reports are stored and accessible to moderators.

### FR-026 Moderation Workflow

**Priority:** MVP

**Description:** Moderators shall be able to review reports and take actions.

**Acceptance Criteria**

- Moderators can view reports in a review queue.
- Moderators can resolve reports with a recorded decision.
- Moderation actions are stored.

### FR-027 Scoped Restrictions ("Soft Bans")

**Priority:** MVP

**Description:** Moderators shall be able to apply temporary scoped restrictions to users.

**Acceptance Criteria**

- Restrictions can target specific capabilities such as discovery visibility, chat send, event join, or event create.
- Restrictions can have an expiration time.
- Restricted users are prevented from restricted actions while the restriction is active.

### FR-028 Audit Logging

**Priority:** MVP

**Description:** The system shall record an immutable audit trail for sensitive actions.

**Acceptance Criteria**

- Moderation actions write audit log entries.
- Group ownership transfer/dissolution writes audit log entries when enabled.
- Audit logs are append-only.

### FR-029 Restaurant Admin Accounts

**Priority:** MVP++

**Description:** The system shall support restaurant admin accounts that manage restaurants and slots.

**Acceptance Criteria**

- A restaurant may have multiple admins.
- A restaurant admin may manage one or more restaurants.
- Restaurant admins can create/update restaurant profiles.

### FR-030 Restaurant Slots (Create/Manage)

**Priority:** MVP++

**Description:** Restaurant admins may create availability slots with capacity and timing.

**Acceptance Criteria**

- A slot contains restaurant, start/end time window, max participants, and cutoff.
- A slot may define a minimum threshold for discount activation.
- Restaurant admins can edit/cancel slots.

### FR-031 Slot Selection and Reservation

**Priority:** MVP++

**Description:** Events may select a restaurant slot, reserving it immediately.

**Acceptance Criteria**

- An event can select a slot only if event time fits the slot window.
- Event capacity cannot exceed slot capacity.
- Selecting the slot reserves it immediately for that event.

### FR-032 Discount Threshold Activation

**Priority:** MVP++

**Description:** Slots may activate discounts once a confirmed threshold is met.

**Acceptance Criteria**

- Discount activates when confirmed participants meet/exceed the threshold before cutoff.
- Discount activation is stored as active/inactive state.
- If the threshold is not met by cutoff, discount remains inactive.

### FR-033 Restaurant Admin Controls on Slot-Linked Events

**Priority:** MVP++

**Description:** Restaurant admins may manage slot-linked event outcomes.

**Acceptance Criteria**

- Restaurant admins can cancel a slot and linked events are cancelled or forced to reselect a slot.
- Optional restaurant approval/denial flows remain disabled by default.

## 3. Non-Functional Requirements

### NFR-001 Performance

- Support at least 100 concurrent users during testing.

### NFR-002 Security

- Passwords must be securely hashed.
- Only authenticated users can create events or send messages.

### NFR-003 Privacy

- Exact home addresses must never be exposed.
- Location matching must use ZIP code or radius filtering.

### NFR-004 Usability

- Users should be able to create or join a dining event within 2 minutes.

### NFR-005 Reliability and Data Integrity

- Prevent duplicate joins.
- Ensure event status transitions are consistent and server-controlled.
- Keep capacity enforcement atomic under contention.

### NFR-006 Simplicity and No Overengineering

- The solution should remain appropriate for a capstone timeline.
- Prefer a modular monolith over microservices.
- Avoid unnecessary distributed patterns or premature optimization.

### NFR-007 Modularity and Best Practices

- The backend must be organized into clear modules such as Auth, Profiles, Restaurants, Events, Groups, Messaging, Discovery, Notifications, and Moderation.
- Business rules must live in services/domain logic, not duplicated across controllers/UI.
- Later capabilities should be addable with minimal redesign.

### NFR-008 Project Structure Constraint

- Use one backend project for the API and business logic.
- Keep boundaries internal to the monolith rather than separate deployable services.

### NFR-009 Database Separation and Stored Procedures

- Use a separate SQL database from the application runtime.
- Stored procedures may be used for complex queries/transactions when justified.
- Schema and SQL artifacts must be source-controlled and deployable.

## Appendix A: Design Decisions

### A1. Locked Design Decisions (MVP)

1. Groups and events both exist; events may optionally link to a group.
2. People discovery includes search + swipe + mutual Budz in MVP, while direct 1-on-1 messaging remains out of MVP UI.
3. Event chat and group chat are both part of MVP.
4. Event status lifecycle is server-controlled and includes cancellation plus automatic time-based completion.
5. Restaurants are stored internally with optional external PlaceId.
6. Basic query-based browse/search exists for open events and public groups in MVP.
7. Event invitations do not reserve seats.
8. Event capacity is 2 to 8 inclusive and the host counts toward capacity.

### A2. Planned or Later Decisions

1. 1-on-1 messaging is backend-ready but may stay disabled initially.
2. Event discovery may support a dedicated feed and/or cached index later.
3. Restaurants may have multiple admins and slot operations later.
4. Slots are reserved immediately upon selection.
5. Discount thresholds activate based on confirmed participants.

## Appendix C: Risk-Based Downscopes

This appendix defines safe fallback variants for higher-risk features.

### C1. Restaurants (FR-006 / FR-007)

- Default: use the internal Restaurant entity and seeded catalog with search/list selection in MVP.
- Downscope: keep filters to cuisine + price tier + distance; midpoint logic stays lightweight and optional.
- Backup: store cuisine target plus optional free-text restaurant name/address on the event and move Restaurant-heavy work later.

### C2. Notifications (FR-016)

- Default: in-app notifications only for state changes and event updates.
- Downscope: compute reminder timing on read instead of scheduling jobs.
- Backup: no reminders at all; rely on event UI plus "My events" pages.

### C3. Chat Complexity (FR-017 / FR-017A)

- Default: SignalR/WebSockets for real-time delivery plus paged history retrieval; no typing indicators, attachments, edits, or reactions.
- Backup: replace chat with comment-thread style posting if real-time delivery becomes too risky.

### C4. Groups and Invites (FR-011 to FR-013, FR-017A)

- Default: owner/member model only, public/private visibility, and basic invites by username.
- Downscope: keep ownership transfer/dissolution later even if groups remain in MVP.
- Backup: remove groups from MVP UI and keep them schema-ready only.

### C5. Moderation and Restrictions (FR-025 to FR-028)

- Default: reporting, moderation queue, scoped restrictions, and audit logging.
- Downscope: keep moderation UI minimal and restriction scopes small.
- Backup: store reports and let admins resolve manually without automated in-app restrictions.

### C6. Search and Feed (FR-022 / FR-023)

- Default: pure DB-query browse/search in MVP.
- Backup: defer feed/index/cache work entirely until needed.

### C7. People Discovery and Budz (FR-018 / FR-019 / FR-020)

- Default: username/display-name search plus a simple swipe queue.
- Mutual Like creates Budz directly in MVP; no pending Bud-request state is exposed.
- Backup: keep username search plus a later Bud request/accept workflow if swipe must be hidden.

## Appendix B: Data Model Readiness

**MVP entities**

- UserAccount
- UserProfile
- UserPreferences
- UserCuisinePreference
- UserDietaryFlag
- UserAllergy
- RecurringAvailabilityWindow
- OneOffAvailabilityWindow
- PrivacySettings
- UserBlock
- Restaurant
- Event
- EventParticipant
- Group
- GroupMember
- GroupInvite
- ChatThread
- ChatMessage
- Notification
- SwipeDecision
- BudConnection
- ModerationReport
- ModerationAction
- UserRestriction
- AuditLogEntry

**MVP+ entities**

- BudRequest
- Optional RSVP/cutoff fields on Event/EventParticipant
- Notification preference toggles if needed

**MVP++ entities**

- RestaurantAdminAssignment
- RestaurantSlot
- EventSlotReservation
- DiscountActivation
- Direct-chat scope over ChatThread/ChatMessage
- Search/feed projections as read models

## Change Log

- Normalized the requirements around the canonical TasteBudz name used by the repository.
- Promoted group chat to MVP and aligned MVP chat on SignalR plus paged history retrieval.
- Clarified Budz as reciprocal Like only in MVP, with no pending Bud-request state.
- Corrected event sizing to a typical 4-6 participants with a hard maximum capacity of 8 and no hard maximum group-member cap in MVP.
- Added explicit host event-edit and automatic event-completion rules so the requirements match the backend architecture and decision log.
- Refreshed the data model checklist so it matches the standalone backend domain model.
