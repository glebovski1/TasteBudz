# Group Hub Announcements and Food Wallpaper Design

Date: 2026-04-24

## Goal

Improve the Groups experience so each group page feels polished, aligned, personal, and complete across desktop and mobile. The group page should preserve the existing TasteBudz visual language while adding first-class group announcements and owner-selected food-themed wallpaper presets.

## Documents Reviewed

- `docs/TasteBudz_Functional_Requirements.md`
- `docs/backend/backend-decisions.md`
- `docs/backend/backend-architecture.md`
- `docs/backend/domain-model.md`
- `docs/backend/api-endpoints.md`
- `docs/backend/testing-strategy.md`

## Current Context

The current MVC group area already has or is being updated toward:

- group browse and create pages
- group detail/manage page
- active member list with profile-style person cards
- group chat link into the existing messaging screen
- group-linked event history
- owner settings and private-group invite tools

The authoritative docs already support current member lists, group chat, and group-linked event history. They do not currently define owner-authored group announcements or group wallpaper personalization, so this feature requires coordinated code, API, schema, test, and documentation updates.

## Scope

In scope:

- Add first-class group announcements.
- Let group owners write manual announcement posts.
- Automatically create a system announcement when a group-linked event is created.
- Let group owners choose a preset food-themed wallpaper for the group page.
- Preserve the existing TasteBudz warm card-based visual style.
- Verify the updated UI at desktop and mobile widths.

Out of scope:

- Uploaded custom wallpaper images.
- Announcement attachments, reactions, comments, edits, pinning, scheduling, or rich text.
- Replacing group chat with inline chat on the group page.
- Changing group membership, ownership transfer, or group dissolution rules.
- Changing event participation or event feedback rules.

## UX Design

The group detail page becomes a group hub while staying visually consistent with the existing application:

- A warm food-themed hero area uses an owner-selected wallpaper preset such as sushi, tacos, brunch, ramen, pizza, or dumplings.
- The existing rounded cards, cream/orange palette, soft shadows, pills, and button styles remain the baseline.
- A member gallery appears near the top so the page clearly shows the person list and social context.
- An announcement panel shows owner posts and system event announcements.
- Linked event history remains visible with event status, participant count, and completed-event feedback where allowed.
- Group chat remains the existing real-time chat screen, linked prominently from the group page.
- Owner tools include group settings, private-group invites when applicable, announcement composer, and wallpaper theme selection.

Responsive expectations:

- Desktop can use a two-column layout with people/events in the main column and announcements/chat/settings in the side column.
- Tablet and mobile collapse into one column.
- Primary actions become full-width on narrow screens.
- Member cards, event cards, and announcement cards must avoid overlap, clipping, and horizontal scrolling.

## Backend Design

### GroupAnnouncement

Add a `GroupAnnouncement` concept owned by the Groups module.

Fields:

- `AnnouncementId`
- `GroupId`
- `AuthorUserId`, nullable for system announcements
- `Kind`, with values `Manual` and `System`
- `Title`
- `Body`
- `CreatedAtUtc`

Rules:

- Manual announcements are created only by the current group owner.
- System announcements are created by server-side workflows only.
- Clients cannot request `Kind = System`.
- Announcement visibility follows existing group visibility and group page access rules.
- Public group announcements are visible to authenticated users who can view the public group page.
- Private group announcements are visible only to users who can view that private group.

### WallpaperTheme

Add a constrained `WallpaperTheme` value to `Group`.

MVP preset values:

- `Default`
- `Sushi`
- `Tacos`
- `Brunch`
- `Ramen`
- `Pizza`
- `Dumplings`

Rules:

- Group owner can update the wallpaper theme through group settings.
- Invalid theme values are rejected server-side.
- Theme selection is presentation metadata only and must not affect group visibility, membership, announcements, events, or chat access.

### Uploaded Wallpaper Later

Uploaded wallpapers can reuse the existing database-backed media infrastructure later, but they require a separate `GroupWallpaper` media context with owner-only write rules, appropriate read rules, media validation, schema/API changes, and tests. This is intentionally excluded from the MVP slice.

## API Design

Update existing group contracts:

- `GroupDetailDto` includes `wallpaperTheme`.
- `UpdateGroupRequest` accepts optional `wallpaperTheme`.

Add endpoints:

- `GET /api/v1/groups/{groupId}/announcements`
- `POST /api/v1/groups/{groupId}/announcements`

Manual announcement request:

```json
{
  "title": "Friday plan",
  "body": "Poll is open for Friday: sushi downtown or ramen in Over-the-Rhine?"
}
```

Announcement response:

```json
{
  "announcementId": "uuid",
  "groupId": "uuid",
  "authorUserId": "uuid",
  "authorDisplayName": "Maya Chen",
  "kind": "Manual",
  "title": "Friday plan",
  "body": "Poll is open for Friday: sushi downtown or ramen in Over-the-Rhine?",
  "createdAtUtc": "timestamp"
}
```

System event announcement example:

```json
{
  "kind": "System",
  "title": "New group event",
  "body": "New group event created: Ramen Backup Plan."
}
```

Contract notes:

- Announcement list responses use the existing list envelope style where practical.
- Returned announcements should be newest first.
- Manual title/body values are trimmed and length-limited.
- System announcement creation happens inside the event creation workflow when a new event is linked to a group.

## Implementation Boundaries

Groups module owns:

- announcement entity/DTO/request contracts
- announcement authorization
- wallpaper theme validation
- announcement persistence and retrieval

Events module integration:

- When a group-linked event is successfully created, the event creation workflow asks the Groups module to create a system event announcement.
- Only owner-approved group-linked event creation remains valid. Group membership still does not replace event participation.

MVC frontend owns:

- group hub layout
- announcement composer and list rendering
- wallpaper preset selector
- responsive alignment and visual polish
- linking to the existing group chat screen

## Documentation Updates Required During Implementation

- `docs/TasteBudz_Functional_Requirements.md`: add owner-authored announcements and preset wallpaper personalization under group MVP behavior.
- `docs/backend/backend-decisions.md`: record the accepted MVP decision for first-class group announcements and preset-only wallpapers.
- `docs/backend/domain-model.md`: add `GroupAnnouncement` and `WallpaperTheme`.
- `docs/backend/api-endpoints.md`: document announcement endpoints and group `wallpaperTheme`.
- `docs/backend/testing-strategy.md`: add announcement and wallpaper coverage under group tests.

## Test Plan

Backend tests:

- Group owner can create a manual announcement.
- Non-owner cannot create a manual announcement.
- Clients cannot create system announcements directly.
- Public group announcements are visible to authenticated users who can view the public group.
- Private group announcements are hidden from non-members.
- Creating a group-linked event creates one system announcement.
- Updating wallpaper theme is owner-only.
- Invalid wallpaper theme values are rejected.

MVC/UI verification:

- Group detail page renders member gallery, announcement panel, linked event history, chat entry, and owner tools.
- Owner announcement composer appears only for owner.
- Wallpaper theme selector appears only for owner.
- Public/non-owner viewers see announcements but not owner composer/settings.
- Desktop viewport has aligned two-column layout.
- Mobile viewport collapses cleanly without clipping, overlap, or horizontal scroll.

## Risks and Simplifications

Risk: event creation and group announcements cross module boundaries.

Mitigation: expose a narrow Groups-module service method for system announcements instead of letting Events write group persistence directly.

Risk: wallpaper uploads would expand media, moderation, and storage scope.

Mitigation: use preset themes for MVP and document uploaded images as later work.

Risk: announcements can become a second chat.

Mitigation: keep announcements owner/system-only, simple, and read-only for members except owner creation. Group chat remains the place for conversation.

## Approval State

Approved decisions:

- Use separate first-class announcement posts.
- Group owner, not global Admin, controls announcements and wallpaper.
- Announcement visibility follows group page visibility.
- Preserve the existing TasteBudz app style.
- Use food-themed preset wallpapers for MVP.
- Defer uploaded custom wallpaper images.
