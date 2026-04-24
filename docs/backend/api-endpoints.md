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
- Event chat, group chat, and support chat use SignalR for real-time delivery plus HTTP history retrieval
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
- `ProfileDto`: profile fields plus public cuisine/dietary compatibility tags used by social UI
- `PreferenceDto`: cuisine tags, spice tolerance, dietary flags, allergies
- `RecurringAvailabilityWindowDto` and `OneOffAvailabilityWindowDto`
- `PasswordResetRequestAcceptedDto` and `PasswordResetRequestDto`
- `RestaurantDto`
- `AdminRestaurantCatalogItemDto`
- `EventSummaryDto`, `EventDetailDto`, `EventParticipantDto`, `EventFeedbackDto`, `EventFeedbackPhotoDto`
- `GroupSummaryDto`, `GroupDetailDto`, `GroupInviteDto`
- `RestaurantAdminAssignmentDto`, `RestaurantSlotDto`, `EventSlotReservationDto`, `DiscountActivationDto`
- `DiscoveryProfilePreviewDto`, `BudConnectionDto`, `SwipeDecisionResultDto`
- `MediaAssetDto`
- `ChatMessageDto`, `SupportThreadDto`
- `PasswordResetTokenDto`
- `NotificationDto`
- `ReportDto`, `RestrictionDto`, `AuditLogEntryDto`

## 3. MVP Endpoints

### 3.1 Auth and Access

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Register User | POST | `/api/v1/auth/register` | Create a new user account | No |
| Login | POST | `/api/v1/auth/login` | Authenticate and issue access/refresh tokens | No |
| Refresh Session | POST | `/api/v1/auth/refresh` | Exchange refresh token for a new session/token pair | No |
| Create Password Reset Request | POST | `/api/v1/auth/password-reset-requests` | Submit anonymous username/message reset request for admin review | No |
| Reset Password | POST | `/api/v1/auth/password-reset` | Complete an admin-issued password reset token by setting a new password | No |
| Logout | POST | `/api/v1/auth/logout` | Revoke the current refresh token/session | Yes |
| List Open Password Reset Requests | GET | `/api/v1/admin/users/password-reset-requests` | Return open password reset requests for admin review | Admin |
| Close Password Reset Request | POST | `/api/v1/admin/users/password-reset-requests/{requestId}/closure` | Dismiss or close an open password reset request | Admin |
| Create Password Reset Token | POST | `/api/v1/admin/users/password-reset-tokens` | Issue a one-time password reset link for an active user | Admin |

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

```json
{
  "username": "string",
  "message": "string"
}
```

```json
{
  "token": "string",
  "newPassword": "string"
}
```

```json
{
  "usernameOrEmail": "string",
  "passwordResetRequestId": "uuid"
}
```

Password reset request submission returns a generic accepted response and does not disclose whether the username matched an active account. Password reset token responses include `userId`, `username`, `resetToken`, `resetUrl`, and `expiresAtUtc`. Reset tokens are one-time use, expire, and successful reset revokes the user's existing sessions. Admin token creation may optionally reference a password reset request id and close that request when the token is issued.

### 3.2 Profiles, Preferences, Availability, Privacy

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Get Onboarding Status | GET | `/api/v1/onboarding/status` | Return onboarding completeness | Yes |
| Get My Profile | GET | `/api/v1/profiles/me` | Return current-user profile | Yes |
| Update My Profile | PATCH | `/api/v1/profiles/me` | Update profile fields | Yes |
| Upload My Profile Avatar | POST | `/api/v1/profiles/me/avatar` | Upload or replace the current user's avatar image | Yes |
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

`bio` is the public personality note surfaced in profile and social UI. Structured `cuisineTags` and `dietaryFlags` are also public compatibility fields for people cards and profile previews, while allergies and availability remain private contracts.

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
  "startsAtUtc": "timestamp",
  "endsAtUtc": "timestamp",
  "label": "This Saturday"
}
```

```json
{
  "discoveryEnabled": false
}
```

Multipart avatar upload shape:

- field name: `file`
- allowed content types: `image/png`, `image/jpeg`, `image/gif`, `image/webp`
- image bytes are stored directly in the application database in MVP

### 3.3 Restaurants

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Browse Restaurants | GET | `/api/v1/restaurants` | Browse/search/filter restaurants | Yes |
| Get Restaurant Detail | GET | `/api/v1/restaurants/{restaurantId}` | Return restaurant details | Yes |
| Get Restaurant Suggestions | GET | `/api/v1/restaurants/suggestions` | Return simple suggestion list | Yes |
| Import Restaurants | POST | `/api/v1/restaurants/import` | Import OpenStreetMap restaurants into the local catalog | Admin |
| List Restaurant Catalog Entries | GET | `/api/v1/admin/restaurants` | Return all catalog entries, including archived ones | Admin |
| Create Restaurant Catalog Entry | POST | `/api/v1/admin/restaurants` | Create a restaurant and geocode its address into stored coordinates | Admin |
| Update Restaurant Catalog Entry | PATCH | `/api/v1/admin/restaurants/{restaurantId}` | Update a catalog record and refresh stored coordinates from its address | Admin |
| Archive Restaurant Catalog Entry | POST | `/api/v1/admin/restaurants/{restaurantId}/archive` | Remove a restaurant from browse/suggestion results without deleting references | Admin |
| Restore Restaurant Catalog Entry | POST | `/api/v1/admin/restaurants/{restaurantId}/restore` | Return an archived restaurant to browse/suggestion results | Admin |

Query parameters:

- browse: `q`, `cuisine`, `priceTier`, `zipCode`, `radiusMiles`, `page`, `pageSize`
- suggestions: `eventId`, `groupId`, `zipCode`, `radiusMiles`, `cuisineTags[]`

Contract notes:

- MVP suggestions remain simple and deterministic.
- Midpoint logic is service behavior, not a separate domain entity.
- The import endpoint is an admin-only catalog maintenance operation; user-facing restaurant browse/search remains local catalog-backed.
- Admin catalog create/update operations geocode the saved address into stored latitude/longitude and may also persist a provider-qualified OpenStreetMap identifier when available.
- `RestaurantDto` may include optional `streetAddress` alongside existing city/state/ZIP data.
- Archived restaurants are excluded from browse/suggestion results but may still be returned by direct id lookups where an existing event or admin tool references them.
- `externalPlaceId` values can be provider-qualified, such as `osm:<id>`.

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
| List Event Feedback | GET | `/api/v1/events/{eventId}/feedback` | List feedback for an event when visible to the caller | Yes |
| Upsert My Event Feedback | PUT | `/api/v1/events/{eventId}/feedback/me` | Create or update the current user's feedback for a completed event | Yes |
| Upload My Feedback Photo | POST | `/api/v1/events/{eventId}/feedback/me/photos` | Attach an image to the current user's event feedback | Yes |
| Delete My Feedback Photo | DELETE | `/api/v1/events/{eventId}/feedback/me/photos/{mediaAssetId}` | Remove one of the current user's feedback photos | Yes |

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

```json
{
  "rating": 5,
  "text": "Great group and easy coordination."
}
```

Representative event-feedback response shape:

```json
{
  "feedbackId": "uuid",
  "eventId": "uuid",
  "authorUserId": "uuid",
  "authorUsername": "sam",
  "authorDisplayName": "Sam Carter",
  "rating": 5,
  "text": "Great group and easy coordination.",
  "photos": [
    {
      "mediaAssetId": "uuid",
      "originalFileName": "table.jpg",
      "contentType": "image/jpeg",
      "contentLength": 12345,
      "createdAtUtc": "timestamp"
    }
  ],
  "createdAtUtc": "timestamp",
  "updatedAtUtc": "timestamp"
}
```

Multipart feedback-photo upload shape:

- field name: `file`
- allowed content types: `image/png`, `image/jpeg`, `image/gif`, `image/webp`
- maximum size: 2 MB

Event contract rules:

- Host is auto-created as a `JOINED` participant and counts toward capacity.
- Exactly one of `selectedRestaurantId` or `cuisineTarget` must be set.
- Clients cannot set `status` directly.
- Open-event joins and closed-event accepts must be atomic/concurrency-safe.
- Closed-event invites do not reserve seats.
- `DecisionAt` locks participant state changes except support/moderator override.
- Material event edits should trigger notifications to affected participants.
- Event feedback can be created or updated only after the event is `Completed`.
- Feedback authors must be joined participants and may have only one editable feedback entry per event.
- Feedback requires a 1-5 rating and non-empty trimmed text up to 1000 characters.
- Feedback photos are optional and capped at four per feedback entry.
- Open-event feedback is readable by authenticated event viewers; Closed-event feedback is readable only by the host, joined participants, and Moderator/Admin roles.
- Event feedback does not change lifecycle, capacity, invites, chat, notifications, or restaurant-review state.

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
- `recommended`
- `groupId`
- `page`
- `pageSize`

Contract note:

- The backend event browse contract remains explicit. MVC quick filters may choose to populate `zipCode`, `radiusMiles`, and `availabilityOnly` from the signed-in user's saved profile data, but the backend does not silently personalize blank requests.
- When `recommended=true`, the backend may rank results using the caller's home ZIP distance, saved cuisine preferences, and Budz already joined in each event. In that explicit mode, `EventSummaryDto` may populate optional recommendation metadata such as `distanceMiles`, `matchingCuisineCount`, and `matchingBudzCount`.

### 3.5 Groups

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Browse Groups | GET | `/api/v1/groups` | Browse/search public groups | Yes |
| Create Group | POST | `/api/v1/groups` | Create group | Yes |
| Get Group Detail | GET | `/api/v1/groups/{groupId}` | Return group detail | Yes |
| Update Group | PATCH | `/api/v1/groups/{groupId}` | Update group settings | Yes |
| List Group-Linked Events | GET | `/api/v1/groups/{groupId}/events` | View linked events for group history/context | Yes |
| List Group Announcements | GET | `/api/v1/groups/{groupId}/announcements` | View owner posts and event-created announcements | Yes |
| Create Group Announcement | POST | `/api/v1/groups/{groupId}/announcements` | Owner creates a group announcement/post | Yes |
| Join Group | POST | `/api/v1/groups/{groupId}/members` | Join public group | Yes |
| Leave Group | DELETE | `/api/v1/groups/{groupId}/members/me` | Leave group | Yes |
| Remove Group Member | POST | `/api/v1/groups/{groupId}/members/{userId}/removal` | Owner removes member | Yes |
| Invite User to Group | POST | `/api/v1/groups/{groupId}/invites` | Invite user to private group | Yes |
| Respond to Group Invite | PATCH | `/api/v1/groups/invites/{inviteId}` | Accept/decline invite | Yes |

Group event history clients should compose `GET /api/v1/groups/{groupId}/events` with `GET /api/v1/events/{eventId}/feedback` when feedback is needed. Feedback visibility remains owned by the Events module.

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
  "visibility": "Private",
  "wallpaperTheme": "SushiBar"
}
```

```json
{
  "title": "Friday ramen plan",
  "body": "Meet near the front window at 7."
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
- People search hides an actor's one-sided outbound Like/Pass target until that subject records a reciprocal swipe decision about the actor.
- Reciprocal effective Like decisions create Budz.
- MVP does not expose pending Bud-request state.
- Repeating the swipe endpoint may update the effective decision before a Budz connection exists.

### 3.7 Messaging (Event + Group + Support Chat)

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Connect Chat Hub | SIGNALR | `/hubs/chat` | Realtime event/group/support chat connection | Yes |
| List Event Messages | GET | `/api/v1/events/{eventId}/messages` | Return paged event-chat history | Yes |
| List Group Messages | GET | `/api/v1/groups/{groupId}/messages` | Return paged group-chat history | Yes |
| List My Support Messages | GET | `/api/v1/support/messages` | Return paged current-user support-chat history | Yes |
| Post My Support Message | POST | `/api/v1/support/messages` | Send a support message as the current user | Yes |
| List Support Threads | GET | `/api/v1/admin/support/threads` | List user support conversations | Admin |
| List User Support Messages | GET | `/api/v1/admin/support/threads/{userId}/messages` | Return paged support history for a user | Admin |
| Post User Support Message | POST | `/api/v1/admin/support/threads/{userId}/messages` | Send an admin reply in a user's support thread | Admin |

Representative history response shape:

```json
{
  "items": [],
  "nextCursor": "string"
}
```

MVP messaging rules:

- SignalR is the primary transport for sending and receiving event/group/support chat messages.
- Event chat access is derived from current event participation state.
- Group chat access is derived from current active group membership.
- Support chat access is derived from the support subject user id: the subject user and admins may access that support thread.
- Event chat: only current `JOINED` participants may read/write.
- Group chat: only current active group members may read/write.
- Support chat: only the supported user and admins may read/write.
- Leaving/removal revokes access immediately.
- Blocking alone does not split a shared event/group chat if both users remain authorized in the same shared context.
- Message model is text-only.

SignalR hub expectations:

- authenticate before connection
- `JoinScope(scopeType, scopeId)` joins callers only to authorized event/group/support channels, or authorized direct-chat channels when direct chat is enabled
- `SendMessage({ scopeType, scopeId, body })` sends text messages into authorized event/group/support/direct threads
- `MessageReceived` is the server event name for broadcast delivery
- use REST history endpoints for initial backfill and reconnection

### 3.8 Media Assets

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Get Media Content | GET | `/api/v1/media/{mediaAssetId}` | Return authorized image bytes | Yes |

Media contract notes:

- media content is image-only in the current MVP slice
- profile avatars are readable by authenticated users through the media endpoint
- report-evidence attachments are readable only by the reporting user and moderator/admin roles
- event-feedback photos are readable only by callers authorized to read that event's feedback

### 3.9 Notifications

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

### 3.10 Moderation and Audit

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Submit Report | POST | `/api/v1/reports` | Submit moderation report | Yes |
| Upload Report Attachment | POST | `/api/v1/reports/{reportId}/attachments` | Add image evidence to a pending report | Yes |
| List Report Attachments | GET | `/api/v1/reports/{reportId}/attachments` | List authorized report evidence attachments | Reporter/Moderator/Admin |
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

Multipart report-attachment upload shape:

- field name: `file`
- only the reporting user may upload
- resolved reports reject new attachments with a conflict response

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

### 4.2 Direct Chat (Feature-Flagged)

These endpoints are implemented behind `FeatureFlags:MessagingDirectChatEnabled` and remain disabled by default. A direct chat can be created only between current connected Budz. Blocking or loss of the Budz connection hides the direct chat from the caller.

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Create Direct Chat | POST | `/api/v1/direct-chats` | Create direct thread when enabled | Yes |
| List Direct Chat Messages | GET | `/api/v1/direct-chats/{directChatId}/messages` | Return direct-message history | Yes |
| Post Direct Chat Message | POST | `/api/v1/direct-chats/{directChatId}/messages` | Send direct message | Yes |

Representative request shapes:

```json
{
  "subjectUserId": "uuid"
}
```

```json
{
  "body": "Hello!"
}
```

Representative direct-chat response shape:

```json
{
  "directChatId": "uuid",
  "otherUserId": "uuid",
  "otherUsername": "sam",
  "otherDisplayName": "Sam Carter",
  "createdAtUtc": "timestamp"
}
```

Contract rules:

- Disabled direct-chat endpoints return `404 Not Found`.
- Enabled direct chat still returns `401 Unauthorized` for unauthenticated callers.
- Direct chat is Budz-only; non-Budz, blocked pairs, and unrelated callers receive `404 Not Found`.
- Direct chat uses `ChatScopeType.Direct`, where the `scopeId` is the returned `directChatId`.
- Active `ChatSend` restrictions block sending.

### 4.3 Feed (Later)

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Get Feed | GET | `/api/v1/feeds/events` | Return Tonight / This Week feed | Yes |

Representative query parameter:

- `window=tonight|this-week`

### 4.4 Restaurant Operations (Feature-Flagged)

These endpoints are implemented behind restaurant operation feature flags and remain disabled by default. `FeatureFlags:RestaurantsOperationsEnabled` gates assignment and managed-restaurant endpoints. `FeatureFlags:RestaurantsSlotsEnabled` gates slot listing, slot mutation, and slot reservation endpoints. `FeatureFlags:RestaurantsDiscountsEnabled` controls discount threshold evaluation and discount DTO output.

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| List Restaurant Admin Assignments | GET | `/api/v1/admin/restaurants/{restaurantId}/admin-assignments` | List active assignments for a restaurant | Admin |
| Grant Restaurant Admin Assignment | POST | `/api/v1/admin/restaurants/{restaurantId}/admin-assignments` | Assign a user to manage a restaurant | Admin |
| Revoke Restaurant Admin Assignment | DELETE | `/api/v1/admin/restaurants/{restaurantId}/admin-assignments/{userId}` | Revoke a user's restaurant assignment | Admin |
| Get Managed Restaurants | GET | `/api/v1/restaurant-admin/restaurants` | List managed restaurants | RestaurantAdmin |
| Update Managed Restaurant | PATCH | `/api/v1/restaurant-admin/restaurants/{restaurantId}` | Update restaurant profile | RestaurantAdmin |
| List Managed Restaurant Slots | GET | `/api/v1/restaurant-admin/restaurants/{restaurantId}/slots` | List slots for an assigned restaurant | RestaurantAdmin |
| Create Restaurant Slot | POST | `/api/v1/restaurant-admin/restaurants/{restaurantId}/slots` | Create slot | RestaurantAdmin |
| Update Restaurant Slot | PATCH | `/api/v1/restaurant-admin/slots/{slotId}` | Update slot | RestaurantAdmin |
| Cancel Restaurant Slot | POST | `/api/v1/restaurant-admin/slots/{slotId}/cancellation` | Cancel slot | RestaurantAdmin |
| List Reservable Restaurant Slots | GET | `/api/v1/restaurants/{restaurantId}/slots` | List open, unreserved slots for event hosts | Yes |
| Reserve Slot For Event | POST | `/api/v1/events/{eventId}/slot-reservations` | Link event to slot | EventHost |

Representative request shapes:

```json
{
  "username": "restaurant-manager"
}
```

```json
{
  "name": "Updated Restaurant",
  "city": "Cincinnati",
  "state": "OH",
  "zipCode": "45202",
  "priceTier": "Three",
  "externalPlaceId": "osm:123456789"
}
```

```json
{
  "startsAtUtc": "timestamp",
  "endsAtUtc": "timestamp",
  "capacity": 8,
  "cutoffAtUtc": "timestamp",
  "minThresholdForDiscount": 6
}
```

```json
{
  "reason": "Restaurant closed for maintenance"
}
```

```json
{
  "slotId": "uuid"
}
```

Contract rules:

- Disabled restaurant operation endpoints return `404 Not Found`.
- Enabled endpoints still return `401 Unauthorized` or `403 Forbidden` for unauthenticated or unauthorized callers.
- Assignment grant auto-adds `RestaurantAdmin`; revoke removes it only when no active assignments remain.
- Restaurant admins can mutate only restaurants with an active assignment.
- Slot capacity is 2 through 8; cutoff must be before or equal to slot start; discount threshold, when present, must be 2 through slot capacity.
- Event slot reservation is host-only, requires an active event and open unreserved slot, and requires event time/capacity to fit the slot.
- Reservation sets the event selected restaurant to the slot restaurant and clears cuisine target.
- `EventDetailDto` may include nullable `slotReservation` and nullable `discountActivation`; event summaries do not include these fields.
- Discount simulation uses joined participants as confirmed participants and freezes the final active/inactive result after cutoff.
- Discount state is simulation-only and may affect checkout simulation when checkout is separately enabled.

### 4.5 Payment Simulation and Checkout (Feature-Flagged)

These endpoints are implemented behind `FeatureFlags:PaymentsCheckoutEnabled` and remain disabled by default. The checkout slice is simulation-only and never calls an external payment provider.

| Endpoint | Method | Path | Description | Auth |
|---|---|---|---|---|
| Create Checkout Session | POST | `/api/v1/events/{eventId}/checkout-sessions` | Create or return a simulated checkout session for a joined event participant | Yes |
| Complete Checkout Session | POST | `/api/v1/checkout-sessions/{checkoutSessionId}/completion` | Mark a pending checkout session completed | Yes |
| Cancel Checkout Session | POST | `/api/v1/checkout-sessions/{checkoutSessionId}/cancellation` | Mark a pending checkout session cancelled | Yes |

Representative response shape:

```json
{
  "checkoutSessionId": "uuid",
  "eventId": "uuid",
  "userId": "uuid",
  "status": "Pending",
  "currency": "USD",
  "subtotalCents": 2500,
  "discountCents": 375,
  "totalCents": 2125,
  "createdAtUtc": "timestamp",
  "updatedAtUtc": "timestamp",
  "completedAtUtc": null,
  "cancelledAtUtc": null
}
```

Contract rules:

- Disabled checkout endpoints return `404 Not Found`.
- Only the checkout-session owner may complete or cancel the session; unrelated callers receive `404 Not Found`.
- Checkout creation requires the caller to be a current `JOINED` event participant.
- Checkout creation requires a selected restaurant and returns `409 Conflict` if the event has only a cuisine target.
- Simulated subtotal is derived from the selected restaurant price tier.
- Active discount activation may reduce the simulated total.
- Completed sessions cannot be cancelled, and cancelled sessions cannot be completed.
- No real money movement, settlement, refunds, tax calculation, tips, saved payment methods, or provider webhooks are implied.

## 5. Recommended MVP Public Surface

Keep MVP focused on:

- auth
- profile/preferences/privacy/dashboard
- restaurants
- events and completed-event feedback
- groups
- discovery/Budz
- event chat, group chat, and support chat
- notifications
- moderation and audit

## 6. Important Contract Rules

- Clients must not directly set `Event.status`.
- Host is auto-created as a `JOINED` participant and counts toward capacity.
- Join/leave/invite logic belongs under event participation workflows.
- Event feedback belongs under the Events module and must not reuse `RestaurantReviews`.
- Group-linked events must be retrievable through group context.
- Availability remains split between recurring and one-off windows.
- Swipe decisions use one effective directional record per actor/subject pair.
- One-sided outbound swipe decisions hide the subject from the actor's people search until the subject decides back.
- Event chat, group chat, and support chat access are derived from current participation, membership, or support-subject state.
- Support chat access is limited to the supported user and admins.
- Material host edits to events should produce participant notifications.
- Hidden/not-launched features stay behind feature flags and normally return `404`.

### 6.1 High-Risk Error Semantics

- Join/accept when no seat is available: return `409 Conflict`.
- Participation change after `DecisionAt` lock: return `409 Conflict` unless an approved support/moderator override path is used.
- Action denied by active block/restriction policy: return `403 Forbidden`.
- Hidden/not-launched feature-flagged endpoint: return `404 Not Found`.
