# QA Hardening Report

Date: April 27, 2026

## Capstone-Readiness Verdict

Mostly ready, with minor risks.

The app has the core shape expected for a strong capstone MVP: authentication, profile, discovery, events, groups, restaurants, messaging/support, notifications, admin, and restaurant-admin flows build and test successfully. The hardening pass found no remaining critical demo blockers after fixes. The main remaining risk is not feature absence; it is that some flows still rely on manual browser verification and split coverage output rather than a clean aggregate coverage gate.

## Documents Reviewed

- `docs/TasteBudz_Functional_Requirements.md`
- `docs/backend/backend-decisions.md`
- `docs/backend/backend-architecture.md`
- `docs/backend/domain-model.md`
- `docs/backend/api-endpoints.md`
- `docs/backend/testing-strategy.md`

The fixes stayed within documented MVP scope and did not change API contracts, domain rules, persistence rules, or backend architecture. No authoritative backend document update was required because the changes were UI error-handling, retry safety, disabled-state consistency, CSS overlap fixes, and MVC regression tests.

## What Was Tested

- Solution restore, build, and full test suite.
- Backend unit tests.
- Backend integration tests.
- MVC integration tests.
- Existing XPlat Code Coverage collection.
- Manual browser smoke flows against the seeded local SQLite development database.
- Negative-path UI behavior for failed discovery swipe submissions and failed notification mark-read requests.

Manual browser flows verified:

- Login as seeded member `alex`.
- Profile/dashboard load.
- Discovery swipe page load and failed swipe retry behavior.
- Notification dropdown mark-read failure feedback.
- Events index load.
- Groups index load.
- Restaurant discovery page load.
- Support chat page load.
- Admin restaurant catalog load as seeded admin `emery`.
- Restaurant admin dashboard load as seeded restaurant admin `gina`.

## Test Commands Used

Baseline:

```powershell
dotnet restore TasteBudz.sln
dotnet build TasteBudz.sln --configuration Debug --no-restore
dotnet test TasteBudz.sln --configuration Debug --no-build --logger "trx;LogFileName=qa-hardening-baseline.trx"
dotnet test TasteBudz.sln --configuration Debug --no-build --collect:"XPlat Code Coverage" --results-directory artifacts\coverage\qa-hardening-baseline --logger "trx;LogFileName=qa-hardening-baseline-coverage.trx"
```

Targeted red/green regression runs:

```powershell
dotnet test tests\TasteBudz.Web.Mvc.IntegrationTests\TasteBudz.Web.Mvc.IntegrationTests.csproj --configuration Debug --filter "FullyQualifiedName~DiscoveryMvcTests|FullyQualifiedName~NotificationsMvcTests|FullyQualifiedName~AuthenticatedLayout_NotificationBellReportsMarkReadFailures|FullyQualifiedName~RestaurantCatalogPage_DisablesBoundaryPaginationConsistently"
dotnet test tests\TasteBudz.Web.Mvc.IntegrationTests\TasteBudz.Web.Mvc.IntegrationTests.csproj --configuration Debug
```

Local manual smoke host:

```powershell
.\start-dev.ps1 -ResetDatabase
```

Final:

```powershell
dotnet build TasteBudz.sln --configuration Debug --no-restore
dotnet test TasteBudz.sln --configuration Debug --no-build --logger "trx;LogFileName=qa-hardening-final-after-browser.trx"
dotnet test TasteBudz.sln --configuration Debug --no-build --collect:"XPlat Code Coverage" --results-directory artifacts\coverage\qa-hardening-final-after-browser --logger "trx;LogFileName=qa-hardening-final-after-browser-coverage.trx"
```

## Initial Test Results

- Restore: passed.
- Build: passed with 0 warnings and 0 errors.
- Full test suite: passed, 290 tests total.
  - Backend unit tests: 119 passed.
  - Backend integration tests: 82 passed.
  - MVC integration tests: 89 passed.

## Coverage Findings

The repository already had `coverlet.collector` and supported `XPlat Code Coverage`, so no new coverage dependency was added.

Coverage collection passed, but output is split by test host/project and is not aggregated into one capstone-readable number. Final generated Cobertura files reported:

- Backend unit coverage slice: line 41.56%, branch 39.28%.
- Backend plus MVC integration coverage slice: line 49.96%, branch 29.06%.
- Backend plus MVC integration coverage slice: line 31.05%, branch 32.8%.

Coverage is useful but not yet presentation-clean. The serious gap is not the exact percentage; it is the lack of a single aggregate report and threshold in CI. Core backend and MVC flows have meaningful tests, but several admin, messaging, provider-parity, and browser-level UX paths still rely more on manual verification than automated guards.

## Findings and Severity

| Severity | Finding | Evidence | Status |
| --- | --- | --- | --- |
| High | Discovery swipe could remove the visible card before confirming that the backend recorded the swipe. | A failed `POST /Discovery/RecordSwipe` left the user thinking the swipe succeeded and advanced the queue. | Fixed with retry-safe UI behavior and regression test. |
| High | Notification mark-read failures were silent. | Failed mark-read requests were caught without user feedback, making the notification state look unreliable. | Fixed with visible error feedback, button re-enable behavior, and regression tests. |
| Medium | Admin restaurant catalog pagination used inconsistent disabled-state markup. | Existing styles expected `is-disabled`, while the catalog link used `disabled`, producing weaker UX and accessibility semantics. | Fixed with consistent disabled class, `aria-disabled`, and regression test. |
| Medium | Floating Help/chat controls overlapped swipe actions on mobile. | Browser smoke screenshot showed the fixed bottom actions competing with swipe pass/like controls on the discovery page. | Fixed by hiding floating support controls on mobile swipe pages. |
| Medium | Coverage output is split and not enforced by a CI threshold. | XPlat coverage succeeds but produces separate non-aggregated Cobertura files. | Remaining risk. Keep existing collector; add aggregation/gating only if time allows. |
| Medium | Some important matrix paths deserve more automated tests. | Messaging authorization, admin moderation/catalog edge cases, and provider-parity scenarios have partial coverage but not exhaustive browser or integration coverage. | Remaining risk. No broad rewrite made. |
| Medium | Some forms still rely on page-level validation feedback rather than consistently focused inline field feedback. | UX review found forms that technically work but can feel generic after submission errors. | Remaining risk. Not changed to avoid broad form churn late in stabilization. |
| Low | Support terminology is slightly duplicated between Help and Support Chat surfaces. | Floating help and support navigation can appear as two similar concepts. | Remaining risk. Low-risk wording cleanup, but not needed for capstone readiness. |

No critical issue remained after the hardening pass.

## Fixes Implemented

- Made the discovery swipe UI retry-safe:
  - Prevents duplicate swipe submissions while a request is in flight.
  - Does not remove the current card until the backend request succeeds.
  - Restores the card and shows a visible retry message on failure.

- Improved notification failure feedback:
  - Notification bell mark-read failures now show a visible error message.
  - Notification page inline mark-read failures now show a visible error message.
  - Buttons are disabled during the request and re-enabled after failure.

- Cleaned up admin pagination disabled state:
  - Added consistent `is-disabled` styling.
  - Added `aria-disabled` and boundary `tabindex` handling.

- Fixed mobile discovery action overlap:
  - Floating support/help controls are hidden on mobile swipe pages so swipe actions remain usable.

## Tests Added

- `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/DiscoveryMvcTests.cs`
  - Verifies the discovery swipe page renders the retry-safe failure message and script behavior for swipe POST failures.

- `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/NotificationsMvcTests.cs`
  - Verifies the notifications page renders visible failure feedback for inline mark-read actions.

- `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/HomeMvcTests.cs`
  - Added coverage that the authenticated notification bell reports mark-read failures.

- `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/AdminRestaurantCatalogMvcTests.cs`
  - Added coverage that admin restaurant catalog boundary pagination renders disabled state consistently.

MVC integration test count increased from 89 to 93. Full suite count increased from 290 to 294.

## Regression Risks Considered

- Discovery swipe behavior was intentionally changed only around failure and duplicate-submit handling. Successful swipe behavior is preserved.
- Notification mark-read success behavior is preserved; only failure feedback and in-flight button state changed.
- Admin pagination route/query behavior is preserved; only disabled-state markup and accessibility attributes changed.
- CSS change for mobile floating support controls is scoped to pages containing `.swipe-area`.
- Existing backend domain, API, persistence, and authorization behavior were not rewritten.

One worktree caveat: during final verification, additional MVC view/CSS polish edits were present in account, event, messaging, and profile views. They were not part of the targeted QA fixes documented above, and they were not reverted because they existed in the shared worktree. The final build, full tests, and browser smoke checks passed with those edits present.

## Existing Behavior Verified As Still Working

- Seeded member login.
- Profile/dashboard load.
- Discovery page load with seeded candidate cards.
- Successful page navigation to events, groups, restaurants, and support chat.
- Admin restaurant catalog access for seeded admin user.
- Restaurant admin dashboard access for seeded restaurant admin user.
- Full backend unit, backend integration, and MVC integration suites.

## Final Test Results

- Final build: passed with 0 warnings and 0 errors.
- Final full test suite: passed, 294 tests total.
  - Backend unit tests: 119 passed.
  - Backend integration tests: 82 passed.
  - MVC integration tests: 93 passed.
- Final coverage collection: passed.

During one final build attempt, the locally started MVC app kept `TasteBudz.Web.Mvc.dll` locked. Stopping that local dev host resolved the lock, and the subsequent final build and test suite passed.

## Remaining Known Risks

- No single aggregate coverage report or CI coverage threshold is currently produced.
- Manual browser smoke checks are not yet codified as Playwright end-to-end tests.
- Admin, moderation, messaging authorization, and provider-parity scenarios would benefit from deeper regression coverage.
- Some form validation paths could be more polished with field-local feedback and focus management.
- Live Azure SQL or SQL Server provider smoke testing was not part of this local SQLite-focused pass.

## Recommended Final Presentation Checklist

- Start from a clean local database with `.\start-dev.ps1 -ResetDatabase`.
- Confirm seeded login credentials before the presentation.
- Demo as a normal member first: profile, discovery, events, groups, restaurants, messaging/support.
- Demo one negative-path polish moment only if useful: failed swipe now keeps the card and asks the user to retry.
- Demo admin and restaurant-admin screens after member flow, not before.
- Keep the browser viewport at desktop size for the main demo; mobile has been smoke-checked, but desktop is the strongest presentation path.
- Run `dotnet test TasteBudz.sln --configuration Debug --no-build` before presenting.
- Avoid presenting coverage as a polished KPI until aggregate reporting is added; present it honestly as existing automated regression coverage.
