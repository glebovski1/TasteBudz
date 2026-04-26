# TasteBudz Domain Model

## 1. Purpose

This document defines the abstract domain model for TasteBudz. It describes business concepts, rules, relationships, and aggregate boundaries used to guide backend implementation.

This is not a physical database schema and not an ORM design. Persistence mapping may differ as long as the business guarantees in this document are preserved.

## 2. Context and Assumptions

TasteBudz allows users to:

- create profiles
- store cuisine preferences, dietary flags, allergies, spice tolerance, location context, and availability
- discover other users
- form mutual Budz connections through reciprocal Like decisions in MVP
- create and join events
- create and participate in groups
- chat in event, group, support, and feature-flagged direct scopes
- run simulation-only checkout when enabled
- receive in-app notifications
- report users or content
- support moderation, scoped restrictions, and audit logging
- support anonymous password reset requests plus admin-issued password reset links

Architecture assumptions:

- ASP.NET Core Web API
- modular monolith
- SQL database
- thin controllers
- business rules in services/domain logic
- capstone scope: practical and testable, not overengineered

## 3. Modeling Principles

1. Ownership and state should have one canonical source of truth.
2. MVP social-graph rules should be explicit and minimal.
3. Generic concepts such as chat should still have explicit scope rules.
4. MVP simplicity takes priority over speculative flexibility.
5. Concepts should map cleanly to a relational database.
6. Later-only concepts must not leak into current MVP behavior.

## 4. Core Business Areas

### Identity and Access

Authenticated identity, coarse global roles, and account lifecycle state.

### Profiles and Preferences

Public profile information, cuisine preferences, dietary flags, allergies, spice tolerance, privacy settings, and availability.

### Discovery and Budz

Search, swipe decisions, and mutual Budz connections.

### Restaurants

Internal restaurant catalog used for browsing, filtering, event selection, and active restaurant operations.

### Events

Open and closed events, participant lifecycle, capacity handling, invite behavior, event lifecycle, and timing rules such as `DecisionAt`.

### Groups

Public/private groups, membership, owner control, invitations, and optional event linkage.

### Messaging and Notifications

Scoped chat plus persisted in-app notifications.

### Payments

Simulation-only checkout sessions for event participants when enabled.

### Media Assets

Database-backed image assets linked to one bounded context such as a profile avatar, report evidence, or event-feedback photo.

### Moderation and Safety

Reports, moderation actions, temporary restrictions, and append-only audit logging.

## 5. Canonical Modeling Decisions

### 5.1 Group ownership has one canonical source

`Group.OwnerUserId` is the only source of truth for ownership.

`GroupMember` tracks membership, not ownership.

### 5.2 Budz in MVP use reciprocal Like only

For MVP:

- one effective directional `SwipeDecision` exists per `(ActorUserId, SubjectUserId)` pair
- a one-sided outbound swipe decision hides the subject from the actor's people search until the subject records a reciprocal decision
- reciprocal effective `Like` decisions create a `BudConnection`
- `BudConnection` never has a `Pending` state in MVP
- `BudRequest` is extension-ready only and not part of MVP behavior

### 5.3 Public groups use direct join and private groups use invites

For MVP:

- public groups allow direct join when active
- private groups require `GroupInvite`
- private-group invites are initiated by the current group owner
- no join-request workflow exists in MVP

### 5.4 Preferences are split into SQL-friendly substructures

`UserPreferences` remains the root, while multi-valued data is split into:

- `UserCuisinePreference`
- `UserDietaryFlag`
- `UserAllergy`

`SpiceTolerance` remains scalar data on `UserPreferences`.

### 5.5 Availability is split by type

Availability is represented as:

- `RecurringAvailabilityWindow`
- `OneOffAvailabilityWindow`

### 5.6 Event host is also a participant

The host is automatically represented as an `EventParticipant` in `JOINED` state and counts toward capacity.

Completed-event feedback is also participant-authored. Each joined participant may have at most one editable `EventFeedback` entry for a completed event, and that feedback rates the event experience rather than the restaurant.

### 5.7 Group-linked events use `GroupId` as context only

`Event.GroupId` is an optional context link.

It allows an event to appear in group context, but it does not make group membership equivalent to event participation.
In MVP, only the current group owner may create or update an event with that `GroupId`.
Group-linked event type follows group visibility: public groups link only to Open events, and private groups link only to Closed events.

### 5.8 Chat uses scope-based threads

`ChatThread` uses:

- `ScopeType`
- `ScopeId`

For MVP:

- event chat is available only to current `JOINED` event participants
- group chat is available only to current active group members
- support chat is available only to the supported user and admins
- direct chat is available only to current connected Budz when enabled and unblocked
- leaving/removal revokes access immediately
- chat access is derived from current state, not cached independently

### 5.9 Reports use one canonical target

`ModerationReport` uses `TargetType` + `TargetId` as its canonical target and may include related context references.

### 5.10 Restrictions are one-scope-per-record

Each `UserRestriction` applies to exactly one scope such as `DiscoveryVisibility`, `ChatSend`, or `EventJoin`.

### 5.11 Account lifecycle and moderation enforcement are separate

- `UserAccount.Status` is for account lifecycle
- `UserRestriction` is for scoped moderation enforcement

### 5.12 Notifications are simplified in MVP

Notifications are persisted in-app notices with read state only. Multi-channel delivery tracking is later work.

### 5.13 Media assets use one bounded context per record

For MVP:

- each `MediaAsset` is owned by exactly one user
- each `MediaAsset` is linked to exactly one bounded context
- current launched contexts are profile-avatar, moderation-report evidence, and event-feedback photo
- image bytes and metadata are stored in the application database

### 5.14 Blocking prevents new direct interaction, not shared-context history

Blocking prevents new Bud interactions, private/direct messaging, and event/group invitations between the pair.

Blocking does not automatically:

- hide public profiles/events
- remove users from already joined shared events/groups
- split an already shared event/group chat while both users remain authorized in that shared context

### 5.15 Event defaults are explicit

For MVP:

- `Capacity` must be between `2` and `8`
- `MinParticipantsToRun` defaults to `2`
- open-event `DecisionAt` defaults to `EventStartAt - 15 minutes`
- closed-event `DecisionAt` defaults to `EventStartAt - 24 hours`

### 5.16 Account deletion preserves historical integrity

Account deletion is modeled as a logical/soft-delete workflow rather than physical cascade delete.

### 5.17 Onboarding completeness is derived

Onboarding completeness is a derived service state rather than a standalone persisted core entity.

### 5.18 Midpoint restaurant suggestion is service behavior

Midpoint or group-aware suggestion logic is application/service behavior over user coarse location data and restaurant data. It is not a core domain entity.

### 5.19 Later concepts must not leak into MVP

Entities tagged as later-only may remain documented for future compatibility, but they should not receive normal controllers, endpoints, repositories, or UI flows in MVP unless explicitly promoted. Promoted concepts may retain kill-switch flags after launch.

## 6. Core Entities

### Identity / Profile Aggregate

- `UserAccount`
- `PasswordResetToken`
- `PasswordResetRequest`
- `UserProfile`
- `UserPreferences`
- `UserCuisinePreference`
- `UserDietaryFlag`
- `UserAllergy`
- `RecurringAvailabilityWindow`
- `OneOffAvailabilityWindow`
- `PrivacySettings`
- `UserBlock`

### Restaurants

- `Restaurant`
- `RestaurantAdminAssignment`
- `RestaurantSlot`
- `EventSlotReservation`
- `DiscountActivation`

### Payments

- `CheckoutSession`

### Events

- `Event`
- `EventParticipant`
- `EventFeedback`
- `EventFeedbackPhoto`

### Groups

- `Group`
- `GroupMember`
- `GroupInvite`
- `GroupAnnouncement`

### Messaging and Notifications

- `MediaAsset`
- `ChatThread`
- `ChatMessage`
- `Notification`

### Discovery

- `SwipeDecision`
- `BudConnection`

### Moderation and Audit

- `ModerationReport`
- `ModerationAction`
- `UserRestriction`
- `AuditLogEntry`

### Extension-Ready Concepts

- `BudRequest`

## 7. Core Value Types and Enums

Formalize these in code as closed enums/value sets where appropriate:

- `EventStatus`
- `EventType`
- `EventParticipantState`
- `GroupVisibility`
- `GroupWallpaperTheme`
- `GroupAnnouncementType`
- `GroupLifecycleState`
- `GroupMemberState`
- `GroupInviteStatus`
- `RestrictionScope`
- `SocialGoal`
- `PriceTier`
- `SpiceTolerance`
- `RestaurantSlotStatus`
- `EventSlotReservationStatus`
- `CheckoutSessionStatus`

Recommended MVP `RestrictionScope` examples:

- `DiscoveryVisibility`
- `ChatSend`
- `EventJoin`
- `EventCreate`

## 8. Entity Summaries

### UserAccount

Represents the authenticated identity that can enter the system and receive authorization decisions.

Core data:

- username, email, credential/password-hash reference
- account status
- coarse global roles (`User`, `Moderator`, `Admin`, `RestaurantAdmin`)
- created/updated timestamps

Rules:

- one account has at most one active profile bundle
- account status is not used for temporary scoped moderation
- deleted accounts leave historical references intact

### PasswordResetToken

Represents a one-time admin-issued credential reset token for an active user.

Core data:

- target user account
- hashed token value
- admin actor that created the token
- created, expiry, used, and revoked timestamps

Rules:

- only admins may create reset tokens
- raw reset tokens are returned only at creation time and stored hashed
- issuing a new reset token revokes previous unused tokens for the user
- successful reset marks the token used, updates the password hash, and revokes existing user sessions

### PasswordResetRequest

Represents an anonymously submitted password reset help request that admins can review before issuing a reset token.

Core data:

- submitted username
- user-written message/context
- optional matched active user account
- created timestamp
- optional closed timestamp
- optional admin actor who closed or handled the request

Rules:

- public submission never reveals whether the username matched an active account
- requests may exist without any matched account
- requests do not reset passwords by themselves and do not replace admin-issued token flow
- admins may close a request directly or implicitly by issuing a reset token from that request

### UserProfile

Represents the user-facing social profile shown in discovery and social contexts.

Core data:

- display name/public username
- bio
- ZIP-based home area
- social goal

Rules:

- exact addresses are never exposed
- profile visibility is constrained by privacy settings
- bio functions as the public personality note; social goal plus structured cuisine and dietary data may be surfaced in public profile cards, while allergies and availability remain private

### UserPreferences

Root compatibility profile used for discovery and filtering.

Core data:

- owning account
- scalar spice tolerance
- links to cuisine, dietary, and allergy substructures

Rules:

- multi-valued categories are not compressed into one opaque blob
- cuisine tags and dietary flags may be surfaced as public compatibility tags, while allergies remain private safety inputs

### RecurringAvailabilityWindow / OneOffAvailabilityWindow

Represent recurring weekly or one-time availability windows.

Rules:

- start must be before end
- recurring and one-off windows remain distinct concepts
- availability is private profile data used for matching/search filters rather than public profile display

### PrivacySettings

Represents user-controlled discovery/contact visibility.

Core data:

- discovery enabled/disabled
- optional later simple notification preferences

Rules:

- if discovery is disabled, the user must not appear in people discovery/search

### UserBlock

Represents a directional block relationship.

Identity:

- conceptual unique pair `(BlockerUserId, BlockedUserId)`

Rules:

- blocking is directional
- blocking filters discovery/search for the pair
- blocking disables new direct/private interaction paths
- blocking is reversible

### Restaurant

Represents a dining venue available for search, filtering, and event selection.

Core data:

- name
- optional street address
- city/state/ZIP
- optional latitude/longitude
- cuisine tags/categories
- price tier
- optional external place identifier
- archive state for catalog visibility

Rules:

- location data must be sufficient for ZIP/distance filtering
- admin catalog saves may geocode a restaurant address into stored coordinates
- MVP suggestions are computed from the internal catalog
- archived restaurants are excluded from browse/suggestion lists but may remain referenced by existing events
- restaurant-admin profile mutation is allowed only through active assignment checks when restaurant operations are enabled

### RestaurantAdminAssignment

Represents a global-admin-granted management relationship between a user and a restaurant.

Core data:

- restaurant reference
- user reference
- created timestamp
- optional revoked timestamp

Rules:

- one active assignment grants management authority for exactly one restaurant/user pair
- global `Admin` is the only actor allowed to grant or revoke assignments
- granting an assignment adds the coarse `RestaurantAdmin` role
- revoking an assignment removes `RestaurantAdmin` only when the user has no remaining active assignments
- revoked assignments preserve history

### RestaurantSlot

Represents a restaurant-owned availability window that an event host can reserve.

Core data:

- restaurant reference
- start/end time window
- capacity
- cutoff timestamp
- optional minimum threshold for discount activation
- optional whole-number discount percentage
- status (`Open` / `Cancelled`)
- cancellation metadata

Rules:

- slot capacity follows event capacity bounds of 2 through 8
- cutoff must be before or equal to the slot start time
- minimum discount threshold, when present, must be between 2 and slot capacity
- minimum discount threshold and discount percentage must be provided together, or both omitted
- discount percentage must be between 1 and 100
- an unreserved open slot's optional discount pair may be cleared explicitly by restaurant admin update
- only an actively assigned restaurant admin may create, edit, or cancel a slot for that restaurant
- cancelled slots cannot be reserved

### EventSlotReservation

Represents the active or cancelled link between an event and a restaurant slot.

Core data:

- event reference
- slot reference
- status (`Active` / `Cancelled`)
- created timestamp
- optional cancellation metadata

Rules:

- only the event host may reserve a slot
- the event must be active
- one event may have at most one active slot reservation
- one slot may have at most one active event reservation
- event start time must fit within the slot window
- event capacity must not exceed slot capacity
- reservation sets the event selected restaurant to the slot restaurant and clears cuisine target
- slot cancellation cancels the active reservation and linked event through normal event cancellation behavior

### DiscountActivation

Represents simulation-only discount state for a slot reservation.

Core data:

- reservation reference
- active/inactive state
- finalized flag
- configured discount percentage from the reserved slot
- evaluated timestamp

Rules:

- joined event participants count as confirmed participants
- before or at cutoff, activation can be recalculated after reservation, participation, or lifecycle changes
- after cutoff, the final active/inactive result is frozen
- active discount state uses the reserved slot's configured discount percentage
- no payment, checkout, or settlement state is owned by this record; checkout simulation is represented separately by `CheckoutSession`

### CheckoutSession

Represents one simulation-only checkout attempt for a joined event participant.

Core data:

- event reference
- user reference
- `CheckoutSessionStatus` (`Pending` / `Completed` / `Cancelled`)
- currency
- subtotal cents
- discount cents
- total cents
- created and updated timestamps
- optional completed timestamp
- optional cancelled timestamp

Rules:

- checkout is disabled by default behind a feature flag
- creation requires a current `JOINED` event participant
- creation requires the event to have a selected restaurant
- subtotal is simulated from the selected restaurant's price tier
- active discount simulation may reduce the total
- the checkout owner is the only normal user who can complete or cancel the session
- completed and cancelled are terminal states
- no external provider, real money movement, settlement, refunds, tax calculation, tips, saved payment methods, or webhooks are implied

### Event

Represents an open or closed dining plan.

Core data:

- optional title
- `EventType` (`Open` / `Closed`)
- `EventStatus`
- `EventStartAt`
- `DecisionAt`
- `Capacity`
- `MinParticipantsToRun`
- optional `SelectedRestaurantId`
- optional `CuisineTarget`
- optional `GroupId`
- host user reference

Rules:

- exactly one of `SelectedRestaurantId` or `CuisineTarget` must be set
- if `GroupId` is set, event type must match group visibility (`Public` -> `Open`, `Private` -> `Closed`)
- host counts toward capacity
- event status is server-controlled
- `CANCELLED` and `COMPLETED` are terminal
- event invites do not reserve seats
- event can auto-complete by time according to server policy
- while an active slot reservation exists, host edits must preserve slot restaurant, clear cuisine target, stay within the slot window, and keep capacity within slot capacity

### EventParticipant

Represents one effective event/user participation record.

Core data:

- event reference
- user reference
- state (`INVITED`, `JOINED`, `DECLINED`, `LEFT`, `REMOVED`)
- invited/joined/responded timestamps

Rules:

- `(EventId, UserAccountId)` is effectively unique
- `JOINED` counts toward capacity
- `INVITED`, `DECLINED`, `LEFT`, and `REMOVED` do not count toward capacity
- leaving preserves history and frees capacity
- host is always represented as `JOINED`
- after `DecisionAt`, participant state changes are locked except support/admin override

### EventFeedback

Represents one participant-authored rating and text response for a completed event.

Identity:

- conceptual unique pair `(EventId, AuthorUserId)`

Core data:

- event reference
- author user reference
- rating from 1 through 5
- required feedback text
- created and updated timestamps

Rules:

- feedback can be created or updated only after the event is `COMPLETED`
- cancelled and active events do not accept feedback
- author must be a current or historical `JOINED` participant for the event
- each author may have at most one feedback entry per event
- feedback text is trimmed, required, and capped at 1000 characters
- feedback visibility follows event type: Open event feedback is readable by authenticated event viewers; Closed event feedback is readable by the host, joined participants, and Moderator/Admin roles
- event feedback does not affect restaurant review state, event lifecycle, capacity, chat, or notifications

### EventFeedbackPhoto

Represents one optional image attachment on an event feedback entry.

Core data:

- feedback reference
- media asset reference
- created timestamp

Rules:

- feedback photos are stored as `MediaAsset` records linked to the event and feedback entry
- each feedback entry may have at most four photos
- only the feedback author can add or remove their own feedback photos
- media bytes are served only to callers who can read that event's feedback

### Group

Represents a persistent social group.

Core data:

- name
- description
- visibility
- wallpaper theme
- lifecycle state
- `OwnerUserId`

Rules:

- creating a group auto-creates the owner as an active member
- owner must always be an active member
- visibility and lifecycle are separate concepts
- wallpaper theme is an owner-managed preset value, not an uploaded media asset in MVP
- groups have no hard member cap in MVP

### GroupAnnouncement

Represents a group-facing announcement shown on the group detail board.

Core data:

- group reference
- author user
- announcement type (`OwnerPost`, `EventCreated`)
- title
- body
- optional related event reference
- created timestamp

Rules:

- only the current group owner can create owner posts
- event creation creates a system announcement when the event is linked to a group
- announcement visibility follows group detail visibility

### GroupMember

Represents current or historical membership in a group.

Rules:

- membership is canonical for group access
- owner status is not stored here as a separate competing truth source

### GroupInvite

Represents a private-group invitation workflow record.

Core data:

- group reference
- invited user
- inviter user
- status (`Pending`, `Accepted`, `Declined`, `Revoked`, `Expired`)
- timestamps

Rules:

- private-group membership is created through accepted group invites in MVP

### ChatThread

Represents a reusable scoped conversation container.

Core data:

- `ScopeType`
- `ScopeId`
- created timestamp

Rules:

- one event-scoped thread exists per event in MVP
- one group-scoped thread exists per group in MVP
- one support-scoped thread can exist per user account
- one direct-scoped thread can exist per connected Budz pair when direct chat is enabled
- event chat access derives from current participant state
- group chat access derives from current group membership
- support chat access derives from the support subject user id and admin role
- direct chat access derives from Budz connection state plus current block state

### ChatMessage

Represents one text message in a chat thread.

Core data:

- thread reference
- sender user reference
- body
- created timestamp

Rules:

- MVP messaging is text-only
- transport choice does not change domain rules

### Notification

Represents a persisted in-app notice about an important state change.

Core data:

- recipient user
- notification type
- context type/id
- optional lightweight payload
- created timestamp
- read timestamp

Rules:

- read state is tracked per notification
- MVP delivery state is effectively persisted/in-app only
- private-group invite notifications use invite context so the client can accept or decline before group membership exists

### SwipeDecision

Represents one user's Like/Pass decision about another user.

Core data:

- actor user
- subject user
- decision (`Like`, `Pass`)
- timestamp

Rules:

- one effective directional record exists per actor/subject pair
- the service may update the effective decision before a Budz connection exists
- reciprocal effective Like decisions create a `BudConnection`

### BudConnection

Represents the mutual Budz relationship between two users.

Identity:

- normalized user pair such as `(LowerUserId, HigherUserId)`

Core data:

- normalized user pair
- connection state (`Connected`, `Removed`)
- created/connected/ended timestamps

Rules:

- connection is mutual, not directional
- only one effective Budz connection exists per pair
- `Pending` is never a valid MVP state
- removing a Budz connection preserves history and ends the active relationship

### ModerationReport

Represents a report submitted about a user or related content/context.

Core data:

- reporter
- canonical target type/id
- optional related user/event/message references
- category/reason
- explanation
- created timestamp
- review status

Rules:

- exactly one canonical target is always present
- reports do not automatically punish users

### ModerationAction

Represents a moderator/admin decision taken in response to a report or safety concern.

Core data:

- moderator/admin actor
- related report
- action type
- decision notes
- created timestamp

Rules:

- moderation actions must be explicit and auditable
- a report may be resolved without issuing a restriction

### UserRestriction

Represents one active or historical restriction on one user for one scope.

Core data:

- subject user
- issuer user
- restriction scope
- reason
- starts/expires timestamps
- status (`Active`, `Expired`, `Revoked`)

Rules:

- one record represents one scope only
- only active, unexpired restrictions are enforceable
- multiple restrictions may coexist if scopes differ

### AuditLogEntry

Represents the immutable record of a sensitive system action.

Core data:

- action type
- actor reference
- target entity type/id
- timestamp
- immutable details payload

Rules:

- audit entries are append-only
- moderation actions must create audit entries

## 9. Relationship Overview

- `UserAccount` 1 -> 1 `UserProfile`
- `UserAccount` 1 -> 1 `UserPreferences`
- `UserAccount` 1 -> many `PasswordResetToken`
- `UserAccount` 1 -> many matched `PasswordResetRequest` references when submitted usernames resolve to active accounts
- `UserPreferences` 1 -> many `UserCuisinePreference`
- `UserPreferences` 1 -> many `UserDietaryFlag`
- `UserPreferences` 1 -> many `UserAllergy`
- `UserAccount` 1 -> many `RecurringAvailabilityWindow`
- `UserAccount` 1 -> many `OneOffAvailabilityWindow`
- `UserAccount` 1 -> 1 `PrivacySettings`
- `UserAccount` many <-> many `UserAccount` through directional `UserBlock`
- `UserAccount` 1 -> many `SwipeDecision`
- `UserAccount` many <-> many `UserAccount` through `BudConnection`
- `UserAccount` 1 -> many `Event` as host
- `UserAccount` many <-> many `Event` via `EventParticipant`
- `UserAccount` 1 -> many `EventFeedback`
- `UserAccount` many <-> many `Group` via `GroupMember`
- `Group` 1 -> many `GroupInvite`
- `Group` 1 -> many `GroupAnnouncement`
- `Restaurant` 1 -> many `Event`
- `Restaurant` 1 -> many `RestaurantAdminAssignment`
- `UserAccount` 1 -> many `RestaurantAdminAssignment`
- `Restaurant` 1 -> many `RestaurantSlot`
- `RestaurantSlot` 1 -> 0..1 active `EventSlotReservation`
- `Event` 1 -> 0..1 active `EventSlotReservation`
- `EventSlotReservation` 1 -> 0..1 `DiscountActivation`
- `Event` 1 -> many `CheckoutSession`
- `UserAccount` 1 -> many `CheckoutSession`
- `Group` 1 -> many `Event` via optional link
- `Event` 1 -> many `EventParticipant`
- `Event` 1 -> many `EventFeedback`
- `EventFeedback` 1 -> many `EventFeedbackPhoto`
- `MediaAsset` 1 -> 0..1 `EventFeedbackPhoto`
- `Event` 1 -> 1 event-scoped `ChatThread`
- `Group` 1 -> 1 group-scoped `ChatThread`
- `UserAccount` 1 -> 0..1 support-scoped `ChatThread`
- `ChatThread` 1 -> many `ChatMessage`
- `ModerationReport` 1 -> many `ModerationAction`
- `ModerationAction` 0..many -> `UserRestriction`
- `ModerationAction` 1 -> many `AuditLogEntry`

## 10. Aggregate Boundaries

### User Aggregate

- `UserAccount`
- `UserProfile`
- `UserPreferences`
- `PasswordResetToken`
- preference substructures
- availability windows
- `PrivacySettings`

Focus: onboarding completeness, profile management, privacy consistency, and availability editing.

### Discovery Aggregate

- `SwipeDecision`
- `BudConnection`

Focus: pair-level discovery behavior and reciprocal-Like transition to mutual Budz in MVP.

### Event Aggregate

- `Event`
- `EventParticipant`
- `EventFeedback`
- `EventFeedbackPhoto`
- `EventSlotReservation` for the event-side reservation link when restaurant slots are enabled

Focus: capacity, duplicate-join prevention, invite handling, explicit status transitions, `DecisionAt` behavior, and completed-event feedback policy.

### Restaurant Operations Aggregate

- `Restaurant`
- `RestaurantAdminAssignment`
- `RestaurantSlot`
- `EventSlotReservation` for the restaurant-side reservation link when restaurant slots are enabled
- `DiscountActivation`

Focus: assignment-gated restaurant mutation, slot lifecycle, reservation uniqueness, and discount simulation.

### Payments Aggregate

- `CheckoutSession`

Focus: participant-owned simulation checkout state and terminal status transitions.

### Group Aggregate

- `Group`
- `GroupMember`
- `GroupInvite`

Focus: ownership, membership, private invite handling, and later ownership transfer/dissolution.

### Messaging Aggregate

- `ChatThread`
- `ChatMessage`

Focus: scope-based access rules across event, group, support, and enabled direct chat.

### Moderation Module

- `ModerationReport`
- `ModerationAction`
- `UserRestriction`

Focus: report review, moderation decisions, and scoped enforcement.

### Cross-Cutting Audit

- `AuditLogEntry`

Focus: append-only record of sensitive actions.

## 11. Cross-Entity Invariants

- Only authenticated users can create events or send messages.
- Exact home addresses must never be exposed.
- Discovery respects privacy settings and active user blocks.
- Discovery search hides one-sided outbound swipe targets until the target user decides back.
- Reciprocal effective Like decisions create Budz directly in MVP.
- `BudConnection` never uses a pending state in MVP.
- Private groups require accepted `GroupInvite` before membership creation.
- `Group.OwnerUserId` must reference an active group member.
- Only active + public groups are publicly discoverable.
- Active event participants must never exceed event capacity.
- The host always has a `JOINED` participant record.
- Capacity counts only `JOINED` participants.
- Event invites do not reserve seats.
- Exactly one of selected restaurant or cuisine target is set on an event.
- Group-linked event type must match the linked group's visibility.
- Active slot-reserved events use the slot restaurant as selected restaurant and have no cuisine target.
- Active slot reservations are unique per event and per slot.
- Slot reservation requires the event time and capacity to fit the slot.
- Discount activation is simulation-only and freezes after cutoff.
- Checkout simulation is participant-owned, requires a selected restaurant, and has terminal completed/cancelled states.
- Event feedback is allowed only for completed events and only by joined event participants.
- Each event participant can have at most one feedback entry per event.
- Feedback photos are readable only by callers who can read the parent event's feedback.
- Event chat access is limited to current `JOINED` participants.
- Group chat access is limited to current active group members.
- Support chat access is limited to the supported user and admins.
- Direct chat access is limited to connected Budz when enabled and unblocked.
- Blocking prevents new direct interaction but does not automatically remove shared-context participation.
- Each `UserRestriction` applies to exactly one scope.
- `UserAccount.Status` is not used for temporary scoped moderation.
- Password reset tokens are admin-created, one-time use, stored hashed, and revoke existing user sessions on success.
- Password reset requests accept username/message anonymously, may remain unmatched, and return a generic accepted response to the public caller.
- Audit entries are append-only.

## 12. MVP vs Extension Readiness

### MVP - model in detail now

- user/account/profile/preferences/privacy/availability entities
- restaurant catalog
- events, event participants, and completed-event feedback
- groups, group members, and group invites
- chat threads/messages for event, group, and support scope
- notifications
- swipe decisions and Budz connections
- moderation reports/actions/restrictions
- audit log entries

### MVP+ - recognize now, keep lighter

- `BudRequest` for a later manual-request Bud flow
- richer RSVP/cutoff fields on `Event` / `EventParticipant`
- ownership transfer and dissolution workflows
- notification preference toggles if needed

### MVP++ / feature-flagged - disabled by default

- direct 1-on-1 messaging using `ChatThread` with `Direct` scope
- checkout session simulation
- feed/search projections and caches as read models

## 13. Persistence Notes

This model is intentionally not a physical schema.

Repositories may map it in multiple ways as long as the business guarantees remain intact.

Important mapping notes:

- `EventParticipant` acts as both participation record and event invite lifecycle record.
- `EventFeedback` is unique per `(EventId, AuthorUserId)` and remains separate from restaurant reviews.
- `EventFeedbackPhoto` links feedback records to database-backed `MediaAsset` bytes for event-feedback images.
- `BudConnection` is the only required Budz relationship record in MVP.
- `ChatThread` uses a generic scope model instead of separate event/group/support/direct roots.
- Support chat uses the same scoped model, with the support subject user id as the scope id.
- Restaurant-operation entities are present in the canonical SQLite schema and active MVP/demo endpoints; restaurant operation flags remain available as kill switches.
- Search indexes, feed caches, and denormalized browse views are read models, not primary domain entities.

