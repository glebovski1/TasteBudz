# Admin Moderator Global Search Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a privileged Admin/Moderator search surface and replace GUID-first report review with readable user links.

**Architecture:** Add a moderation-owned read query service over existing relational tables, returning DTOs only. MVC consumes that service through `ModerationApiService`, rendering a search page, user detail page, and report-review identity summaries without changing normal user-facing authorization rules.

**Tech Stack:** ASP.NET Core MVC/API on .NET 9, EF Core relational persistence, xUnit integration tests, existing Azure App Service deployment scripts.

---

### Task 1: Backend Search And Review Contracts

**Files:**
- Create: `src/TasteBudz.Backend/Modules/Moderation/DTOs/ModerationSearchDtos.cs`
- Modify: `src/TasteBudz.Backend/Controllers/ModerationController.cs`
- Modify: `src/TasteBudz.Backend/Infrastructure/Configuration/ServiceCollectionExtensions.cs`
- Create/modify tests: `tests/TasteBudz.Backend.IntegrationTests/Api/ModerationApiTests.cs`

- [ ] **Step 1: Write failing backend API tests**

Add tests proving Admin and Moderator can search, normal users cannot, message search returns sender identity, and report review resolves the subject user.

- [ ] **Step 2: Run backend moderation tests and verify RED**

Run: `dotnet test tests/TasteBudz.Backend.IntegrationTests/TasteBudz.Backend.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~ModerationApiTests`

Expected: fails because `/api/v1/moderation/search` and report review/user detail DTOs do not exist.

- [ ] **Step 3: Add DTOs and query service**

Add `ModerationSearchQuery`, `ModerationSearchResultKind`, `ModerationUserSummaryDto`, `ModerationSearchResultDto`, `ModerationSearchResponseDto`, `ModerationReportReviewDto`, and `ModerationUserDetailDto`.

Implement a scoped `ModerationSearchService` that:

- searches users, messages, reports, events, groups, feedback, restaurants, and admin-only audit entries
- resolves report reporter, related user, and ban subject
- resolves a message report subject to the message sender
- returns short snippets and readable user summaries

- [ ] **Step 4: Expose moderation endpoints**

Add:

- `GET /api/v1/moderation/search`
- `GET /api/v1/moderation/reports/{reportId}/review`
- `GET /api/v1/moderation/users/{userId}`

Keep `[Authorize(Roles = "Moderator,Admin")]` at the controller boundary.

- [ ] **Step 5: Run backend tests and verify GREEN**

Run: `dotnet test tests/TasteBudz.Backend.IntegrationTests/TasteBudz.Backend.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~ModerationApiTests`

Expected: pass.

### Task 2: MVC Search, User Links, And Report Review UI

**Files:**
- Modify: `src/TasteBudz.Web.Mvc/Services/BackendApi/ModerationApiService.cs`
- Modify: `src/TasteBudz.Web.Mvc/ViewModels/AdminViewModel.cs`
- Modify: `src/TasteBudz.Web.Mvc/Controllers/AdminController.cs`
- Create: `src/TasteBudz.Web.Mvc/Views/Admin/Search.cshtml`
- Create: `src/TasteBudz.Web.Mvc/Views/Admin/UserDetail.cshtml`
- Modify: `src/TasteBudz.Web.Mvc/Views/Admin/Index.cshtml`
- Modify: `src/TasteBudz.Web.Mvc/Views/Admin/Reports.cshtml`
- Modify: `src/TasteBudz.Web.Mvc/Views/Admin/ReportDetail.cshtml`
- Modify tests: `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/AdminMvcTests.cs`
- Modify tests: `tests/TasteBudz.Web.Mvc.IntegrationTests/Services/ModerationApiServiceTests.cs`

- [ ] **Step 1: Write failing MVC tests**

Add tests for:

- service wrapper builds `/api/v1/moderation/search`
- Admin/Moderator search page renders grouped result links
- report detail renders `Display Name (@username)` links instead of GUID-only user labels
- ban form still posts the resolved subject id

- [ ] **Step 2: Run MVC tests and verify RED**

Run: `dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AdminMvcTests|FullyQualifiedName~ModerationApiServiceTests"`

Expected: fails because service methods and MVC pages do not exist.

- [ ] **Step 3: Implement MVC service and controller actions**

Add `SearchAsync`, `GetReportReviewAsync`, and `GetUserDetailAsync` to `ModerationApiService`.

Add `AdminController.Search` and `AdminController.UserDetail`. Change `ReportDetail` to use the review DTO.

- [ ] **Step 4: Implement Razor views**

Render a search form, result cards, user links, and a user detail review page. Update report lists/detail to show readable links for reporter and subject users, keeping GUIDs as secondary metadata.

- [ ] **Step 5: Run MVC tests and verify GREEN**

Run: `dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AdminMvcTests|FullyQualifiedName~ModerationApiServiceTests"`

Expected: pass.

### Task 3: Documentation Alignment

**Files:**
- Modify: `docs/TasteBudz_Functional_Requirements.md`
- Modify: `docs/backend/backend-architecture.md`
- Modify: `docs/backend/api-endpoints.md`
- Modify: `docs/backend/testing-strategy.md`

- [ ] **Step 1: Update authoritative docs**

Document Admin/Moderator global search, report-review identity links, and the new moderation search endpoints.

- [ ] **Step 2: Re-check consistency**

Search the changed docs for conflicting statements about Moderator/Admin search scope or audit visibility.

### Task 4: Full Validation And Azure Publish

**Files:**
- Deployment uses existing scripts under `.agents/skills/azure-app-service-deployment/scripts`.

- [ ] **Step 1: Run Release validation**

Run:

```powershell
dotnet restore TasteBudz.sln
dotnet build TasteBudz.sln -c Release --no-restore
dotnet test TasteBudz.sln -c Release --no-build
git diff --check
```

Expected: all pass.

- [ ] **Step 2: Check deployment safety**

Confirm there are no production SQL schema changes required by this feature. If only code/docs changed, use the normal code-only Azure update script.

- [ ] **Step 3: Publish existing App Service**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1
```

Expected: script restores, builds, tests, deploys, and smoke-checks production.

- [ ] **Step 4: Verify production smoke**

Confirm homepage `200`, unauthenticated restaurants API `401`, and SignalR negotiate `401`.
