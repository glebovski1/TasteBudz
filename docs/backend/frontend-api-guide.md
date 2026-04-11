# TasteBudz Frontend API Guide

This guide is for frontend developers integrating with the current TasteBudz backend.

Use this document as a quick-start guide.
Use [api-endpoints.md](./api-endpoints.md) for the fuller contract shape.
Use [implementation-status.md](./implementation-status.md) for what is actually implemented at runtime.

## 1. Current Backend State

The backend is currently `Backend-complete` for the implemented MVP module surface.

Implications:

- the implemented API surface is usable for frontend work
- service logic and API behavior are in place
- runtime persistence is SQLite-backed for the implemented modules
- data is durable within the configured SQLite database file
- Development and IntegrationTesting may recreate local databases from canonical SQL scripts, so test/dev data should still be treated as disposable

## 2. API Basics

- Base path: `/api/v1`
- Auth: bearer access token for protected endpoints
- IDs are UUIDs
- Timestamps are UTC ISO-8601

Common response patterns:

- list endpoints usually return `{ items, totalCount }`
- chat history endpoints return `{ items, nextCursor }`

Common status expectations:

- `401` when auth is missing or invalid
- `403` when the user is authenticated but not allowed to perform the action
- `404` for hidden or not-launched feature-flagged endpoints
- `409` for high-risk workflow conflicts such as full-event joins or post-`DecisionAt` participation changes

Important contract rules:

- clients must not set server-owned lifecycle values such as event status
- event host is automatically created as a joined participant
- event and group chat access is derived from current participation or membership state

## 3. Auth Flow

Typical login flow:

1. `POST /api/v1/auth/register` or `POST /api/v1/auth/login`
2. store the returned access token and refresh token
3. send `Authorization: Bearer <access-token>` on protected requests
4. use `POST /api/v1/auth/refresh` when the access token expires
5. use `POST /api/v1/auth/logout` to end the current session

## 4. Endpoints Ready for Frontend Use

### Current user and profile

- `GET /api/v1/onboarding/status`
- `GET /api/v1/profiles/me`
- `PATCH /api/v1/profiles/me`
- `POST /api/v1/profiles/me/avatar`
- `GET /api/v1/me/dashboard`
- `GET /api/v1/me/events`
- `GET /api/v1/me/groups`
- `GET /api/v1/me/event-invites`
- `POST /api/v1/account/deletion`

Frontend notes:

- `ProfileDto` now includes `avatarMediaAssetId` when the user has an avatar
- avatar upload uses multipart form-data with a `file` field
- avatar bytes can be loaded from `GET /api/v1/media/{mediaAssetId}`

### Preferences, availability, privacy, and blocks

- `GET /api/v1/preferences/me`
- `PUT /api/v1/preferences/me`
- `GET/POST/PATCH/DELETE /api/v1/availability/recurring`
- `GET/POST/PATCH/DELETE /api/v1/availability/one-off`
- `GET /api/v1/privacy-settings/me`
- `PATCH /api/v1/privacy-settings/me`
- `GET /api/v1/blocks`
- `POST /api/v1/blocks`
- `DELETE /api/v1/blocks/{blockedUserId}`

### Restaurants

- `GET /api/v1/restaurants`
- `GET /api/v1/restaurants/{restaurantId}`
- `GET /api/v1/restaurants/suggestions`
- `POST /api/v1/restaurants/import` (Admin only; imports OpenStreetMap data into the local catalog)

Useful browse query params:

- `q`
- `cuisine`
- `priceTier`
- `zipCode`
- `radiusMiles`
- `page`
- `pageSize`

Frontend notes:

- `RestaurantDto.externalPlaceId` is optional; do not require it for restaurant display
- `externalPlaceId` can be provider-qualified, such as `osm:<id>` for OpenStreetMap imports
- only Google Place IDs should be sent as Google Maps `query_place_id`; OpenStreetMap IDs should fall back to a normal Google Maps search URL from restaurant name and location
- when `externalPlaceId` is absent, clients should fall back to a normal Google Maps search URL from restaurant name and location

### Restaurant operations

These flows are feature-flagged and disabled by default.

Admin assignment endpoints:

- `GET /api/v1/admin/restaurants/{restaurantId}/admin-assignments`
- `POST /api/v1/admin/restaurants/{restaurantId}/admin-assignments`
- `DELETE /api/v1/admin/restaurants/{restaurantId}/admin-assignments/{userId}`

Restaurant admin endpoints:

- `GET /api/v1/restaurant-admin/restaurants`
- `PATCH /api/v1/restaurant-admin/restaurants/{restaurantId}`
- `GET /api/v1/restaurant-admin/restaurants/{restaurantId}/slots`
- `POST /api/v1/restaurant-admin/restaurants/{restaurantId}/slots`
- `PATCH /api/v1/restaurant-admin/slots/{slotId}`
- `POST /api/v1/restaurant-admin/slots/{slotId}/cancellation`

Event host slot endpoints:

- `GET /api/v1/restaurants/{restaurantId}/slots`
- `POST /api/v1/events/{eventId}/slot-reservations`

Frontend notes:

- disabled restaurant operation or slot endpoints return `404`; render these as unavailable flows
- enabled endpoints still use normal `401` and `403` handling
- assignment grant uses `{ "username": "..." }`
- slot create/update uses `startsAtUtc`, `endsAtUtc`, `capacity`, `cutoffAtUtc`, and optional `minThresholdForDiscount`
- event details may include nullable `slotReservation` and `discountActivation`; summaries should not depend on them
- discount state is simulation-only and does not imply checkout or payment behavior

### Events

- `GET /api/v1/events`
- `POST /api/v1/events`
- `GET /api/v1/events/{eventId}`
- `PATCH /api/v1/events/{eventId}`
- `GET /api/v1/events/{eventId}/participants`
- `POST /api/v1/events/{eventId}/participants`
- `PATCH /api/v1/events/{eventId}/participants/me`
- `POST /api/v1/events/{eventId}/participants/{userId}/removal`
- `POST /api/v1/events/{eventId}/invites`
- `POST /api/v1/events/{eventId}/cancellation`
- `GET /api/v1/events/{eventId}/messages`

Frontend notes:

- open-event join and closed-event accept can fail with `409` if no seat is available
- participant changes after `DecisionAt` can fail with `409`
- exactly one of `selectedRestaurantId` or `cuisineTarget` should be set when creating an event

### Groups

- `GET /api/v1/groups`
- `POST /api/v1/groups`
- `GET /api/v1/groups/{groupId}`
- `PATCH /api/v1/groups/{groupId}`
- `GET /api/v1/groups/{groupId}/events`
- `POST /api/v1/groups/{groupId}/members`
- `DELETE /api/v1/groups/{groupId}/members/me`
- `POST /api/v1/groups/{groupId}/members/{userId}/removal`
- `POST /api/v1/groups/{groupId}/invites`
- `PATCH /api/v1/groups/invites/{inviteId}`
- `GET /api/v1/groups/{groupId}/messages`

Frontend notes:

- public groups allow direct join
- private groups use invite flow
- group membership does not replace event participation

### Discovery and Budz

- `GET /api/v1/discovery/people`
- `GET /api/v1/discovery/swipe-candidates`
- `POST /api/v1/discovery/swipes`
- `GET /api/v1/budz`

Frontend notes:

- discovery respects privacy settings, blocks, and discovery restrictions
- MVP does not expose pending Bud request state

### Notifications

- `GET /api/v1/notifications`
- `PATCH /api/v1/notifications/{notificationId}`

Current notification types documented for MVP:

- `EventInviteReceived`
- `EventParticipantChanged`
- `EventStatusChanged`
- `EventUpdated`
- `GroupInviteReceived`
- `BudMatchCreated`

### Media

- `GET /api/v1/media/{mediaAssetId}`

Frontend notes:

- media is image-only in the current backend slice
- profile-avatar media is readable by authenticated users
- report-evidence media follows moderation-specific authorization rules

### Reports, moderation, and audit

- `POST /api/v1/reports`
- `POST /api/v1/reports/{reportId}/attachments`
- `GET /api/v1/reports/{reportId}/attachments`
- `GET /api/v1/moderation/reports`
- `GET /api/v1/moderation/reports/{reportId}`
- `PATCH /api/v1/moderation/reports/{reportId}`
- `POST /api/v1/moderation/restrictions`
- `PATCH /api/v1/moderation/restrictions/{restrictionId}`
- `GET /api/v1/audit-logs`

Frontend notes:

- report attachments use multipart form-data with a `file` field
- only the reporting user can upload attachments
- reporting users plus moderator/admin roles can list attachments

Note:

- moderation and audit routes are mainly support or admin flows, not normal end-user frontend flows

## 5. Realtime Chat

The backend supports event chat and group chat in MVP.

Use:

- SignalR hub: `/hubs/chat`
- REST history:
  - `GET /api/v1/events/{eventId}/messages`
  - `GET /api/v1/groups/{groupId}/messages`

Recommended frontend flow:

1. fetch history with REST
2. connect to the SignalR hub with auth
3. subscribe with `JoinScope(scopeType, scopeId)`
4. send messages with `SendMessage({ scopeType, scopeId, body })`
5. listen for `MessageReceived`

Chat constraints:

- text-only
- event chat is only for current joined participants
- group chat is only for current active group members
- leaving or removal revokes access immediately

## 6. Not Ready Yet

These documented routes/features are not implemented yet and should not be used by the frontend:

- group ownership transfer and dissolution
- direct 1-on-1 chat
- payment simulation and checkout behavior

## 7. Recommended Frontend Usage

- treat [implementation-status.md](./implementation-status.md) as the runtime readiness reference
- treat [api-endpoints.md](./api-endpoints.md) as the contract reference
- build against the implemented MVP routes first
- handle `401`, `403`, `404`, and `409` explicitly in UI flows
- avoid depending on development/test seed records beyond documented API contracts
