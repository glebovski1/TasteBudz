# TasteMatch — Functional Requirements (FR) with Acceptance Criteria

## 0. MVP Build Checklist

Implement the following MVP items first (rough order). Each item references the owning requirement(s):

- **Account auth + sessions + first-run onboarding** (FR-001, FR-002)
- **Profile CRUD + dashboard summary + account deletion** (FR-002)
- **Preferences + availability windows** (FR-003, FR-004)
- **Privacy controls + blocking** (FR-005, FR-024)
- **Seeded Restaurant catalog + Restaurant entity** (FR-006)
- **Restaurant browse + filtering + simple suggestions** (FR-007)
- **People discovery core: search + swipe + Budz list** (FR-018, FR-019, FR-020)
- **Basic browse/search for open events and public groups** (FR-022)
- **Create Events (Open + Closed) + Closed invites (username-based)** (FR-008)
- **Join/Leave with atomic capacity enforcement + DecisionAt lock** (FR-009, FR-010)
- **Groups: create/join/leave + owner management** (FR-011, FR-012, FR-013)
- **Event status lifecycle + DecisionAt evaluation** (FR-014)
- **In-app notifications for state changes (invite/join/leave/confirmed/cancelled)** (FR-016)
- **Event-only chat (basic, non-1:1)** (FR-017)
- **Safety stack: report → moderation queue → soft bans → audit log** (FR-025, FR-026, FR-027, FR-028)

> **MVP Decisions (Locked for Capstone)**
> - **Restaurant data source (MVP):** **Seeded internal restaurant list** (no external API dependency). (FR-006/FR-007, Appendix C1)
> - **Notifications (MVP):** **State-change notifications only** (no scheduled reminders/jobs). (FR-016, Appendix C2)
> - **People discovery (MVP):** **Search + swipe + mutual Budz core are included**, while **1-on-1 messaging remains out of MVP UI**. (FR-018–FR-021, Appendix C7)
> - **Browse/search scope (MVP):** **Basic query-based browsing/search for open events and public groups is in scope**; advanced feed/caching remains a later layer. (FR-022/FR-023, Appendix C6)

## 1. System Overview

TasteMatch is a web-based social dining coordination platform that connects people who want to try restaurants together based on cuisine preferences, dietary compatibility, location proximity, and availability. The goal is to let users form small dining groups, discover compatible people, and coordinate restaurant visits quickly and safely.

For MVP UX, the product is organized around three core surfaces reflected in the prototype/design work: **Profile Creation**, **Budz and Groups**, and **Events**.

Core value flow:

User wants food → discovers Budz, a group, or an event → restaurant is suggested → participants confirm → dinner happens.

### 1.1 Roles & Permissions (MVP)

| Role | Allowed actions (MVP; non-exhaustive) |
|---|---|
| **User** | Register/login/logout (FR-001)<br>Update profile/preferences/availability/privacy (FR-002–FR-005)<br>Browse/filter restaurants (FR-007)<br>Search/swipe people and view Budz (FR-018–FR-020)<br>Browse/search open events and public groups (FR-022)<br>Join/leave **Open** events; accept/decline **Closed** invites (FR-008–FR-009)<br>Use event chat if participating (FR-017)<br>Block/report users (FR-024–FR-025) |
| **Host** | Create Open/Closed events (FR-008)<br>Invite users to Closed events (FR-008)<br>Cancel own event with reason (FR-014)<br>View participants and event details (FR-008–FR-014) |
| **Group Owner** | Create group; manage name/description/visibility (FR-011–FR-012)<br>Remove group members (FR-012)<br>Transfer ownership or dissolve group (FR-012A)<br>Create/view group-linked events (FR-013)<br>Use group chat if enabled (FR-017A) |
| **Moderator** | View report queue; resolve reports with recorded decision (FR-026)<br>Apply/expire soft bans (FR-027)<br>Actions are audit-logged (FR-028) |
| **Admin** | All Moderator actions (FR-026–FR-027)<br>Cancel events as needed (FR-014)<br>View audit log for sensitive actions (FR-028)<br>May perform support overrides (e.g., unlock participation) only when required to resolve errors or abuse cases (FR-009/FR-014) |

---

## 2. Functional Requirements Catalogue

> **Feature layers (priority legend)**
>
> - **MVP**: required for initial release (capstone demo scope)
> - **MVP+**: optional “nice-to-have” improvements if time permits (moderate effort, high UX/reliability value)
> - **MVP++**: back-end ready (data model/endpoints may exist) and feature-flagged; may be disabled or not exposed in UI initially

---

## 2.1 User Stories

### MVP User Stories

- **US-001** — As a user, I want to register an account so that I can use TasteMatch. *(Covers: FR-001)*
- **US-002** — As a user, I want to log in and log out so that my account stays secure. *(Covers: FR-001)*
- **US-003** — As a user, I want to edit my profile (bio, ZIP, goal) so that others can understand my vibe and location area. *(Covers: FR-002)*
- **US-004** — As a user, I want to manage my cuisine preferences, spice tolerance, and dietary/allergy flags so that recommendations and matches fit my needs. *(Covers: FR-003)*
- **US-005** — As a user, I want to define when I’m available so that I can find events I can actually attend. *(Covers: FR-004)*
- **US-006** — As a user, I want to control whether people can discover me so that I can manage privacy. *(Covers: FR-005)*
- **US-007** — As a user, I want to browse and filter restaurants by cuisine, price, and distance so that I can choose a practical place. *(Covers: FR-006, FR-007)*
- **US-008** — As a user, I want the app to suggest a restaurant (optionally midpoint) so that my group can decide faster. *(Covers: FR-007)*
- **US-009** — As a user, I want to create an **open event** so that people nearby can join my dining plan. *(Covers: FR-008)*
- **US-010** — As a user, I want to create a **closed event** and invite specific people so that I can plan privately. *(Covers: FR-008)*
- **US-011** — As a user, I want to join and leave events so that I can commit or change plans without breaking the system. *(Covers: FR-009, FR-010)*
- **US-012** — As a user, I want events to prevent overfilling so that capacity is respected. *(Covers: FR-010, FR-014)*
- **US-013** — As a user, I want to create and join persistent groups so that I can build recurring circles for dining. *(Covers: FR-011)*
- **US-014** — As a group owner, I want to manage group settings and members so that the group stays healthy. *(Covers: FR-012)*
- **US-015** — As a user, I want to link an event to a group so that the group can coordinate around it. *(Covers: FR-013)*
- **US-016** — As a user, I want event statuses (open/full/confirmed/cancelled) so that I know whether a dinner is happening. *(Covers: FR-014)*
- **US-018** — As a user, I want notifications so that I don’t miss important event changes. *(Covers: FR-016)*
- **US-019** — As an event participant, I want an event-only group chat so that I can coordinate logistics safely. *(Covers: FR-017)*
- **US-020** — As a user, I want to block someone so that they cannot contact or appear to me again. *(Covers: FR-024)*
- **US-021** — As a user, I want to report inappropriate behavior so that moderators can keep the community safe. *(Covers: FR-025)*
- **US-022** — As a moderator, I want a queue of reports so that I can review and resolve issues consistently. *(Covers: FR-026)*
- **US-023** — As a moderator, I want to apply temporary restrictions (soft bans) so that harmful users are limited without permanent deletion. *(Covers: FR-027)*
- **US-024** — As an admin/moderator, I want sensitive actions to be audit-logged so that decisions are traceable. *(Covers: FR-028)*
- **US-025** — As a user, I want to search people by username so that I can find someone I met. *(Covers: FR-018)*
- **US-026** — As a user, I want to swipe Like/Pass on suggested profiles so that discovery feels fast and fun. *(Covers: FR-019)*
- **US-027** — As a user, I want a mutual-like connection (Budz) so that the social layer feels explicit and trackable. *(Covers: FR-020)*
- **US-029** — As a user, I want to browse and search events and groups so that I can discover plans that match my schedule and preferences. *(Covers: FR-022)*
- **US-036** — As a user, I want my profile page to show my active events, groups, and Budz so that I can quickly understand my current activity. *(Covers: FR-002)*

### MVP+ User Stories

- **US-017** — As a user, I want RSVP confirmations and deadlines so that I can trust events won’t collapse last minute. *(Covers: FR-015)*
- **US-037** — As a group owner, I want to transfer ownership or dissolve a group so that group administration remains manageable over time. *(Covers: FR-012A)*
- **US-038** — As a group member, I want a group-only chat so that members can coordinate without sharing phone numbers. *(Covers: FR-017A)*

### MVP++ User Stories (Back-end Ready)

- **US-028** — As a connected user, I want 1-on-1 chat (if enabled) so that I can coordinate privately. *(Covers: FR-021)*
- **US-030** — As a user, I want a Tonight/This Week feed so that I can quickly find active events without searching. *(Covers: FR-023)*
- **US-031** — As a restaurant admin, I want to manage my restaurant profile so that it is accurate in the system. *(Covers: FR-029)*
- **US-032** — As a restaurant admin, I want to open time-window slots with a capacity so that groups can reserve a dining window. *(Covers: FR-030)*
- **US-033** — As a user, I want to select a restaurant slot when creating an event so that our plan aligns with restaurant availability. *(Covers: FR-031)*
- **US-034** — As a group, I want a discount to activate when enough people confirm so that we’re rewarded for organizing a larger dinner. *(Covers: FR-032)*
- **US-035** — As a restaurant admin, I want to cancel a slot and have linked events handled correctly so that capacity stays correct. *(Covers: FR-033)*

---

### FR-001 Authentication (Register / Login / Logout)

**Priority:** MVP

**Description:** The system shall allow users to create accounts and authenticate.

**Acceptance Criteria:**

- Users can register with required fields (at minimum: username/email + password); ZIP code may be collected during registration or required immediately in first-run onboarding.
- Users can log in with valid credentials.
- Users can log out and the session/token is invalidated client-side.
- Invalid credentials result in an error without revealing whether the account exists.

---

### FR-002 Account and Profile Management

**Priority:** MVP

**Description:** Users shall be able to view and update their profile.

**Acceptance Criteria:**

- Users can edit profile fields: display name/username, bio (≤ 255 chars), ZIP code, social goal.
- Users can view a personal profile page/dashboard that includes their profile information plus summaries of current events, joined groups, and Budz/connections.
- Users can delete their account.
- Users can update profile without affecting other users’ data.

---

### FR-003 Food Preferences, Dietary Flags, and Allergies

**Priority:** MVP

**Description:** Users shall be able to store cuisine preferences, spice tolerance, and dietary compatibility information.

**Acceptance Criteria:**

- Users can select one or more cuisine tags.
- Users can set a spice preference/tolerance value.
- Users can set dietary flags (e.g., vegetarian/vegan) and allergy warnings.
- Preferences are used as filters for matching/event discovery.

---

### FR-004 Availability Windows

**Priority:** MVP

**Description:** Users shall be able to define time windows when they are available for dining.

**Acceptance Criteria:**

- Users can create, edit, and delete availability windows.
- Availability windows can be used as filters for event matching and event search.

---

### FR-005 Privacy Settings

**Priority:** MVP

**Description:** Users shall be able to control basic discovery/contact visibility.

**Acceptance Criteria:**

- Users can disable discovery (hidden from people discovery/search).
- Users can block other users (see FR-024).

---

### FR-006 Restaurant Entity with Optional External PlaceId

**Priority:** MVP

**Description:** The system shall store restaurants internally and may link them to an external provider identifier. *(Downscope/backup options: Appendix C1).*

**Acceptance Criteria:**

- Restaurants are stored with: name, location, cuisine tags, price tier.
- A restaurant may optionally store an external **PlaceId** (e.g., Google PlaceId).
- Restaurant records can be referenced by Events and Restaurant Slots.

---

### FR-007 Restaurant Discovery and Filtering

**Priority:** MVP

**Description:** Users shall be able to browse/search restaurants and apply basic filters. *(Downscope/backup options: Appendix C1).*

**Acceptance Criteria:**
- Users can filter restaurants by cuisine, price tier ($/$$/$$$), and distance. **[MVP]**
- Dietary flags/allergies are used for **people/event compatibility** and do not require restaurant dietary metadata in MVP. **[MVP]**
- The system shall suggest restaurants using the **seeded internal restaurant list** (MVP) and may additionally use a lightweight external search (e.g., Google Places) when enabled. **[MVP/MVP++]**
- Restaurant selection may be reused during event creation and may be presented in list and/or map form when location coordinates are available. **[MVP/MVP+]**
- Restaurant suggestions may support a midpoint suggestion approach for group members. **[MVP+]**

---

### FR-008 Create Events (Open and Closed)

**Priority:** MVP

**Description:** Users shall be able to create dining events.

**Acceptance Criteria:**

- An Event includes: optional title/name, time/date, capacity, event type (Open/Closed), and either a selected restaurant or cuisine target.
- Open events are discoverable/joinable by eligible users.
- Closed events are invite-only.
- Closed events allow invite acceptance until `DecisionAt` (MVP); optional configurable invite cutoff `CutoffAt` (default `EventStartAt - 24h`) when FR-015 is enabled (MVP+).

#### MVP Closed Event Invite Flow (Closed events only)

- **Host creates** an event with `EventType = Closed`, `Capacity`, and `EventStartAt` (and restaurant or cuisine target).
- **Host selects invitees by exact username** (must match an existing user account).
- The system creates (or updates) an **EventParticipant** record per invitee with state `INVITED` and generates `EVENT_INVITE_RECEIVED` for each.
- Invitees **accept** (state transitions to `JOINED`) or **decline** (state transitions to `DECLINED`) **until `DecisionAt`**.
- Capacity is enforced on **accept/join**: invites do **not** reserve seats. If accepting would exceed `Capacity`, the action is rejected (“event full”).
- At `DecisionAt`, the system blocks further accept/decline/join/leave changes (except Admin support override) and applies the FR-014 decision rule to **confirm or cancel** the event.

---

### FR-009 Event Participation (Join / Leave)

**Priority:** MVP

**Description:** Eligible users shall be able to join or leave events.

**Acceptance Criteria:**

- Joining an event creates a participant record.
- Leaving an event removes participation (or marks as left) and updates capacity.
- Duplicate joins are prevented (idempotent join).
- **Event capacity shall be enforced under concurrent joins.**
- The system shall guarantee that **ActiveParticipants never exceeds Capacity**, even under simultaneous join requests.
- The join operation shall use a **transactional or atomic mechanism** (e.g., database transaction/locking strategy, constraint-based guard, or equivalent safeguard).
- After `DecisionAt`, joining and leaving are blocked except Admin support override.

---

### FR-010 Group Size Defaults and Limits

**Priority:** MVP

**Description:** The system shall support small group coordination with defined defaults and caps.

**Acceptance Criteria:**

- Typical recommended group size is **4–6**.
- The maximum event capacity is **12**.
- The system prevents joining beyond the capacity.

---

### FR-011 Persistent Groups (Create / Join / Leave)

**Priority:** MVP

**Description:** The system shall support persistent Groups (in addition to event-based groups). *(Downscope/backup options: Appendix C4).*

**Acceptance Criteria:**

- Users can create a Group with name and short description.
- Groups have visibility: **Public** (discoverable) or **Private** (invite-only).
- Users can join/leave Groups (if allowed by visibility/invite rules).
- Group members can view basic group details and the current member list.

---

### FR-012 Group Roles

**Priority:** MVP

**Description:** Groups shall have basic roles for management. *(Downscope/backup options: Appendix C4).*

**Acceptance Criteria:**

- Each Group has an **Owner**.
- Owners can manage group settings (name/description/visibility).
- Owners can remove members.
- Owners can access a management view that lists current members and available owner actions.

---

### FR-012A Group Ownership Transfer and Dissolution

**Priority:** MVP+

**Description:** Groups may support explicit ownership transfer and dissolution actions for long-term maintainability. *(Downscope/backup options: Appendix C4).*

**Acceptance Criteria:**

- A Group Owner can transfer ownership to another current group member.
- A Group Owner can dissolve/delete a group with explicit confirmation.
- Ownership transfer and dissolution update membership/discoverability state consistently and are timestamped.

---

### FR-013 Link Events to Groups

**Priority:** MVP

**Description:** Events may optionally be associated with a Group. *(Downscope/backup options: Appendix C4).*

**Acceptance Criteria:**

- An event may store an optional GroupId.
- Group members can view group-linked events via group context.

---

### FR-014 Event Status Lifecycle

**Priority:** MVP

#### MVP Summary

- Event status is **server-controlled**; clients cannot set status directly.
- `DecisionAt` determines whether the event proceeds and locks participation changes (default: Open = `EventStartAt - 15m`, Closed = `EventStartAt - 24h`, configurable).
- `OPEN`/`FULL` reflect current capacity; at `DecisionAt` the event becomes `CONFIRMED` or `CANCELLED`.
- Joining/leaving is allowed only **before `DecisionAt`** (except Admin support override).
- `CANCELLED` and `COMPLETED` are terminal statuses.

**Description:** Each event shall follow a server-controlled status lifecycle that reflects capacity and ensures events do not occur if there are not enough participants.

#### Mini Glossary (Event Lifecycle)

- **DecisionAt** — The datetime when the system evaluates whether the event should proceed and locks participation changes.
  - Default: **Open events** = `EventStartAt - 15m`
  - Default: **Closed events** = `EventStartAt - 24h` (configurable)
  - After `DecisionAt`, join/leave and invite accept/decline are blocked except Admin support override.
- **CutoffAt** — RSVP/invite cutoff datetime **only when FR-015 is enabled (MVP+)** (default `EventStartAt - 24h`). In MVP, “cutoff” refers to `DecisionAt`.
- **ActiveParticipants** — The participant count used for capacity/decision rules:
  - In **FR-014 (MVP)**: count of participants in state `JOINED` (excluding `LEFT/REMOVED/DECLINED`).
  - In **FR-015 (MVP+)**: expands as defined in FR-015.
- **Canonical EventStatus values (stored)** — `OPEN`, `FULL`, `CONFIRMED`, `CANCELLED`, `COMPLETED`.
  - UI may label **CONFIRMED** as “Locked/Closed” for readability, but stored value remains `CONFIRMED`.


#### Definitions

- `EventStartAt` = scheduled start date/time.
- `DecisionAt` = time when the system determines whether the event should proceed (defaults: Open = `EventStartAt - 15m`, Closed = `EventStartAt - 24h`; configurable).
- `Capacity` = max participants allowed.
- `MinParticipantsToRun` = minimum participants required at `DecisionAt` (default **2**).
- `ActiveParticipants` = count of participants in state `JOINED` (excluding LEFT/REMOVED).
- If FR-015 reliability controls are enabled, participant-state metrics may expand as defined in FR-015.

#### Statuses

`OPEN`, `FULL`, `CONFIRMED`, `CANCELLED`, `COMPLETED`

#### Transition rules (server-controlled)

| From | To | Trigger |
|---|---|---|
| OPEN | FULL | Active participants count reaches `Capacity` |
| FULL | OPEN | Active participants drops below `Capacity` |
| OPEN/FULL | CANCELLED | Host/admin cancels OR slot cancelled (if slot-linked) |
| OPEN/FULL | CONFIRMED | At `DecisionAt`, `ActiveParticipants >= MinParticipantsToRun` |
| OPEN/FULL | CANCELLED | At `DecisionAt`, `ActiveParticipants < MinParticipantsToRun` |
| CONFIRMED | COMPLETED | After `EventStartAt + GracePeriod` (default **6h**) OR host marks completed |

#### Invariants

- `CANCELLED` and `COMPLETED` are terminal.
- Status updates must be executed server-side only (clients cannot set status directly).

**Acceptance Criteria:**

- Event supports statuses: OPEN, FULL, CONFIRMED, CANCELLED, COMPLETED. **[MVP]**
- Server updates status deterministically based on rules above. **[MVP]**
- Joining/leaving updates status between OPEN/FULL correctly and safely under concurrency. **[MVP]**
- Event creator (host) can cancel an event; cancellation records a reason and timestamps. **[MVP]**
- Host can manually mark COMPLETED (otherwise automatic completion based on time rule). **[MVP+]**
- If slot-linked, slot cancellation forces event to CANCELLED (default), or may require reselection if introduced later. **[MVP++]**

---
### FR-015 Advanced Reliability Controls (RSVP, Cutoffs, Auto-Cancel)

**Priority:** MVP+

#### MVP+ Summary

- Adds `CutoffAt` (invite/RSVP deadline), optional per-event thresholds, and RSVP participant states.
- After `CutoffAt`, joining and RSVP changes are blocked except Admin support override.
- At `CutoffAt`, the system auto-**CONFIRMS** or auto-**CANCELS** based on `ConfirmedCount`.
- Capacity enforcement remains atomic under concurrency.
- This layer is optional; MVP behavior uses `DecisionAt` rules from FR-014.

**Description:** The system may introduce advanced reliability controls to reduce last-minute collapses by requiring confirmations, enforcing RSVP deadlines, and optionally auto-cancelling events that lack minimum confirmed participants.

#### Participant states (per event)

A participant record has one of:

`INVITED`, `JOINED`, `CONFIRMED`, `DECLINED`, `LEFT`, `REMOVED`

- **ActiveParticipants** = `INVITED + JOINED + CONFIRMED` (excluding LEFT/REMOVED)
- **ConfirmedCount** = count of `CONFIRMED` only

#### Optional event fields (when reliability controls are enabled)

- `CutoffAt` = RSVP/invite cutoff datetime (default `EventStartAt - 24h`, allowed range: **2h–72h**)
- `MinConfirmedToRun` = minimum confirmed participants required at cutoff (default **3**, allowed range: **2–6**)
- `RSVPRequired` = whether RSVP confirmation is required (default `true` for Open events)

#### Optional rules (when enabled)

1) **Join rules**

- Joining creates or reactivates a participant record (idempotent).
- If `now >= CutoffAt`, joining is blocked (returns “join closed”).
- If capacity full, joining is blocked (returns “event full”).

2) **RSVP rules**

- Participants can set RSVP to `CONFIRMED` or `DECLINED` once invited/joined.
- After `CutoffAt`, RSVP changes are blocked except by Admin support override.

3) **Auto-cancel / auto-confirm rule**

- At `CutoffAt`, if `ConfirmedCount < MinConfirmedToRun`, event becomes `CANCELLED` with reason `INSUFFICIENT_CONFIRMATIONS`.
- At `CutoffAt`, if `ConfirmedCount >= MinConfirmedToRun`, event becomes `CONFIRMED`.

4) **Seat computations**

- System displays: `SeatsFilled = ActiveParticipantsCount`, `SeatsRemaining = Capacity - ActiveParticipantsCount`.

**Acceptance Criteria:**

- System may store cutoff time, RSVP requirement, and min-confirmed threshold per event. **[MVP+]**
- System may track RSVP per participant with the state model above. **[MVP+]**
- Auto-cancel/confirm may occur at cutoff time based on confirmed count. **[MVP+]**
- Join/leave operations remain safe under concurrency (capacity cannot be exceeded). **[MVP+]**
- Admin support override may extend cutoff (within allowed range) and/or manually cancel/confirm. **[MVP++]**
- Optional: no-show scoring/reputation is out of scope unless defined later. **[MVP++]**

---
### FR-016 Notifications and Reminders (Tight)

**Priority:** MVP

#### MVP Summary

- Provide an **in-app notification center** backed by persisted notification records.
- Generate notifications on **state changes** (invite received, joined/left, confirmed/cancelled).
- **No scheduled reminders/jobs in MVP**; clients can display upcoming `DecisionAt`/start countdowns using event timestamps.
- Email and push are optional, deferred layers.

**Description:** The system shall notify users about event activity and state changes. Notifications shall be persisted and user read-state tracked. Scheduled reminders may be added in later layers. *(Downscope/backup options: Appendix C2).*

#### Cutoff terminology (MVP vs MVP+)

- In MVP, “cutoff” refers to `DecisionAt` (see FR-014).
- If FR-015 is enabled (MVP+), “cutoff” refers to `CutoffAt`.

#### Notification channels by layer

- **MVP:** in-app notification center only (persisted; UI reads from API)
- **MVP+:** optional email notifications (transactional)
- **MVP++:** push notifications (APNs/FCM) feature-flagged

#### Notification types (minimum set)

- `EVENT_INVITE_RECEIVED` **[MVP]**
- `EVENT_JOINED` / `EVENT_LEFT` **[MVP]**
- `EVENT_CONFIRMED` / `EVENT_CANCELLED` **[MVP]**
- `RSVP_REQUESTED` **[MVP+]** (only when FR-015 is enabled)
- `CUTOFF_REMINDER` **[MVP+]** (only if scheduled reminders are implemented)
- `EVENT_REMINDER` **[MVP+]** (only if scheduled reminders are implemented)

#### Scheduling defaults (configurable)

- **MVP:** No scheduled reminders (see **MVP Decisions**). Countdown messaging is computed/displayed by the client from `DecisionAt` / `EventStartAt`.
- **MVP+:** If scheduled reminders are implemented:
  - `CutoffReminderOffset = 2h before CutoffAt` (fallback to **15m** if too soon)
  - `EventReminderOffset = 2h before EventStartAt`
  - Optional second reminder `30m before EventStartAt` **[MVP++]**

#### Delivery state model

Each notification has:

- `CreatedAt`, `UserId`, `Type`, `Payload`, `ReadAt?`
- `DeliveryState`:
  - MVP: `PERSISTED` only (delivered via API fetch)
  - MVP+: `QUEUED/SENT/FAILED` for email

**Acceptance Criteria:**

- Notifications are persisted in the database and retrievable via API. **[MVP]**
- Users can mark notifications as read. **[MVP]**
- System generates notifications for: invite received, joined/left, confirmed/cancelled. **[MVP]**
- System exposes event timestamps (including `DecisionAt`) so clients can display upcoming cutoff/start timing without scheduled reminders. **[MVP]**
- If FR-015 is enabled, system generates `RSVP_REQUESTED` and CutoffAt-based reminders. **[MVP+]**
- If scheduled reminders are implemented, they should be triggered by a server-side background job runner and be idempotent (no duplicate sends). **[MVP+]**
- Users can opt out of non-critical reminders (cannot opt out of confirmed/cancelled). **[MVP+]**
- Email delivery is optional and feature-flagged. **[MVP+]**
- Push notifications are feature-flagged and may be deferred. **[MVP++]**

---

### FR-017 Event Chat (Event-Only)

**Priority:** MVP

**Description:** Event participants shall be able to communicate in an event-linked group chat. *(Downscope/backup options: Appendix C3).*

**Acceptance Criteria:**

- Each Event has an associated chat thread.
- Only event participants can read/write messages.
- Messages are stored with sender + timestamps.
- **No 1-on-1 messaging in MVP UI**.

---

### FR-017A Group Chat (Group-Only)

**Priority:** MVP+

**Description:** Group members may be able to communicate in a group-linked chat thread without sharing phone numbers. *(Downscope/backup options: Appendix C3/C4).*

**Acceptance Criteria:**

- A Group may have an associated chat thread.
- Only current group members can read/write messages.
- Messages are stored with sender + timestamps.
- MVP+ delivery may use the same basic text-only/polling model as event chat.

---

### FR-018 People Discovery (Search)

**Priority:** MVP

**Description:** Users shall be able to discover other users via search.

**Acceptance Criteria:**

- Users can search by username/display name.
- Discovery displays only a limited public profile preview.
- Search respects privacy settings and blocking rules.
- Users can block/report from discovery.

---

### FR-019 People Discovery (Swipe / Like / Pass)

**Priority:** MVP

**Description:** Users shall be able to discover people through a swipe-based flow.

**Acceptance Criteria:**

- The system can present candidate profiles in a swipe queue.
- Users can Like or Pass.
- Swipe decisions are stored.
- Mutual Like produces a Budz/connection outcome as defined by FR-020.

---

### FR-020 Mutual Connections (“Budz”) and Requests

**Priority:** MVP

**Description:** The system shall support a Budz connection model for the social layer.

**Acceptance Criteria:**

- The system stores connection requests and final Budz connections.
- Connections are **mutual** (both users agree or match).
- Users can view a list of their current Budz/connections.
- Users can view pending Bud requests or pending connection state where applicable.

---

### FR-021 1-on-1 Messaging (Back-end Ready, Feature-Flagged)

**Priority:** MVP++

**Description:** The backend may implement 1-on-1 messaging structures/endpoints, but the feature may remain disabled in production until enabled.

**Acceptance Criteria:**

- 1-on-1 chats exist as separate threads/messages.
- 1-on-1 is allowed only between mutual connections or via request/accept.
- A feature flag can disable creation/sending in production.
- Block/report and moderation policies apply.

---

### FR-022 Event and Group Browse/Search

**Priority:** MVP

**Description:** Users shall be able to browse and search open events and public groups using basic query-based filters. *(Downscope/backup options: Appendix C6).*

**Acceptance Criteria:**

- Users can browse open events that match cuisine, time window, distance, price tier, and availability filters.
- Users can filter events by status.
- Users can browse/search public groups by name/visibility.
- MVP implementation may be simple database queries without a dedicated cached feed.

---

### FR-023 Feed Support (“Tonight / This Week”) — Both Approaches

**Priority:** MVP++

**Description:** The system shall support both query-based feeds and optional cached feeds. *(Downscope/backup options: Appendix C6).*

**Acceptance Criteria:**

- Feed can be generated as filtered queries over events.
- The backend may maintain an optional cached/indexed feed for performance.

---

### FR-024 Block Users

**Priority:** MVP

**Description:** Users shall be able to block other users.

**Acceptance Criteria:**

- Blocked users cannot message the blocker.
- Blocked users do not appear in discovery for the blocker.
- Blocking is reversible by the blocker.

---

### FR-025 Report Users/Content

**Priority:** MVP

**Description:** Users shall be able to report inappropriate behavior or content. *(Downscope/backup options: Appendix C5).*

**Acceptance Criteria:**

- Users can submit a report with a reason/category.
- Reports can target a user and may optionally target an event or message.
- Reports are stored and accessible to moderators.

---

### FR-026 Moderation Workflow

**Priority:** MVP

**Description:** Moderators shall be able to review reports and take actions. *(Downscope/backup options: Appendix C5).*

**Acceptance Criteria:**

- Moderators can view reports in a review queue.
- Moderators can resolve reports with a recorded decision.
- Moderation actions are stored.

---

### FR-027 Soft Bans

**Priority:** MVP

**Description:** Moderators shall be able to apply temporary restrictions to users. *(Downscope/backup options: Appendix C5).*

**Acceptance Criteria:**

- A soft ban can restrict posting/chat/event participation (scope-based).
- Soft bans can have an expiration time (default: 7 days).
- Soft-banned users are prevented from restricted actions.

---

### FR-028 Audit Logging (Sensitive Actions)

**Priority:** MVP

**Description:** The system shall record an immutable audit trail for sensitive actions. *(Downscope/backup options: Appendix C5).*

**Acceptance Criteria:**

- Moderation actions shall write an audit log entry.
- Restaurant slot cancellations/discount changes (if enabled) shall write audit log entries.
- Audit logs shall be append-only (immutable once written).

---

### FR-029 Restaurant Admin Accounts

**Priority:** MVP++

**Description:** The system shall support Restaurant Admin accounts that manage restaurants and slots.

**Acceptance Criteria:**

- A restaurant may have multiple admins.
- A restaurant admin may manage one or more restaurants.
- Restaurant admins can create/update restaurant profiles.

---

### FR-030 Restaurant Slots (Create/Manage)

**Priority:** MVP++

**Description:** Restaurant admins may create availability slots with capacity and timing.

**Acceptance Criteria:**

- A slot contains: restaurant, start/end time window, max participants, cutoff.
- A slot may define a minimum threshold to unlock a discount.
- Restaurant admins can edit/cancel slots.

---

### FR-031 Slot Selection and Reservation

**Priority:** MVP++

**Description:** Events may select a restaurant slot, reserving it immediately.

**Acceptance Criteria:**

- An event can select a slot only if the event time fits the slot window.
- Event capacity cannot exceed slot capacity.
- Selecting a slot reserves it immediately for that event.

---

### FR-032 Discount Threshold Activation

**Priority:** MVP++

**Description:** Slots may activate discounts once a confirmed threshold is met.

**Acceptance Criteria:**

- Discount activates when **confirmed** participants meet/exceed threshold before cutoff.
- Discount activation is stored as a state (active/inactive) for the event.
- If threshold is not met by cutoff, discount remains inactive.

---

### FR-033 Restaurant Admin Controls on Slot-Linked Events

**Priority:** MVP++

**Description:** Restaurant admins may manage slot-linked event outcomes.

**Acceptance Criteria:**

- Restaurant admin can cancel a slot; linked events are cancelled or forced to reselect a slot.
- Optional: restaurant admin may approve/deny slot-linked events (off by default).

---

## 3. Non-Functional Requirements

### NFR-001 Performance

- Support at least **100 concurrent users** during testing.

### NFR-002 Security

- Passwords must be securely hashed.
- Only authenticated users can create events or send messages.

### NFR-003 Privacy

- Exact home addresses must never be exposed.
- Location matching must use ZIP code or radius filtering.

### NFR-004 Usability

- Users should be able to create or join a dining event within **2 minutes**.

### NFR-005 Reliability and Data Integrity

- Prevent duplicate joins (unique participant constraint).
- Ensure event status transitions are consistent and server-controlled.

### NFR-006 Simplicity and No Overengineering

- The solution should remain **relatively simple** and appropriate for a capstone timeline.
- Prefer a **modular monolith** over microservices.
- Avoid unnecessary frameworks, complex distributed patterns, or premature optimizations.

### NFR-007 Modularity, Scalability, and Best Practices

- The backend must be organized into clear modules (folders/namespaces) with separation of concerns (e.g., Identity, Profiles, Events, Groups, Messaging, Restaurants, Moderation, Notifications).
- Business rules must be implemented in services/domain logic (not duplicated across controllers/UI).
- New features should be addable with minimal changes to existing modules (extension-friendly design).
- The system should support feature flags for MVP++ capabilities (e.g., 1-on-1 messaging, push notifications).

### NFR-008 Project Structure Constraint

- Use **one backend project** (organized by folders/namespaces) for the API and business logic.
- Use **one frontend project** (technology TBD).
- The design should keep boundaries internal (modules), not separate deployable services.

### NFR-009 Database Separation and Stored Procedures

- The system shall use a **separate SQL database** (e.g., SQL Server/Azure SQL) from the application runtime.
- Stored procedures may be used for performance-critical operations or complex queries/transactions.
- Stored procedures and schema changes must be versioned (source-controlled) and deployable.
- Data-access must follow best practices: parameterized queries, transactional integrity for multi-step operations, and least-privilege DB access.

---

## Appendix A: Design Decisions

### A1. Locked Design Decisions (MVP)

1. Groups and Events both exist; Events may optionally link to a Group.
2. People discovery includes **search + swipe + mutual Budz** in MVP, while **no 1-on-1 messaging appears in MVP UI**.
3. Event chat is event-only in MVP; group chat is a controlled extension layer.
4. Event status lifecycle includes cancellation and server-side transitions.
5. Restaurants are stored internally with optional external PlaceId.
6. Basic query-based browse/search exists for open events and public groups in MVP.

### A2. Planned (MVP++) Decisions (Feature-Flagged / Back-end Ready)

1. 1-on-1 messaging is back-end ready but may be disabled in production initially.
2. Event discovery may support a dedicated Tonight/This Week feed and/or cached index.
3. Restaurants may have multiple admins; admins may manage multiple restaurants.
4. Slots are reserved immediately upon selection.
5. Discount thresholds activate based on confirmed participants.

---

## Appendix C: Risk-Based Downscopes (Backup Simpler Requirements)

This appendix defines **safe “Plan B” requirement variants** for features that tend to become high-risk in a capstone timeline. If time/integration issues occur, apply the downscope without breaking the core product flow.

### C1. Restaurants: External integration + midpoint suggestion (FR-006 / FR-007)

**Why risky:** external API keys/billing, rate limits, unpredictable data, geospatial edge cases.

**Default (current requirement):** internal Restaurant entity + optional PlaceId; discovery supports filters + optional midpoint suggestion; may use Google Places.

**Downscope (keep it Medium risk) — MVP/MVP+ compatible**
- **MVP:** Use an **internal seeded restaurant catalog only** (import CSV/manual admin seeding).
- **MVP:** Filters limited to **cuisine + price tier + distance** (distance can be ZIP-to-ZIP or simple lat/long if stored).
- **MVP+:** Midpoint suggestion becomes **“closest-to-group average”** using ZIP centroids (no full geo routing).
- **MVP+:** PlaceId stored but **not required** for operation.

**Backup (simplest) — if restaurant data becomes a blocker**
- **MVP fallback:** Event stores **cuisine target + optional free-text restaurant name/address link** (no Restaurant entity required for MVP UI).
- Restaurants can be added later; event can still proceed using “chosen place text”.

**What changes in FR text (if you apply backup):**
- FR-006 becomes **MVP++** (schema-ready only).
- FR-007 MVP acceptance removes external search + midpoint and becomes “list from seeded catalog OR free-text selection.”

---

### C2. Notifications + reminders (FR-016)

**Why risky:** background scheduling reliability, time zones, duplicates.

**Terminology note:** In MVP, “cutoff” refers to `DecisionAt` (FR-014). If FR-015 is enabled (MVP+), “cutoff” refers to `CutoffAt`.

**Downscope (Medium risk)**
- **MVP:** In-app notifications only for **state changes**: invite received, joined/left, confirmed/cancelled.
- **MVP:** Reminders are **computed on read** (event page shows “Cutoff in X hours”) and optionally emitted by a **single periodic job** (e.g., runs every 5–15 minutes).
- **MVP+:** Add one reminder type only: **cutoff reminder** (drop “event reminder”).

**Backup (simplest)**
- **MVP fallback:** No scheduled reminders. Only:
  - status changes shown in event UI,
  - “My events” page highlights upcoming cutoff/start.

---

### C3. Chat complexity (FR-017 / FR-017A)

**Why risky:** real-time transport (SignalR/WebSockets), auth, reconnect, scaling.

**Downscope (Medium risk)**
- **MVP / MVP+:** Chat is **HTTP-based** (polling/refresh) with pagination; no real-time typing/online indicators.
- **MVP / MVP+:** Limit message features: **text-only**, no attachments, no edits, no reactions.

**Backup (simplest)**
- **MVP fallback:** Replace chat with an **Event Comments** thread:
  - participants can post,
  - newest-first list,
  - no live updates.
- **MVP+ fallback:** Group chat can use the same comments/thread model or remain hidden until later.

---

### C4. Groups + invites + role rules (FR-011 / FR-012 / FR-012A / FR-013 / FR-017A)

**Why risky:** permissions/visibility rules and invite edge cases across many screens.

**Downscope (Medium risk)**
- **MVP:** Keep Groups but simplify:
  - **Owner only** role (no admins/moderators),
  - visibility only affects **discoverability**, not complex permission trees.
- **MVP:** Group invites are **basic** (invite by username; no shareable links).
- **MVP+:** Ownership transfer/dissolution may be limited to a simple confirmation flow; group chat may remain basic text-only.

**Backup (simplest)**
- **MVP fallback:** Remove persistent groups from MVP UI.
  - Events still exist.
  - Users can create events as open/closed.
  - “Group” becomes a **Phase/MVP++ concept** (schema-ready only).

**What changes in FR text (if you apply backup):**
- FR-011–FR-013 and FR-012A/FR-017A move to **MVP+ or MVP++**.
- US-013–US-015 and any group-extension stories move out of MVP.

---

### C5. Moderation + soft bans + audit logging (FR-025–FR-028)

**Why risky:** cross-cutting enforcement and policy consistency.

**Downscope (Medium risk)**
- **MVP:** Reporting exists; moderation UI is **minimal list + resolve**.
- **MVP:** Soft ban is a **single restriction flag**: `CanCreateEvents/CanChat` with expiration.
- **MVP:** Audit log only records **moderation actions** (not every admin change).

**Backup (simplest)**
- **MVP fallback:** Reports are stored and viewable by admins, but **no in-app bans**.
  - Admin resolution is “mark reviewed”; enforcement is manual.

---

### C6. Search/feed (FR-022 / FR-023)

**Why risky (if overbuilt):** caching/indexing adds complexity.

**Downscope (Medium risk)**
- **MVP:** Implement browse/search as **pure DB queries only**.
- **MVP++:** Do **not** build EventFeedCache until performance requires it.

---

### C7. People discovery + Budz (FR-018 / FR-019 / FR-020)

**Why risky:** recommendation quality, moderation edge cases, and network-effect pressure can encourage overbuilding.

**Downscope (Medium risk)**
- **MVP:** Keep people discovery to **username/display-name search + a simple swipe queue**.
- **MVP:** Mutual Like creates a Budz connection; pending Bud request state can remain minimal/read-only if needed.
- Avoid complex ranking, compatibility scoring, or reputation logic in the first release.

**Backup (simplest)**
- **MVP fallback:** Keep **username search + Bud request/accept only**, and hide swipe until later without breaking the social graph.

---

## Appendix B: Data Model Readiness (Entity Checklist)

**MVP entities:** UserAccount, UserProfile, UserPreferences, UserAllergies, AvailabilityWindow, PrivacySettings, Restaurant, Event, EventParticipant, ChatThread, ChatMessage, Notification, UserReport, ModerationAction, UserBan, AuditLog, Group, GroupMember, GroupInvite, UserDiscoveryIndex, SwipeDecision, MutualMatch, FriendRequest, Friendship.

**MVP++ entities:** DirectChatThread, DirectChatMessage, MessageRequest, EventSearchIndex, GroupSearchIndex, EventFeedCache, RestaurantAdminAccount, RestaurantSlot, SlotEventLink, DiscountRule, DiscountActivation.

## Change Log

- Synced the requirements with the prototype/design document so that all design-defined feature areas now exist explicitly in the FR set.
- Promoted core design-driven features into priority scope: **People Discovery/Search**, **Swipe/Like/Pass**, **Budz**, and **basic Event/Group browse/search**.
- Added profile/dashboard language so the profile page now explicitly includes summaries for active events, groups, and Budz.
- Added missing design-level details: **spice preference**, **optional event title/name**, and reuse of restaurant discovery during event creation (including optional map/list presentation).
- Added **FR-012A Group Ownership Transfer and Dissolution**.
- Added **FR-017A Group Chat (Group-Only)** as a controlled extension aligned to the design intent.
- Updated locked design decisions, downscope appendix, and entity checklist so priority changes remain internally consistent.
- Preserved the original structure and tone while tightening wording and improving cross-section consistency.
