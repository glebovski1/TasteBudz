# TasteBudz API Endpoint List

This document defines the recommended public backend surface for TasteBudz. It is aligned with the functional requirements, domain model, backend architecture, and accepted ADRs.

## 1. API Conventions

- Base path: `/api/v1`
- Style: REST-oriented, noun-based resources with a small number of explicit action endpoints where that is clearer than overloading `DELETE`
- Auth: bearer access token plus refresh token/session flow
- Protected endpoints require authenticated user context
- Controllers remain thin; business rules stay in services/domain logic
- Clients must not directly set server-owned lifecycle state such as `Event.status`
- DTOs are explicit contracts; persistence entities are not exposed directly
- Event chat and group chat use SignalR for real-time delivery plus HTTP history retrieval
- Availability is modeled with separate recurring and one-off resources
- Hidden or not-launched feature-flagged endpoints should generally return `404 Not Found`
- Launched features with insufficient permission should return `403 Forbidden`

## 2. Shared Contract Notes

Common response patterns:

- list endpoints typically return `{ items, totalCount }`
- cursor-based chat history endpoints return `{ items, nextCursor }`
- timestamp fields are UTC ISO-8601 values
- IDs are UUIDs

Key DTO families:

- `SessionDto`: access token, refresh token, expiry, and current user summary
- `OnboardingStatusDto`: `isComplete` plus `missingRequiredFields`
- `ProfileDto`: public/private profile fields for the current user
- `PreferenceDto`: cuisine tags, spice tolerance, dietary flags, allergies
- `RecurringAvailabilityWindowDto` and `OneOffAvailabilityWindowDto`
- `RestaurantDto`
- `EventSummaryDto`, `EventDetailDto`, `EventParticipantDto`
- `GroupSummaryDto`, `GroupDetailDto`, `GroupInviteDto`
- `DiscoveryProfilePreviewDto`, `BudConnectionDto`, `SwipeDecisionResultDto`
- `ChatMessageDto`
- `NotificationDto`
- `ReportDto`, `RestrictionDto`, `AuditLogEntryDto`

## 3. MVP Endpoints

### 3.1 Auth and Access

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Register User | POST | `/api/v1/auth/register` | Create a new user account | No |
| Login | POST | `/api/v1/auth/login` | Authenticate and issue access/refresh tokens | No |
| Refresh Session | POST | `/api/v1/auth/refresh` | Exchange refresh token for a new session/token pair | No |
| Logout | POST | `/api/v1/auth/logout` | Revoke the current refresh token/session | Yes |

Representative request shapes:

```json
{
  "username": "string",
  "email": "string",
  "password": "string",
  "zipCode": "string"
}
```

```json
{
  "usernameOrEmail": "string",
  "password": "string"
}
```

```json
{
  "refreshToken": "string"
}
```

### 3.2 Profiles, Preferences, Availability, Privacy

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Get Onboarding Status | GET | `/api/v1/onboarding/status` | Return onboarding completeness | Yes |
| Get My Profile | GET | `/api/v1/profiles/me` | Return current-user profile | Yes |
| Update My Profile | PATCH | `/api/v1/profiles/me` | Update profile fields | Yes |
| Get My Dashboard | GET | `/api/v1/me/dashboard` | Return profile/dashboard summary | Yes |
| List My Events | GET | `/api/v1/me/events` | Return hosted/joined events | Yes |
| List My Groups | GET | `/api/v1/me/groups` | Return active groups | Yes |
| List My Event Invites | GET | `/api/v1/me/event-invites` | Return pending closed-event invites | Yes |
| Request Account Deletion | POST | `/api/v1/account/deletion` | Soft-delete the current account | Yes |
| Get My Preferences | GET | `/api/v1/preferences/me` | Return current food preferences | Yes |
| Replace My Preferences | PUT | `/api/v1/preferences/me` | Replace food preferences | Yes |
| List Recurring Availability | GET | `/api/v1/availability/recurring` | List recurring weekly availability | Yes |
| Create Recurring Availability | POST | `/api/v1/availability/recurring` | Create recurring availability window | Yes |
| Update Recurring Availability | PATCH | `/api/v1/availability/recurring/{windowId}` | Edit recurring availability window | Yes |
| Delete Recurring Availability | DELETE | `/api/v1/availability/recurring/{windowId}` | Remove recurring availability window | Yes |
| List One-Off Availability | GET | `/api/v1/availability/one-off` | List one-time availability windows | Yes |
| Create One-Off Availability | POST | `/api/v1/availability/one-off` | Create one-time availability window | Yes |
| Update One-Off Availability | PATCH | `/api/v1/availability/one-off/{windowId}` | Edit one-time availability window | Yes |
| Delete One-Off Availability | DELETE | `/api/v1/availability/one-off/{windowId}` | Remove one-time availability window | Yes |
| Get Privacy Settings | GET | `/api/v1/privacy-settings/me` | Return privacy settings | Yes |
| Update Privacy Settings | PATCH | `/api/v1/privacy-settings/me` | Update privacy settings | Yes |
| List Blocks | GET | `/api/v1/blocks` | List blocked users | Yes |
| Create Block | POST | `/api/v1/blocks` | Block a user | Yes |
| Remove Block | DELETE | `/api/v1/blocks/{blockedUserId}` | Unblock a user | Yes |

Representative request shapes:

```json
{
  "displayName": "string",
  "bio": "string",
  "homeAreaZipCode": "45220",
  "socialGoal": "Friends"
}
```

```json
{
  "cuisineTags": ["Sushi", "Thai"],
  "spiceTolerance": "Medium",
  "dietaryFlags": ["Vegetarian"],
  "allergies": ["Peanuts"]
}
```

```json
{
  "dayOfWeek": "Friday",
  "startTime": "18:00",
  "endTime": "21:00",
  "label": "Friday Dinner"
}
```

```json
{
  "startsAt": "timestamp",
  "endsAt": "timestamp",
  "label": "This Saturday"
}
```

```json
{
  "discoveryEnabled": false
}
```

### 3.3 Restaurants

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Browse Restaurants | GET | `/api/v1/restaurants` | Browse/search/filter restaurants | Yes |
| Get Restaurant Detail | GET | `/api/v1/restaurants/{restaurantId}` | Return restaurant details | Yes |
| Get Restaurant Suggestions | GET | `/api/v1/restaurants/suggestions` | Return simple suggestion list | Yes |

Query parameters:

- browse: `q`, `cuisine`, `priceTier`, `zipCode`, `radiusMiles`, `page`, `pageSize`
- suggestions: `eventId`, `groupId`, `zipCode`, `radiusMiles`, `cuisineTags[]`

Contract notes:

- MVP suggestions remain simple and deterministic.
- Midpoint logic is service behavior, not a separate domain entity.

### 3.4 Events

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Browse Events | GET | `/api/v1/events` | Browse/search open events | Yes |
| Create Event | POST | `/api/v1/events` | Create open or closed event | Yes |
| Get Event Detail | GET | `/api/v1/events/{eventId}` | Return event detail | Yes |
| Update Event | PATCH | `/api/v1/events/{eventId}` | Host edits material event details before cancellation/completion | Yes |
| List Event Participants | GET | `/api/v1/events/{eventId}/participants` | List participants | Yes |
| Join Event | POST | `/api/v1/events/{eventId}/participants` | Join an open event | Yes |
| Update My Participation | PATCH | `/api/v1/events/{eventId}/participants/me` | Leave / accept / decline | Yes |
| Remove Participant | POST | `/api/v1/events/{eventId}/participants/{userId}/removal` | Host or moderator removes participant | Yes |
| Invite Users to Closed Event | POST | `/api/v1/events/{eventId}/invites` | Invite users by username | Yes |
| Cancel Event | POST | `/api/v1/events/{eventId}/cancellation` | Cancel event | Yes |

Representative create/update request shapes:

```json
{
  "title": "Friday Sushi Night",
  "eventType": "Open",
  "eventStartAt": "timestamp",
  "capacity": 6,
  "selectedRestaurantId": "uuid",
  "cuisineTarget": null,
  "groupId": null,
  "inviteUsernames": []
}
```

```json
{
  "title": "Updated Friday Sushi Night",
  "eventStartAt": "timestamp",
  "selectedRestaurantId": "uuid",
  "cuisineTarget": null
}
```

```json
{
  "state": "LEFT"
}
```

```json
{
  "usernames": ["alex", "sam"]
}
```

```json
{
  "reason": "Restaurant closed"
}
```

Event contract rules:

- Host is auto-created as a `JOINED` participant and counts toward capacity.
- Exactly one of `selectedRestaurantId` or `cuisineTarget` must be set.
- Clients cannot set `status` directly.
- Open-event joins and closed-event accepts must be atomic/concurrency-safe.
- Closed-event invites do not reserve seats.
- `DecisionAt` locks participant state changes except support/moderator override.
- Material event edits should trigger notifications to affected participants.

Representative browse query parameters:

- `q`
- `cuisine`
- `priceTier`
- `status`
- `eventType`
- `zipCode`
- `radiusMiles`
- `startsAfter`
- `startsBefore`
- `availabilityOnly`
- `groupId`
- `page`
- `pageSize`

### 3.5 Groups

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Browse Groups | GET | `/api/v1/groups` | Browse/search public groups | Yes |
| Create Group | POST | `/api/v1/groups` | Create group | Yes |
| Get Group Detail | GET | `/api/v1/groups/{groupId}` | Return group detail | Yes |
| Update Group | PATCH | `/api/v1/groups/{groupId}` | Update group settings | Yes |
| List Group-Linked Events | GET | `/api/v1/groups/{groupId}/events` | View linked events in group context | Yes |
| Join Group | POST | `/api/v1/groups/{groupId}/members` | Join public group | Yes |
| Leave Group | DELETE | `/api/v1/groups/{groupId}/members/me` | Leave group | Yes |
| Remove Group Member | POST | `/api/v1/groups/{groupId}/members/{userId}/removal` | Owner removes member | Yes |
| Invite User to Group | POST | `/api/v1/groups/{groupId}/invites` | Invite user to private group | Yes |
| Respond to Group Invite | PATCH | `/api/v1/groups/invites/{inviteId}` | Accept/decline invite | Yes |

Representative request shapes:

```json
{
  "name": "Cincy Foodies",
  "description": "Weekend dinner group",
  "visibility": "Public"
}
```

```json
{
  "name": "Updated Cincy Foodies",
  "description": "Weekend dinner group",
  "visibility": "Private"
}
```

```json
{
  "username": "alex"
}
```

```json
{
  "status": "Accepted"
}
```

Group contract rules:

- Public groups allow direct join when active.
- Private groups require invitation in MVP.
- Private-group invites are owner-initiated in MVP.
- Only the current group owner may create or update an event with that group's `GroupId`.
- `GroupId` on an event is context only and does not replace event participation rules.
- Group owner is auto-created as an active member.

### 3.6 Discovery / Budz

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Search People | GET | `/api/v1/discovery/people` | Search users | Yes |
| Get Swipe Candidates | GET | `/api/v1/discovery/swipe-candidates` | Return swipe queue | Yes |
| Record Swipe Decision | POST | `/api/v1/discovery/swipes` | Save Like/Pass decision | Yes |
| List My Budz | GET | `/api/v1/budz` | List mutual Budz | Yes |

Representative request shape:

```json
{
  "subjectUserId": "uuid",
  "decision": "Like"
}
```

Contract notes:

- Search respects privacy settings, blocks, and moderation restrictions such as `DiscoveryVisibility`.
- One effective directional swipe decision exists per actor/subject pair.
- Reciprocal effective Like decisions create Budz.
- MVP does not expose pending Bud-request state.
- Repeating the swipe endpoint may update the effective decision before a Budz connection exists.

### 3.7 Messaging (Event + Group Chat)

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Connect Chat Hub | SIGNALR | `/hubs/chat` | Realtime event/group chat connection | Yes |
| List Event Messages | GET | `/api/v1/events/{eventId}/messages` | Return paged event-chat history | Yes |
| List Group Messages | GET | `/api/v1/groups/{groupId}/messages` | Return paged group-chat history | Yes |

Representative history response shape:

```json
{
  "items": [],
  "nextCursor": "string"
}
```

MVP messaging rules:

- SignalR is the primary transport for sending and receiving event/group chat messages.
- Event chat access is derived from current event participation state.
- Group chat access is derived from current active group membership.
- Event chat: only current `JOINED` participants may read/write.
- Group chat: only current active group members may read/write.
- Leaving/removal revokes access immediately.
- Blocking alone does not split a shared event/group chat if both users remain authorized in the same shared context.
- Message model is text-only.

SignalR hub expectations:

- authenticate before connection
- `JoinScope(scopeType, scopeId)` joins callers only to authorized event/group channels
- `SendMessage({ scopeType, scopeId, body })` sends text messages into authorized event/group threads
- `MessageReceived` is the server event name for broadcast delivery
- use REST history endpoints for initial backfill and reconnection

### 3.8 Notifications

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| List Notifications | GET | `/api/v1/notifications` | Return notification center | Yes |
| Mark Notification Read | PATCH | `/api/v1/notifications/{notificationId}` | Mark notification as read | Yes |

Representative request shape:

```json
{
  "read": true
}
```

MVP notification contract:

| Type | Trigger | Recipient | Minimum context |
|---|---|---|---|
| `EventInviteReceived` | User is invited to a closed event | invited user | `eventId`, `eventTitle`, `inviterUserId` |
| `EventParticipantChanged` | Participant joins or leaves an event | event host and affected participant | `eventId`, `participantUserId`, `changeType` |
| `EventStatusChanged` | Event transitions to `CONFIRMED` or `CANCELLED` | active event participants | `eventId`, `status`, `decisionAt` |
| `EventUpdated` | Host makes a material event edit | active event participants | `eventId`, `changedFields` |
| `GroupInviteReceived` | User is invited to a private group | invited user | `groupId`, `groupName`, `inviterUserId` |
| `BudMatchCreated` | Reciprocal Like creates a Bud connection | both Bud users | `otherUserId`, `connectionId` |

### 3.9 Moderation and Audit

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Submit Report | POST | `/api/v1/reports` | Submit moderation report | Yes |
| List Moderation Reports | GET | `/api/v1/moderation/reports` | Return moderation queue | Moderator/Admin |
| Get Moderation Report | GET | `/api/v1/moderation/reports/{reportId}` | Return report detail | Moderator/Admin |
| Resolve Moderation Report | PATCH | `/api/v1/moderation/reports/{reportId}` | Resolve report | Moderator/Admin |
| Create Restriction | POST | `/api/v1/moderation/restrictions` | Apply scoped restriction | Moderator/Admin |
| Update Restriction | PATCH | `/api/v1/moderation/restrictions/{restrictionId}` | Revoke/update restriction | Moderator/Admin |
| View Audit Logs | GET | `/api/v1/audit-logs` | Return audit log entries | Admin |

Representative request shapes:

```json
{
  "targetType": "User",
  "targetId": "uuid",
  "category": "Harassment",
  "reason": "string",
  "explanation": "string",
  "relatedEventId": "uuid",
  "relatedUserId": "uuid",
  "relatedMessageId": "uuid"
}
```

```json
{
  "subjectUserId": "uuid",
  "scope": "DiscoveryVisibility",
  "reason": "Harassment",
  "expiresAt": "timestamp"
}
```

Allowed MVP restriction scopes:

- `DiscoveryVisibility`
- `ChatSend`
- `EventJoin`
- `EventCreate`

Audit log query parameters may include `actorUserId`, `targetEntityType`, `targetEntityId`, `page`, and `pageSize`.

## 4. Later or Feature-Flagged Endpoints

Disabled/not-launched endpoints in this section should generally return `404 Not Found` until launched.

### 4.1 Group Administration (Later)

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Transfer Group Ownership | POST | `/api/v1/groups/{groupId}/ownership-transfer` | Transfer ownership | GroupOwner |
| Dissolve Group | POST | `/api/v1/groups/{groupId}/dissolution` | Dissolve group | GroupOwner |

Representative request shapes:

```json
{
  "newOwnerUserId": "uuid"
}
```

```json
{
  "confirm": true
}
```

### 4.2 Direct Chat (Later)

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Create Direct Chat | POST | `/api/v1/direct-chats` | Create direct thread when enabled | Yes |
| List Direct Chat Messages | GET | `/api/v1/direct-chats/{threadId}/messages` | Return direct-message history | Yes |
| Post Direct Chat Message | POST | `/api/v1/direct-chats/{threadId}/messages` | Send direct message | Yes |

### 4.3 Feed (Later)

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Get Feed | GET | `/api/v1/feeds/events` | Return Tonight / This Week feed | Yes |

Representative query parameter:

- `window=tonight|this-week`

### 4.4 Restaurant Operations (Later)

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Get Managed Restaurants | GET | `/api/v1/restaurant-admin/restaurants` | List managed restaurants | RestaurantAdmin |
| Update Managed Restaurant | PATCH | `/api/v1/restaurant-admin/restaurants/{restaurantId}` | Update restaurant profile | RestaurantAdmin |
| Create Restaurant Slot | POST | `/api/v1/restaurant-admin/restaurants/{restaurantId}/slots` | Create slot | RestaurantAdmin |
| Update Restaurant Slot | PATCH | `/api/v1/restaurant-admin/slots/{slotId}` | Update slot | RestaurantAdmin |
| Cancel Restaurant Slot | POST | `/api/v1/restaurant-admin/slots/{slotId}/cancellation` | Cancel slot | RestaurantAdmin |
| Reserve Slot For Event | POST | `/api/v1/events/{eventId}/slot-reservations` | Link event to slot | EventHost |

Representative request shapes:

```json
{
  "startsAt": "timestamp",
  "endsAt": "timestamp",
  "capacity": 8,
  "cutoffAt": "timestamp",
  "minThresholdForDiscount": 6
}
```

```json
{
  "slotId": "uuid"
}
```

## 5. Recommended MVP Public Surface

Keep MVP focused on:

- auth
- profile/preferences/privacy/dashboard
- restaurants
- events
- groups
- discovery/Budz
- event chat and group chat
- notifications
- moderation and audit

## 6. Important Contract Rules

- Clients must not directly set `Event.status`.
- Host is auto-created as a `JOINED` participant and counts toward capacity.
- Join/leave/invite logic belongs under event participation workflows.
- Group-linked events must be retrievable through group context.
- Availability remains split between recurring and one-off windows.
- Swipe decisions use one effective directional record per actor/subject pair.
- Event chat and group chat access are derived from current participation/membership state.
- Material host edits to events should produce participant notifications.
- Hidden/not-launched features stay behind feature flags and normally return `404`.

### 6.1 High-Risk Error Semantics

- Join/accept when no seat is available: return `409 Conflict`.
- Participation change after `DecisionAt` lock: return `409 Conflict` unless an approved support/moderator override path is used.
- Action denied by active block/restriction policy: return `403 Forbidden`.
- Hidden/not-launched feature-flagged endpoint: return `404 Not Found`.
