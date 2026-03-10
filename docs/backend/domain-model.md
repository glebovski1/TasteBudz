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
- chat in event and group scopes
- receive in-app notifications
- report users or content
- support moderation, scoped restrictions, and audit logging

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

Internal restaurant catalog used for browsing, filtering, and event selection.

### Events

Open and closed events, participant lifecycle, capacity handling, invite behavior, event lifecycle, and timing rules such as `DecisionAt`.

### Groups

Public/private groups, membership, owner control, invitations, and optional event linkage.

### Messaging and Notifications

Scoped chat plus persisted in-app notifications.

### Moderation and Safety

Reports, moderation actions, temporary restrictions, and append-only audit logging.

## 5. Canonical Modeling Decisions

### 5.1 Group ownership has one canonical source

`Group.OwnerUserId` is the only source of truth for ownership.

`GroupMember` tracks membership, not ownership.

### 5.2 Budz in MVP use reciprocal Like only

For MVP:

- one effective directional `SwipeDecision` exists per `(ActorUserId, SubjectUserId)` pair
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

### 5.7 Group-linked events use `GroupId` as context only

`Event.GroupId` is an optional context link.

It allows an event to appear in group context, but it does not make group membership equivalent to event participation.
In MVP, only the current group owner may create or update an event with that `GroupId`.

### 5.8 Chat uses scope-based threads

`ChatThread` uses:

- `ScopeType`
- `ScopeId`

For MVP:

- event chat is available only to current `JOINED` event participants
- group chat is available only to current active group members
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

### 5.13 Blocking prevents new direct interaction, not shared-context history

Blocking prevents new Bud interactions, private/direct messaging, and event/group invitations between the pair.

Blocking does not automatically:

- hide public profiles/events
- remove users from already joined shared events/groups
- split an already shared event/group chat while both users remain authorized in that shared context

### 5.14 Event defaults are explicit

For MVP:

- `Capacity` must be between `2` and `8`
- `MinParticipantsToRun` defaults to `2`
- open-event `DecisionAt` defaults to `EventStartAt - 15 minutes`
- closed-event `DecisionAt` defaults to `EventStartAt - 24 hours`

### 5.15 Account deletion preserves historical integrity

Account deletion is modeled as a logical/soft-delete workflow rather than physical cascade delete.

### 5.16 Onboarding completeness is derived

Onboarding completeness is a derived service state rather than a standalone persisted core entity.

### 5.17 Midpoint restaurant suggestion is service behavior

Midpoint or group-aware suggestion logic is application/service behavior over user coarse location data and restaurant data. It is not a core domain entity.

### 5.18 Later concepts must not leak into MVP

Entities tagged as later-only may remain documented for future compatibility, but they should not receive normal controllers, endpoints, repositories, or UI flows in MVP unless explicitly promoted.

## 6. Core Entities

### Identity / Profile Aggregate

- `UserAccount`
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

### Events

- `Event`
- `EventParticipant`

### Groups

- `Group`
- `GroupMember`
- `GroupInvite`

### Messaging and Notifications

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
- `RestaurantAdminAssignment`
- `RestaurantSlot`
- `EventSlotReservation`
- `DiscountActivation`

## 7. Core Value Types and Enums

Formalize these in code as closed enums/value sets where appropriate:

- `EventStatus`
- `EventType`
- `EventParticipantState`
- `GroupVisibility`
- `GroupLifecycleState`
- `GroupMemberState`
- `GroupInviteStatus`
- `RestrictionScope`
- `SocialGoal`
- `PriceTier`
- `SpiceTolerance`

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
- coarse global roles (`User`, `Moderator`, `Admin`, later `RestaurantAdmin`)
- created/updated timestamps

Rules:

- one account has at most one active profile bundle
- account status is not used for temporary scoped moderation
- deleted accounts leave historical references intact

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

### UserPreferences

Root compatibility profile used for discovery and filtering.

Core data:

- owning account
- scalar spice tolerance
- links to cuisine, dietary, and allergy substructures

Rules:

- multi-valued categories are not compressed into one opaque blob
- allergies and dietary data are safety inputs, not public-by-default data

### RecurringAvailabilityWindow / OneOffAvailabilityWindow

Represent recurring weekly or one-time availability windows.

Rules:

- start must be before end
- recurring and one-off windows remain distinct concepts

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
- city/state/ZIP
- optional latitude/longitude
- cuisine tags/categories
- price tier
- optional external place identifier

Rules:

- location data must be sufficient for ZIP/distance filtering
- MVP suggestions are computed from the internal catalog

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
- host counts toward capacity
- event status is server-controlled
- `CANCELLED` and `COMPLETED` are terminal
- closed-event invites do not reserve seats
- event can auto-complete by time according to server policy

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

### Group

Represents a persistent social group.

Core data:

- name
- description
- visibility
- lifecycle state
- `OwnerUserId`

Rules:

- creating a group auto-creates the owner as an active member
- owner must always be an active member
- visibility and lifecycle are separate concepts
- groups have no hard member cap in MVP

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
- event chat access derives from current participant state
- group chat access derives from current group membership

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
- `UserAccount` many <-> many `Group` via `GroupMember`
- `Group` 1 -> many `GroupInvite`
- `Restaurant` 1 -> many `Event`
- `Group` 1 -> many `Event` via optional link
- `Event` 1 -> many `EventParticipant`
- `Event` 1 -> 1 event-scoped `ChatThread`
- `Group` 1 -> 1 group-scoped `ChatThread`
- `ChatThread` 1 -> many `ChatMessage`
- `ModerationReport` 1 -> many `ModerationAction`
- `ModerationAction` 0..many -> `UserRestriction`
- `ModerationAction` 1 -> many `AuditLogEntry`

## 10. Aggregate Boundaries

### User Aggregate

- `UserAccount`
- `UserProfile`
- `UserPreferences`
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

Focus: capacity, duplicate-join prevention, invite handling, explicit status transitions, and `DecisionAt` behavior.

### Group Aggregate

- `Group`
- `GroupMember`
- `GroupInvite`

Focus: ownership, membership, private invite handling, and later ownership transfer/dissolution.

### Messaging Aggregate

- `ChatThread`
- `ChatMessage`

Focus: scope-based access rules across event and group chat in MVP.

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
- Event chat access is limited to current `JOINED` participants.
- Group chat access is limited to current active group members.
- Blocking prevents new direct interaction but does not automatically remove shared-context participation.
- Each `UserRestriction` applies to exactly one scope.
- `UserAccount.Status` is not used for temporary scoped moderation.
- Audit entries are append-only.

## 12. MVP vs Extension Readiness

### MVP - model in detail now

- user/account/profile/preferences/privacy/availability entities
- restaurant catalog
- events and event participants
- groups, group members, and group invites
- chat threads/messages for event + group scope
- notifications
- swipe decisions and Budz connections
- moderation reports/actions/restrictions
- audit log entries

### MVP+ - recognize now, keep lighter

- `BudRequest` for a later manual-request Bud flow
- richer RSVP/cutoff fields on `Event` / `EventParticipant`
- ownership transfer and dissolution workflows
- notification preference toggles if needed

### MVP++ / feature-flagged - extension-ready only

- direct 1-on-1 messaging using `ChatThread` with `Direct` scope
- restaurant-admin assignment
- restaurant slots and slot reservations
- discount activation
- feed/search projections and caches as read models

## 13. Persistence Notes

This model is intentionally not a physical schema.

Repositories may map it in multiple ways as long as the business guarantees remain intact.

Important mapping notes:

- `EventParticipant` acts as both participation record and closed-event invite lifecycle record.
- `BudConnection` is the only required Budz relationship record in MVP.
- `ChatThread` uses a generic scope model instead of separate event/group/direct roots.
- Later restaurant-operation entities may be absent from MVP persistence entirely.
- Search indexes, feed caches, and denormalized browse views are read models, not primary domain entities.

