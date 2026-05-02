# TasteBudz - Functional Requirements (FR) with Acceptance Criteria

## 0. MVP Build Checklist

Implement the following MVP items first. Each item references the owning requirement(s):

- Account auth + sessions + first-run onboarding (FR-001, FR-002)
- Profile CRUD + dashboard summary + account deletion (FR-002)
- Database-backed media assets for profile avatars and report evidence (FR-002, FR-025)
- Preferences + availability windows (FR-003, FR-004)
- Privacy controls + blocking (FR-005, FR-024)
- Seeded restaurant catalog + Restaurant entity (FR-006)
- Restaurant browse + filtering + simple suggestions (FR-007)
- People discovery core: search + swipe + Budz list (FR-018, FR-019, FR-020)
- Basic browse/search for visible events plus public groups, with Quick Search limited to active open events that still have seats (FR-022)
- Create events (Open + Closed) + event invites by username (FR-008)
- Join/leave with atomic capacity enforcement + DecisionAt lock (FR-009, FR-010)
- Groups: create/join/leave + owner management (FR-011, FR-012, FR-013)
- Event status lifecycle + DecisionAt evaluation (FR-014)
- In-app notifications for state changes and material event updates (FR-016)
- Event + group + support chat, real-time and text-only (FR-017, FR-017A, FR-017B)
- Completed-event feedback with ratings, text, and optional photos (FR-017C)
- Safety stack: report -> moderation queue -> scoped soft bans -> audit log (FR-025, FR-026, FR-027, FR-028)

> MVP decisions locked for the capstone:
> - Restaurants use the internal catalog for MVP user-facing discovery; admin-only catalog import may populate it from OpenStreetMap/Overpass without making user browse/search depend on a live external API.
> - Notifications are in-app only in MVP; no scheduled reminder jobs are required.
> - People discovery in MVP includes search + swipe + mutual Budz; direct 1-on-1 messaging is feature-flagged and remains outside the default MVP UI.
> - Basic query-based browse/search for open events and public groups is in scope; richer feed/caching is later.
> - Event chat, group chat, and user-to-admin support chat are in scope for MVP and share the same basic messaging core.

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
| User | Register/login/logout and submit or complete password reset flows (FR-001), update profile/preferences/availability/privacy (FR-002 to FR-005), browse/filter restaurants (FR-007), search/swipe people and view Budz (FR-018 to FR-020), browse/search open events and public groups (FR-022), join/leave Open events and accept/decline event invites (FR-008 to FR-009), use event chat when participating, group chat when a current member, and support chat with admins (FR-017 to FR-017B), block/report users (FR-024 to FR-025) |
| Host | Create Open/Closed events (FR-008), invite users to events (FR-008), edit event details before cancellation/completion (FR-014), cancel own event with reason (FR-014), view participants and event details (FR-008 to FR-014) |
| Group Owner | Create group, manage name/description/visibility (FR-011 to FR-012), remove group members (FR-012), transfer ownership or dissolve group later (FR-012A), create/view group-linked events (FR-013), use group chat (FR-017A) |
| Moderator | View report queue, search moderation-relevant users/messages/content, resolve reports, apply/expire scoped restrictions, and rely on audit logging (FR-026 to FR-028) |
| Admin | All Moderator actions plus support chat replies, password reset-request review, password reset-token issuance, user account soft/permanent deletion controls, support overrides for safety/correctness cases, event cancellation support, and audit-log review (FR-001, FR-002, FR-014, FR-017B, FR-026 to FR-028) |

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
- US-020A: As an event participant, I want to leave feedback after a completed event.
- US-021: As a user, I want to block someone.
- US-022: As a user, I want to report inappropriate behavior.
- US-023: As a moderator, I want a queue of reports to review.
- US-024: As an admin/moderator, I want sensitive actions to be audit-logged.
- US-024A: As an admin/moderator, I want to search users, messages, reports, and content so I can quickly review safety issues.
- US-025: As a user, I want to search people by username/display name.
- US-026: As a user, I want to Like/Pass discovery candidates quickly.
- US-027: As a user, I want mutual Like to create a Budz connection.
- US-029: As a user, I want to browse and search events and groups.
- US-036: As a user, I want my profile page to show active events, groups, and Budz.
- US-037: As a user, I want to message admins through Help/Support.
- US-038: As an admin, I want to issue a password reset link so a user can choose a new password.

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
- Users can submit a password reset request by providing a username and message for admin review.
- Password reset request submission returns the same generic accepted response whether or not the username matches an active account.
- Admins can review or dismiss open password reset requests.
- Admins can generate a one-time password reset link for an active user.
- A user who opens a valid reset link must create a new password before signing in again.
- Successful password reset invalidates existing sessions for that user.

### FR-002 Account and Profile Management

**Priority:** MVP

**Description:** Users shall be able to view and update their profile.

**Acceptance Criteria**

- Users can edit profile fields including display name/username, public profile note (`bio`), ZIP code, and social goal.
- Users can upload or replace one profile avatar image stored in the application database.
- Users can view a personal dashboard with profile info plus My Events, groups, and Budz.
- Users can request account deletion.
- Admins can soft-delete another user's account, which revokes sessions and prevents future authentication.
- Admins can permanently delete a soft-deleted account only after entering an exact `delete` confirmation and only when the account has no historical records that must be preserved.
- Profile changes only affect the current user's data.
- Public people cards and profile previews may surface the user's personality note (`bio`), social goal, cuisine tags, and dietary flags; allergies and availability remain non-public by default.

### FR-003 Food Preferences, Dietary Flags, and Allergies

**Priority:** MVP

**Description:** Users shall be able to store cuisine preferences, spice tolerance, and dietary compatibility information.

**Acceptance Criteria**

- Users can select one or more cuisine tags.
- Users can set spice tolerance.
- Users can set dietary flags and allergy warnings.
- Preferences are available for matching and discovery filters.
- Cuisine tags and dietary flags may also be surfaced on public people cards and profile previews, while allergy warnings remain private safety data.

### FR-004 Availability Windows

**Priority:** MVP

**Description:** Users shall be able to define recurring and one-off time windows when they are available for dining.

**Acceptance Criteria**

- Users can create, edit, and delete recurring weekly availability windows.
- Users can create, edit, and delete one-off availability windows.
- Availability windows can be managed from the profile area and used as filters for event matching and event search.
- Availability remains private profile data even when it is used to narrow event results.

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

- Restaurants are stored with name, city/state/ZIP, cuisine tags, and price tier; street address may also be stored for admin-managed catalog maintenance.
- Restaurants may optionally store latitude/longitude and a provider-qualified external PlaceId, such as an OpenStreetMap `osm:<id>` value.
- Admins can create, update, archive, and restore internal catalog records without making user-facing browse/search depend on a live external API.
- Manual admin catalog saves may geocode the restaurant address into stored latitude/longitude so map presentation can use catalog-backed coordinates.
- Admin-only OpenStreetMap imports preview candidates before commit, support ZIP/radius or manual geographic bounds, and skip duplicate candidates rather than overwriting or merging existing records.
- Restaurant records can be referenced by events and later slot entities.

### FR-007 Restaurant Discovery and Filtering

**Priority:** MVP

**Description:** Users shall be able to browse/search restaurants and apply basic filters.

**Acceptance Criteria**

- Users can filter restaurants by cuisine, price tier, and distance.
- MVP restaurant discovery reads from the internal catalog only; admin-only OpenStreetMap/Overpass import and admin-maintained catalog CRUD may be used to populate that catalog.
- Restaurant selection is reusable during event creation and may be shown in search/list form; map presentation is optional when coordinates exist.
- When restaurant slots are enabled, event creation may filter mapped restaurants to those with open discounted slots in the next rolling 30 days and show available slots for each selected restaurant.
- Midpoint or group-aware suggestion logic remains lightweight service behavior over the internal catalog.
- Archived restaurants are excluded from browse/search/suggestion results while remaining valid historical references for existing events.

### FR-008 Create Events (Open and Closed)

**Priority:** MVP

**Description:** Users shall be able to create dining events.

**Acceptance Criteria**

- An event includes optional title, event type (Open/Closed), start time, capacity, and exactly one of selected restaurant or cuisine target.
- Standalone event creation lets the host choose Open or Closed.
- The host automatically becomes a `JOINED` participant and counts toward capacity.
- Open events are discoverable and joinable by eligible users.
- Closed events are invite-only.
- Hosts may invite users to either Open or Closed events by exact username.
- Event invites remain actionable until `DecisionAt` in MVP.
- Event invites do not reserve seats.

**Event Invite Flow**

- Host creates an event or opens an active event they host.
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
- Group detail includes a member/person list, group chat entry point, linked event history, and announcement panel.
- Group owners can choose a preset food-themed group wallpaper/background for personalization in MVP.

### FR-012 Group Roles

**Priority:** MVP

**Description:** Groups shall have a simple owner/member model in MVP.

**Acceptance Criteria**

- Each group has exactly one owner.
- The owner is auto-created as an active member.
- Owners can manage group settings and remove members.
- Owners initiate private-group invites in MVP.
- Owners can write group announcements/posts that are shown on the group announcement panel.
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
- Group-linked event type follows group visibility: public groups create Open events, and private groups create Closed events.
- Group-linked events are viewable in group context.
- Group event history may display linked-event feedback by reusing event feedback visibility rules.
- Creating a group-linked event creates a group announcement so members can see the new event from the group board.
- Group event history and event-created announcements should provide a direct link to the event detail when the viewer is allowed to see it.
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
- Direct 1-on-1 messaging is governed by FR-021 and remains outside the default MVP UI unless enabled.

### FR-017A Group Chat

**Priority:** MVP

**Description:** Group members shall be able to communicate in a group-linked chat thread without sharing phone numbers.

**Acceptance Criteria**

- Each group has an associated chat thread.
- Only current active group members can read/write group chat.
- Leaving or removal revokes group-chat access immediately.
- MVP group chat is text-only and uses the same basic SignalR plus history-retrieval model as event chat.

### FR-017B Support Chat

**Priority:** MVP

**Description:** Users shall be able to message admins from a Help/Support entry point.

**Acceptance Criteria**

- Authenticated users can open support chat from the application shell.
- A user's support chat is scoped to that user's account and is visible to that user and admins only.
- Admins can list support conversations and reply in the selected user's support thread.
- Support chat is text-only and uses the same SignalR plus history-retrieval model as event and group chat.
- Normal users cannot read or write another user's support thread.

### FR-017C Event Feedback

**Priority:** MVP

**Description:** Joined event participants shall be able to leave feedback after a dining event is completed.

**Acceptance Criteria**

- Feedback can be submitted only after the event reaches `COMPLETED`.
- Cancelled, open, full, and confirmed events do not accept feedback.
- Only users with a current `JOINED` participant record for the completed event can create or update feedback.
- Each participant can maintain one editable feedback entry per event.
- Feedback includes required text and a required rating from 1 to 5.
- Feedback may include up to four optional image photos stored through the database-backed media path.
- Open-event feedback is visible to authenticated users who can view the event.
- Closed-event feedback is visible only to the host, joined participants, moderators, and admins.
- Group-linked event feedback can appear in group event history only when the current user is allowed to view that event's feedback.
- Feedback and photos can be reported through the existing report flow with related event/user context.
- Feedback does not change event lifecycle, participation, capacity, chat, or notification behavior.

### FR-018 People Discovery (Search)

**Priority:** MVP

**Description:** Users shall be able to discover other users via search.

**Acceptance Criteria**

- Users can search by username/display name.
- Discovery exposes only a limited public profile preview.
- Search respects privacy settings, blocks, and active discovery-visibility moderation restrictions.
- After a user Likes or Passes another user in the swipe flow, that subject is hidden from the actor's people search until the subject makes a reciprocal swipe decision about the actor.
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

**Description:** The backend implements direct messaging structures/endpoints while keeping the feature disabled until enabled.

**Acceptance Criteria**

- Direct chats exist as separate threads/messages.
- Direct messaging is allowed only between current connected Budz.
- Feature flags can disable creation/sending in production.
- Blocking, moderation, `ChatSend` restrictions, and reporting policies apply.
- Direct chats use the same text-only SignalR plus paged history model as event and group chat.

### FR-022 Event and Group Browse/Search

**Priority:** MVP

**Description:** Users shall be able to browse and search open events and public groups using basic query-based filters.

**Acceptance Criteria**

- Users can browse visible events that match cuisine, time window, distance, price tier, status, event category, and availability filters.
- Visible event browse includes open public events plus closed events the signed-in user is allowed to view through hosting, invite, or participation context.
- Event browse can explicitly use the signed-in user's home ZIP code and saved availability windows when the user turns those filters on.
- Event browse may expose an explicit Quick Search mode/tab that ranks active Open events with available seats using home ZIP distance, saved cuisine preferences, and Budz already joined; this personalization happens only when the user selects that mode.
- Quick Search excludes Full, Completed, Cancelled, and Closed events.
- Users can filter events by status.
- Users can distinguish group-linked events from ordinary standalone events.
- Users can browse/search public groups by name and visibility.
- Event and group browse exclude live contexts that would place the signed-in user into a shared active event/group with a user blocked in either direction.
- Blank browse/search state does not silently personalize event results from profile data.
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
- Blocking removes any active Budz connection between the pair.
- Blocking separates the pair from shared live contexts: the blocker leaves shared active groups and non-completed joined events, except that a blocking group owner or event host keeps their role and the blocked user is removed instead.
- Blocking prevents either user from newly viewing or joining live event/group contexts that already contain the other user as host, owner, joined participant, or active member.
- Completed shared events are preserved for history/feedback, but blocked users do not see each other's completed-event chat messages.
- Blocking is reversible by the blocker.
- Unblocking does not automatically recreate Budz, group membership, or event participation.

### FR-025 Report Users/Content

**Priority:** MVP

**Description:** Users shall be able to report inappropriate behavior or content.

**Acceptance Criteria**

- Users can submit a report with category/reason and optional explanation.
- Reports can target a user and may include related event/message context.
- Reports may include one or more optional image evidence attachments stored in the application database.
- Reports are stored and accessible to moderators.

### FR-026 Moderation Workflow

**Priority:** MVP

**Description:** Moderators shall be able to review reports and take actions.

**Acceptance Criteria**

- Moderators can view reports in a review queue.
- Moderators and admins can search users, messages, reports, events, groups, feedback, restaurants, and other moderation-relevant content from a privileged staff surface.
- Report review shows reporter, reported user, message sender, or content author as readable user links such as display name plus username, with GUIDs only as secondary traceability metadata.
- Moderators can resolve reports with a recorded decision.
- Moderation actions are stored.

### FR-027 Scoped Restrictions ("Soft Bans")

**Priority:** MVP

**Description:** Moderators shall be able to apply temporary scoped restrictions to users.

**Acceptance Criteria**

- Restrictions can target specific capabilities such as discovery visibility, chat send, event join, or event create.
- Restrictions can have an expiration time.
- Restricted users are prevented from restricted actions while the restriction is active.
- A full MVP soft ban applies the `DiscoveryVisibility`, `ChatSend`, `EventJoin`, and `EventCreate` scopes together, revokes the user's active sessions, blocks login/refresh while active, and hides that user from regular user-facing people surfaces such as discovery, Budz, group members, and event participant lists while staff moderation surfaces retain traceability.

### FR-028 Audit Logging

**Priority:** MVP

**Description:** The system shall record an immutable audit trail for sensitive actions.

**Acceptance Criteria**

- Moderation actions write audit log entries.
- Group ownership transfer/dissolution writes audit log entries when enabled.
- Audit logs are append-only.

### FR-029 Restaurant Admin Accounts

**Priority:** MVP

**Description:** The system shall support restaurant admin accounts that manage assigned restaurants and slots in the active MVP/demo flow.

**Acceptance Criteria**

- A restaurant may have multiple admins.
- A restaurant admin may manage one or more restaurants.
- Global admins grant and revoke restaurant-admin assignments.
- Granting an active assignment grants the coarse `RestaurantAdmin` role.
- Revoking an assignment removes the coarse `RestaurantAdmin` role only when the user has no remaining active restaurant-admin assignments.
- Restaurant admins can create/update restaurant profiles.
- Restaurant-admin operations are active by default and remain behind explicit kill-switch flags.

### FR-030 Restaurant Slots (Create/Manage)

**Priority:** MVP

**Description:** Restaurant admins may create availability slots with capacity and timing.

**Acceptance Criteria**

- A slot contains restaurant, start/end time window, max participants, and cutoff.
- A slot may define a minimum threshold and whole-number discount percentage for discount activation.
- Discount threshold and discount percentage must be provided together, or both omitted.
- Restaurant admins may remove an existing optional discount configuration from an unreserved open slot.
- Restaurant admins can edit/cancel slots.
- Slot capacity follows the event capacity range of 2 to 8 participants.
- Slot cancellation may cancel a linked event with a restaurant-slot cancellation reason.
- Slot operations are active by default and remain behind explicit kill-switch flags.

### FR-031 Slot Selection and Reservation

**Priority:** MVP

**Description:** Events may select a restaurant slot, reserving it immediately.

**Acceptance Criteria**

- Only the event host may reserve a slot for the event.
- The event must be active when the reservation is made.
- An event can select a slot only if event time fits the slot window.
- Event capacity cannot exceed slot capacity.
- Selecting the slot reserves it immediately for that event.
- Event creation may let the host select a compatible open slot from the restaurant picker; the server must re-check compatibility before creating the event, then reserve the selected slot using the normal reservation rules.
- A slot can have only one active event reservation.
- A slot-reserved event uses the slot restaurant as the selected restaurant and clears cuisine-target selection.

### FR-032 Discount Threshold Activation

**Priority:** MVP

**Description:** Slots may activate simulation-only discounts once a confirmed threshold is met.

**Acceptance Criteria**

- Discount activates when joined participants meet/exceed the threshold before cutoff.
- Active discounts use the slot's configured discount percentage.
- Discount activation is stored as active/inactive state.
- Before or at cutoff, discount activation is recalculated after reservation, participant, or lifecycle changes.
- After cutoff, the final active/inactive result is frozen.
- If the threshold is not met by cutoff, discount remains inactive.
- Discount simulation is active by default and remains behind an explicit kill-switch flag.
- Discount activation does not itself settle payments; checkout simulation is governed by FR-034.

### FR-033 Restaurant Admin Controls on Slot-Linked Events

**Priority:** MVP

**Description:** Restaurant admins may manage slot-linked event outcomes.

**Acceptance Criteria**

- Restaurant admins can cancel a slot and linked events are cancelled with normal event-cancellation notifications.
- Optional restaurant approval/denial flows remain out of active MVP/demo scope.

### FR-034 Payment Simulation and Checkout (Feature-Flagged)

**Priority:** MVP++

**Description:** Joined event participants may run a simulation-only checkout flow for an event with a selected restaurant while the feature remains disabled by default.

**Acceptance Criteria**

- Checkout creation is feature-flagged and disabled by default.
- Only a current `JOINED` event participant can create a checkout session for that event.
- Checkout requires a selected restaurant and does not call an external payment provider.
- Checkout totals are simulated from the selected restaurant price tier.
- An active discount activation may reduce the simulated total.
- Checkout sessions can move from `Pending` to `Completed` or `Cancelled`.
- Completed sessions cannot be cancelled, and cancelled sessions cannot be completed.
- No real money movement, settlement, refunds, tax calculation, tips, saved payment methods, or provider webhooks are implied.

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
2. People discovery includes search + swipe + mutual Budz in MVP, while direct 1-on-1 messaging is feature-flagged and remains out of default MVP UI.
3. Event chat, group chat, and user-to-admin support chat are part of MVP.
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
6. Payment simulation and checkout remain feature-flagged and simulation-only.

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
- EventFeedback
- EventFeedbackPhoto
- Group
- GroupMember
- GroupInvite
- ChatThread
- ChatMessage
- Notification
- PasswordResetToken
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

**MVP++ / feature-flagged entities**

- RestaurantAdminAssignment
- RestaurantSlot
- EventSlotReservation
- DiscountActivation
- Direct-chat scope over ChatThread/ChatMessage
- CheckoutSession
- Search/feed projections as read models

## Change Log

- Normalized the requirements around the canonical TasteBudz name used by the repository.
- Promoted group chat to MVP and aligned MVP chat on SignalR plus paged history retrieval.
- Clarified Budz as reciprocal Like only in MVP, with no pending Bud-request state.
- Corrected event sizing to a typical 4-6 participants with a hard maximum capacity of 8 and no hard maximum group-member cap in MVP.
- Added explicit host event-edit and automatic event-completion rules so the requirements match the backend architecture and decision log.
- Added completed-event feedback with participant-authored ratings, text, and optional database-backed photos.
- Refreshed the data model checklist so it matches the standalone backend domain model.
- Promoted direct chat and simulation-only checkout to feature-flagged MVP++ backend behavior.
- Added support chat, admin-issued password reset links, and search filtering for one-sided outbound swipe decisions.
