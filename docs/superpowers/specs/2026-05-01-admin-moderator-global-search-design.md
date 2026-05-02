# Admin and Moderator Global Search Design

Date: 2026-05-01

## Goal

Add an Admin/Moderator search and review experience that helps staff quickly inspect users, messages, reports, and other app content when handling safety issues. The primary moderation workflow must let a reviewer see the person they are considering banning as a human-readable user link, not as a raw GUID.

## Documents Reviewed

- `docs/TasteBudz_Functional_Requirements.md`
- `docs/backend/backend-decisions.md`
- `docs/backend/backend-architecture.md`
- `docs/backend/domain-model.md`
- `docs/backend/api-endpoints.md`
- `docs/backend/testing-strategy.md`

## Approved Direction

Provide one global search surface for both `Admin` and `Moderator` roles. Keep it database-backed and scoped to the current modular monolith. Do not introduce a full search engine, background indexing job, or broad architecture change.

The search page should group results by content type so staff can quickly move between:

- Users
- Messages
- Reports
- Events
- Groups
- Event feedback
- Restaurants
- Audit entries when the current role is allowed to see them

Admin and Moderator users may use the global search page. Admin-only actions remain admin-only. For example, audit-log review remains Admin-only unless the authoritative documents are explicitly changed later.

## User Links in Report Review

Report review must not rely on raw GUIDs as the main user-facing identifier.

Reporter, reported user, related user, message sender, and content author references should render as:

`Display Name (@username)`

Each user label should link to an Admin/Moderator user detail page. That user detail page is the staff review hub for deciding whether to ban or apply restrictions.

GUIDs may remain visible as secondary technical metadata, but only after the readable user link. If a user profile or account cannot be found, show `Unknown user` and keep the GUID as fallback traceability.

Report target resolution should follow these rules:

- User report: the ban subject is the target user.
- Message report: the ban subject is the message sender, with related message context shown.
- Event feedback report: the ban subject is the feedback author.
- Other content report with `RelatedUserId`: the ban subject is the related user.
- If no subject user can be resolved, show the report context but do not show a ban shortcut.

## Search UX

Add an Admin Panel entry point named `Search` or `Content Search`.

The search page should support:

- A single query box.
- Optional content-type filter.
- Paged results.
- Result sections with short snippets and direct action links.
- Links back to report detail, user detail, event detail, group detail, support thread, or restaurant catalog where existing routes allow it.

Search results should be readable and action-oriented:

- User results show display name, username, email, account status, roles, and active restriction summary.
- Message results show sender link, scope type, created time, and a body snippet.
- Report results show reporter link, subject link when resolvable, status, category, reason, and created time.
- Event results show title or fallback label, host link, status, start time, restaurant or cuisine context.
- Group results show name, owner link, visibility, lifecycle state.
- Feedback results show author link, event link, rating, and text snippet.
- Restaurant results show name, location, archive state, and catalog link.
- Audit results show actor link, action type, target metadata, and timestamp. Audit results are visible only to Admins unless policy changes.

Do not put destructive actions directly on search result cards. Use links into review pages where existing actions and anti-forgery forms already live.

## Backend Contract

Add a moderation-owned read API such as:

`GET /api/v1/moderation/search?q=&type=&page=&pageSize=`

The endpoint requires `Moderator` or `Admin`.

Return DTOs, not persistence entities. A practical response shape:

- `query`
- `type`
- `items`
- `totalCount`
- result item fields:
  - `kind`
  - `id`
  - `title`
  - `subtitle`
  - `snippet`
  - `createdAtUtc`
  - `primaryUser`
  - `secondaryUser`
  - `targetUrl` or route metadata for MVC
  - type-specific metadata where needed

The search should use simple database queries over the current relational store. It may fan out to module repositories or a moderation-owned query service that reads through existing repository boundaries where practical. If a single service needs cross-module read access, keep it read-only and DTO-focused.

## Authorization and Privacy

This is a privileged staff surface. It intentionally sees more than normal user search and browse flows, but it should still obey role boundaries:

- `Admin` and `Moderator` can search moderation-relevant content.
- Admin-only operational capabilities remain Admin-only.
- Audit-log visibility stays Admin-only under the current documents.
- Normal users cannot access any global search endpoint or MVC page.
- Clients do not control lifecycle state, restriction state, moderation resolution, or ban scope.

The result should preserve historical moderation context. It should not delete or hide completed history just because normal user-facing views filter blocked users.

## Technical Shape

Add a small Moderation/Admin search slice:

- DTOs under `src/TasteBudz.Backend/Modules/Moderation/DTOs`.
- A query service under `src/TasteBudz.Backend/Modules/Moderation`.
- A controller endpoint on `ModerationController` or a dedicated `ModerationSearchController`.
- MVC service wrapper in `ModerationApiService`.
- MVC view model and `AdminController.Search`.
- MVC view under `Views/Admin/Search.cshtml`.
- Report list/detail view models should include resolved user display data instead of rendering GUIDs directly.

User identity summaries should be reusable:

- User id
- Username
- Display name
- Email if appropriate for staff review
- Account status
- Roles

Keep snippets short and HTML-encoded by the Razor view. Do not render message or report text as raw HTML.

## Documentation Updates Required During Implementation

Update:

- `docs/TasteBudz_Functional_Requirements.md`: Admin/Moderator can search users, messages, reports, and content for moderation review.
- `docs/backend/backend-architecture.md`: Moderation/Admin search is a privileged read surface over existing modules.
- `docs/backend/api-endpoints.md`: document the global search endpoint and report review identity-link behavior.
- `docs/backend/testing-strategy.md`: add coverage for role access, normal-user denial, user-link resolution, message-author search, and report subject resolution.

Add a backend decision only if implementation goes beyond simple relational query search, such as adding a search index, background indexing, or external search dependency. The approved MVP direction does not need that.

## Tests

Minimum test coverage:

- Admin can call global search.
- Moderator can call global search.
- Normal user cannot call global search.
- Search by username/display name returns a user result.
- Search by message body returns the message with sender identity summary.
- Report detail data resolves reporter and ban subject user labels.
- Message report resolves the message sender as the ban subject.
- Search result paging is stable.
- Audit results are omitted or denied for Moderator if audit policy remains Admin-only.

MVC coverage should verify:

- Admin search page renders result groups and links.
- Moderator search page renders search results.
- Report detail renders user names/links instead of GUID-only user identifiers.
- Ban form uses the resolved ban subject id while displaying the resolved subject label.

## Scope Boundaries

In scope:

- Privileged search for Admin and Moderator.
- Human-readable user links in report review.
- Staff user detail page as a moderation review hub.
- Simple text search and paging over existing persisted data.
- Documentation alignment.

Out of scope:

- External search engines.
- Background indexing.
- Fuzzy ranking, stemming, typo correction, or autocomplete.
- Bulk moderation actions.
- Replacing existing report, restriction, audit, or support workflows.
- Making audit-log review Moderator-visible unless explicitly approved as a separate policy change.

## Risks

- A broad search endpoint can leak sensitive data if role checks are weak.
- Report review can still be confusing if the target, related user, and content author are not clearly labeled.
- Cross-module reads can blur boundaries if the implementation exposes persistence entities instead of DTOs.
- Large result sets can slow the admin page if the first slice tries to load too much at once.

## Recommendation

Proceed with the simple database-backed Admin/Moderator global search. Use it as a moderation review accelerator, not as a new platform-wide search subsystem. First-class user identity links in report review are required for usability and safety.
