# App Visual Polish and Group Covers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the approved B+C visual direction with realistic general imagery, richer group/event cards, and owner-selected built-in group cover presets.

**Architecture:** Keep the existing ASP.NET Core modular monolith and MVC frontend. Reuse the current `GroupWallpaperTheme` model, group owner settings flow, and MVC Razor/CSS patterns; do not add admin cover management, user uploads, or new media infrastructure.

**Tech Stack:** ASP.NET Core MVC on .NET 9, Razor views, existing backend DTO/service layer, source-controlled SQLite/SQL Server schema, xUnit integration/unit tests, Playwright/browser visual verification.

---

## File Structure

- Modify `src/TasteBudz.Backend/Domain/CommonEnums.cs`
  - Add realistic and illustrated group cover preset enum values while preserving existing values.
- Modify `src/TasteBudz.Backend/Modules/Groups/DTOs/CreateGroupRequest.cs`
  - Allow initial owner-selected cover theme at group creation.
- Modify `src/TasteBudz.Backend/Modules/Groups/DTOs/GroupSummaryDto.cs`
  - Return cover theme in public group browse summaries.
- Modify `src/TasteBudz.Backend/Modules/Profiles/DTOs/DashboardGroupSummaryDto.cs`
  - Return cover theme in current-user group summaries.
- Modify `src/TasteBudz.Backend/Modules/Groups/GroupService.cs`
  - Persist create-time cover theme and include cover theme in browse DTOs.
- Modify `src/TasteBudz.Backend/Modules/Profiles/DashboardService.cs`
  - Include cover theme in dashboard group summaries.
- Modify `src/TasteBudz.Web.Mvc/ViewModels/GroupViewModels.cs`
  - Surface cover theme data in group index, my-groups cards, create form, preview, and manage page.
- Modify `src/TasteBudz.Web.Mvc/Views/Group/CreateGroup.cshtml`
  - Add owner cover selector and live preview theme switching.
- Modify `src/TasteBudz.Web.Mvc/Views/Group/Index.cshtml`
  - Render public and my-group cards as photo/thematic cover cards.
- Modify `src/TasteBudz.Web.Mvc/Views/Group/Manage.cshtml`
  - Refine the group hub to match the B+C concept and improve cover selector presentation.
- Modify `src/TasteBudz.Web.Mvc/wwwroot/css/site.css`
  - Add the B+C shell polish, local realistic cover backgrounds, illustrated cover backgrounds, overlay contrast, and responsive fixes.
- Create `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/*.png`
  - Store the static, built-in cover art used by CSS theme presets.
- Modify `tests/TasteBudz.Backend.UnitTests/Groups/GroupServiceTests.cs`
  - Prove create-time cover selection and browse cover propagation.
- Modify `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs`
  - Prove create/manage pages render cover selector and owner-selected class.
- Modify `tests/TasteBudz.Web.Mvc.IntegrationTests/Services/GroupApiServiceTests.cs`
  - Update DTO constructor usage and assert create payload includes `wallpaperTheme`.
- Modify authoritative docs if code changes persisted/API-visible contracts:
  - `docs/TasteBudz_Functional_Requirements.md`
  - `docs/backend/domain-model.md`
  - `docs/backend/api-endpoints.md`
  - `docs/backend/testing-strategy.md`

---

### Task 1: Backend Cover Contract

**Files:**
- Modify: `src/TasteBudz.Backend/Domain/CommonEnums.cs`
- Modify: `src/TasteBudz.Backend/Modules/Groups/DTOs/CreateGroupRequest.cs`
- Modify: `src/TasteBudz.Backend/Modules/Groups/DTOs/GroupSummaryDto.cs`
- Modify: `src/TasteBudz.Backend/Modules/Profiles/DTOs/DashboardGroupSummaryDto.cs`
- Modify: `src/TasteBudz.Backend/Modules/Groups/GroupService.cs`
- Modify: `src/TasteBudz.Backend/Modules/Profiles/DashboardService.cs`
- Test: `tests/TasteBudz.Backend.UnitTests/Groups/GroupServiceTests.cs`

- [ ] **Step 1: Write failing group service tests**

Add these tests after `UpdateAsync_OwnerCanSetWallpaperTheme` in `tests/TasteBudz.Backend.UnitTests/Groups/GroupServiceTests.cs`:

```csharp
[Fact]
public async Task CreateAsync_UsesOwnerSelectedWallpaperTheme()
{
    var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");

    var detail = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
    {
        Name = "Brunch Budz",
        Visibility = GroupVisibility.Public,
        WallpaperTheme = GroupWallpaperTheme.CoffeeBrunchIllustrated,
    });

    Assert.Equal(GroupWallpaperTheme.CoffeeBrunchIllustrated, detail.WallpaperTheme);
}

[Fact]
public async Task BrowseAsync_ReturnsWallpaperThemeForPublicGroups()
{
    var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
    var viewer = await RegisterAsync(services.AuthService, "viewer", "viewer@example.com");

    await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
    {
        Name = "Taco Budz",
        Visibility = GroupVisibility.Public,
        WallpaperTheme = GroupWallpaperTheme.TacoTableRealistic,
    });

    var results = await services.GroupService.BrowseAsync(viewer.CurrentUser.UserId, new BrowseGroupsQuery());

    var group = Assert.Single(results.Items);
Assert.Equal(GroupWallpaperTheme.TacoTableRealistic, group.WallpaperTheme);
}

[Fact]
public async Task CreateAsync_RejectsUnsupportedWallpaperTheme()
{
    var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");

    var exception = await Assert.ThrowsAsync<ApiException>(() =>
        services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Unsupported cover",
            Visibility = GroupVisibility.Public,
            WallpaperTheme = (GroupWallpaperTheme)999,
        }));

    Assert.Equal(400, exception.StatusCode);
    Assert.Contains("wallpaperTheme", exception.Detail, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task UpdateAsync_RejectsUnsupportedWallpaperTheme()
{
    var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
    var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
    {
        Name = "Unsupported update cover",
        Visibility = GroupVisibility.Public,
    });

    var exception = await Assert.ThrowsAsync<ApiException>(() =>
        services.GroupService.UpdateAsync(ToCurrentUser(owner), group.GroupId, new UpdateGroupRequest
        {
            WallpaperTheme = (GroupWallpaperTheme)999,
        }));

    Assert.Equal(400, exception.StatusCode);
    Assert.Contains("wallpaperTheme", exception.Detail, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test tests/TasteBudz.Backend.UnitTests/TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~GroupServiceTests"
```

Expected: FAIL because `CreateGroupRequest.WallpaperTheme`, `GroupSummaryDto.WallpaperTheme`, unsupported theme validation, and the new enum values do not exist yet.

- [ ] **Step 3: Extend the enum without renumbering existing values**

Modify `GroupWallpaperTheme` in `src/TasteBudz.Backend/Domain/CommonEnums.cs` to:

```csharp
public enum GroupWallpaperTheme
{
    Default,
    PizzaNight,
    SushiBar,
    TacoTable,
    CoffeeBrunch,
    GardenFresh,
    PizzaNightRealistic,
    SushiBarRealistic,
    TacoTableRealistic,
    CoffeeBrunchRealistic,
    GardenFreshRealistic,
    CurryNightRealistic,
    NoodleHouseRealistic,
    PizzaNightIllustrated,
    SushiBarIllustrated,
    TacoTableIllustrated,
    CoffeeBrunchIllustrated,
    GardenFreshIllustrated,
    CurryNightIllustrated,
    NoodleHouseIllustrated,
}
```

- [ ] **Step 4: Add cover theme to create and summary DTOs**

Modify `src/TasteBudz.Backend/Modules/Groups/DTOs/CreateGroupRequest.cs`:

```csharp
public sealed class CreateGroupRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(80)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; init; }

    [Required]
    public GroupVisibility? Visibility { get; init; }

    public GroupWallpaperTheme? WallpaperTheme { get; init; }
}
```

Modify `src/TasteBudz.Backend/Modules/Groups/DTOs/GroupSummaryDto.cs`:

```csharp
public sealed record GroupSummaryDto(
    Guid GroupId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupWallpaperTheme WallpaperTheme,
    int ActiveMembers);
```

Modify `src/TasteBudz.Backend/Modules/Profiles/DTOs/DashboardGroupSummaryDto.cs`:

```csharp
public sealed record DashboardGroupSummaryDto(
    Guid GroupId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupWallpaperTheme WallpaperTheme,
    int ActiveMemberCount);
```

- [ ] **Step 5: Wire cover theme through services**

In `src/TasteBudz.Backend/Modules/Groups/GroupService.cs`, update the browse DTO mapping:

```csharp
filtered.Add(new GroupSummaryDto(
    group.Id,
    group.Name,
    group.Description,
    group.Visibility,
    group.WallpaperTheme,
    members.Count(member => member.State == GroupMemberState.Active)));
```

Update create-time group construction:

```csharp
var wallpaperTheme = NormalizeWallpaperTheme(request.WallpaperTheme);
var group = new Group(groupId, currentUser.UserId, name, description, visibility, wallpaperTheme, GroupLifecycleState.Active, now, now);
```

Update the `UpdateAsync` assignment:

```csharp
WallpaperTheme = request.WallpaperTheme.HasValue
    ? NormalizeWallpaperTheme(request.WallpaperTheme)
    : group.WallpaperTheme,
```

Add this private helper near the other private normalization helpers in `GroupService`:

```csharp
private static GroupWallpaperTheme NormalizeWallpaperTheme(GroupWallpaperTheme? theme)
{
    if (theme is null)
    {
        return GroupWallpaperTheme.Default;
    }

    if (!Enum.IsDefined(theme.Value))
    {
        throw ApiException.BadRequest("wallpaperTheme is invalid.");
    }

    return theme.Value;
}
```

In `src/TasteBudz.Backend/Modules/Profiles/DashboardService.cs`, update my-groups mapping:

```csharp
return groups
    .Select(group => new DashboardGroupSummaryDto(
        group.GroupId,
        group.Name,
        group.Description,
        group.Visibility,
        group.WallpaperTheme,
        group.ActiveMemberCount))
    .ToArray();
```

- [ ] **Step 6: Run backend group tests**

Run:

```powershell
dotnet test tests/TasteBudz.Backend.UnitTests/TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~GroupServiceTests"
```

Expected: PASS.

- [ ] **Step 7: Commit backend contract slice**

Run:

```powershell
git add src/TasteBudz.Backend/Domain/CommonEnums.cs src/TasteBudz.Backend/Modules/Groups/DTOs/CreateGroupRequest.cs src/TasteBudz.Backend/Modules/Groups/DTOs/GroupSummaryDto.cs src/TasteBudz.Backend/Modules/Profiles/DTOs/DashboardGroupSummaryDto.cs src/TasteBudz.Backend/Modules/Groups/GroupService.cs src/TasteBudz.Backend/Modules/Profiles/DashboardService.cs tests/TasteBudz.Backend.UnitTests/Groups/GroupServiceTests.cs
git commit -m "Add group cover themes to backend contracts"
```

---

### Task 2: MVC View Models and API Service Tests

**Files:**
- Modify: `src/TasteBudz.Web.Mvc/ViewModels/GroupViewModels.cs`
- Modify: `tests/TasteBudz.Web.Mvc.IntegrationTests/Services/GroupApiServiceTests.cs`

- [ ] **Step 1: Update failing service tests first**

In `tests/TasteBudz.Web.Mvc.IntegrationTests/Services/GroupApiServiceTests.cs`, update every `new GroupSummaryDto(...)` call from:

```csharp
new GroupSummaryDto(groupId, "Foodies", "Dinner club", GroupVisibility.Public, 8)
```

to:

```csharp
new GroupSummaryDto(groupId, "Foodies", "Dinner club", GroupVisibility.Public, GroupWallpaperTheme.TacoTableRealistic, 8)
```

In `SerializesGroupMutationRequests`, add the create cover theme:

```csharp
await service.CreateAsync(new CreateGroupRequest
{
    Name = "Foodies",
    Description = "Dinner club",
    Visibility = GroupVisibility.Public,
    WallpaperTheme = GroupWallpaperTheme.TacoTableRealistic,
});
```

Add this assertion after the create request name assertion:

```csharp
Assert.Contains(
    "\"wallpaperTheme\":\"TacoTableRealistic\"",
    context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/groups").Body);
```

- [ ] **Step 2: Run MVC service tests to verify compile failures**

Run:

```powershell
dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~GroupApiServiceTests"
```

Expected: FAIL until the MVC view models and DTO constructor usages are updated.

- [ ] **Step 3: Add cover helpers to group view models**

In `src/TasteBudz.Web.Mvc/ViewModels/GroupViewModels.cs`, set `WallpaperTheme` from DTOs:

```csharp
public static GroupSummaryItem FromDto(GroupSummaryDto dto) => new()
{
    GroupId = dto.GroupId,
    Name = dto.Name,
    Description = dto.Description,
    Visibility = dto.Visibility.ToString(),
    WallpaperTheme = dto.WallpaperTheme,
    ActiveMembers = dto.ActiveMembers,
    IsPublic = dto.Visibility == GroupVisibility.Public,
};
```

Add these properties to `GroupSummaryItem`:

```csharp
public string CoverCssClass => GroupCoverThemeFormatting.ToCssClass(WallpaperTheme);
public string CoverLabel => GroupCoverThemeFormatting.GetLabel(WallpaperTheme);
```

Add `WallpaperTheme` and cover helpers to `MyGroupSummaryItem`:

```csharp
public GroupWallpaperTheme WallpaperTheme { get; init; }
public string CoverCssClass => GroupCoverThemeFormatting.ToCssClass(WallpaperTheme);
public string CoverLabel => GroupCoverThemeFormatting.GetLabel(WallpaperTheme);
```

Update `MyGroupSummaryItem.FromDto`:

```csharp
public static MyGroupSummaryItem FromDto(DashboardGroupSummaryDto dto) => new()
{
    GroupId = dto.GroupId,
    Name = dto.Name,
    Description = dto.Description,
    Visibility = dto.Visibility,
    WallpaperTheme = dto.WallpaperTheme,
    ActiveMembers = dto.ActiveMemberCount,
};
```

Add `WallpaperTheme`, options, and CSS helpers to `GroupCreateViewModel`:

```csharp
[Display(Name = "Group Background")]
public GroupWallpaperTheme? WallpaperTheme { get; set; } = GroupWallpaperTheme.Default;

public IReadOnlyList<GroupWallpaperOption> WallpaperOptions => GroupWallpaperOptions.All;

public string PreviewWallpaperCssClass => GroupCoverThemeFormatting.ToCssClass(WallpaperTheme ?? GroupWallpaperTheme.Default);
```

Update `GroupCreateViewModel.ToRequest()`:

```csharp
public CreateGroupRequest ToRequest() => new()
{
    Name = Name,
    Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
    Visibility = Visibility,
    WallpaperTheme = WallpaperTheme ?? GroupWallpaperTheme.Default,
};
```

Update `GroupManageViewModel.WallpaperCssClass`:

```csharp
public string WallpaperCssClass => GroupCoverThemeFormatting.ToCssClass(WallpaperTheme);
```

Replace `GroupWallpaperOptions.All` with this list:

```csharp
public static IReadOnlyList<GroupWallpaperOption> All { get; } =
[
    new(GroupWallpaperTheme.Default, "TasteBudz Warm", "Soft neutral cards with a warm table glow."),
    new(GroupWallpaperTheme.PizzaNightRealistic, "Pizza Night Photo", "Real pizza table photography."),
    new(GroupWallpaperTheme.SushiBarRealistic, "Sushi Bar Photo", "Real sushi counter photography."),
    new(GroupWallpaperTheme.TacoTableRealistic, "Taco Table Photo", "Real taco table photography."),
    new(GroupWallpaperTheme.CoffeeBrunchRealistic, "Coffee Brunch Photo", "Real brunch and cafe photography."),
    new(GroupWallpaperTheme.GardenFreshRealistic, "Garden Fresh Photo", "Real vegetarian table photography."),
    new(GroupWallpaperTheme.CurryNightRealistic, "Curry Night Photo", "Real curry and shared plates."),
    new(GroupWallpaperTheme.NoodleHouseRealistic, "Noodle House Photo", "Real noodle bowl photography."),
    new(GroupWallpaperTheme.PizzaNightIllustrated, "Pizza Night Illustration", "Stylized pizza cover art."),
    new(GroupWallpaperTheme.SushiBarIllustrated, "Sushi Bar Illustration", "Stylized sushi cover art."),
    new(GroupWallpaperTheme.TacoTableIllustrated, "Taco Table Illustration", "Stylized taco cover art."),
    new(GroupWallpaperTheme.CoffeeBrunchIllustrated, "Coffee Brunch Illustration", "Stylized brunch cover art."),
    new(GroupWallpaperTheme.GardenFreshIllustrated, "Garden Fresh Illustration", "Stylized vegetarian cover art."),
    new(GroupWallpaperTheme.CurryNightIllustrated, "Curry Night Illustration", "Stylized curry cover art."),
    new(GroupWallpaperTheme.NoodleHouseIllustrated, "Noodle House Illustration", "Stylized noodle cover art."),
];
```

Add this file-scoped helper below `GroupWallpaperOptions`:

```csharp
file static class GroupCoverThemeFormatting
{
    public static string ToCssClass(GroupWallpaperTheme theme) =>
        $"group-cover--{theme.ToString().ToLowerInvariant()}";

    public static string GetLabel(GroupWallpaperTheme theme) =>
        GroupWallpaperOptions.All.FirstOrDefault(option => option.Value == theme)?.Label ?? "TasteBudz Warm";
}
```

- [ ] **Step 4: Run MVC service tests**

Run:

```powershell
dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~GroupApiServiceTests"
```

Expected: PASS.

- [ ] **Step 5: Commit MVC model slice**

Run:

```powershell
git add src/TasteBudz.Web.Mvc/ViewModels/GroupViewModels.cs tests/TasteBudz.Web.Mvc.IntegrationTests/Services/GroupApiServiceTests.cs
git commit -m "Surface group cover themes in MVC models"
```

---

### Task 3: Group Create and Browse UI

**Files:**
- Modify: `src/TasteBudz.Web.Mvc/Views/Group/CreateGroup.cshtml`
- Modify: `src/TasteBudz.Web.Mvc/Views/Group/Index.cshtml`
- Modify: `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs`

- [ ] **Step 1: Add failing MVC HTML assertions**

In `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs`, add a test near the other group page tests:

```csharp
[Fact]
public async Task CreateGroup_RendersBuiltInCoverPicker()
{
    using var factory = new TasteBudzMvcFactory();
    using var client = MvcTestHelpers.CreateClient(factory);

    await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
    factory.BackendHandler.Requests.Clear();

    using var response = await client.GetAsync("/Group/CreateGroup");
    var html = await response.Content.ReadAsStringAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("name=\"WallpaperTheme\"", html);
    Assert.Contains("Pizza Night Photo", html);
    Assert.Contains("Sushi Bar Illustration", html);
    Assert.Contains("data-group-cover-preview", html);
}
```

Add a browse-card assertion in the existing index/browse test if present, or create:

```csharp
[Fact]
public async Task Index_RendersGroupCoverClasses()
{
    using var factory = new TasteBudzMvcFactory();
    using var client = MvcTestHelpers.CreateClient(factory);
    var groupId = Guid.NewGuid();

    await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
    factory.BackendHandler.Requests.Clear();

    factory.BackendHandler.Enqueue(
        HttpMethod.Get,
        "/api/v1/me/groups",
        (_, _) => StubBackendApiHandler.Json(
            HttpStatusCode.OK,
            Array.Empty<DashboardGroupSummaryDto>()));
    factory.BackendHandler.Enqueue(
        HttpMethod.Get,
        "/api/v1/groups?page=1&pageSize=20",
        (_, _) => StubBackendApiHandler.Json(
            HttpStatusCode.OK,
            new ListResponse<GroupSummaryDto>(
                new[]
                {
                    new GroupSummaryDto(groupId, "Taco Budz", "Weekend tacos", GroupVisibility.Public, GroupWallpaperTheme.TacoTableRealistic, 8),
                },
                1)));

    using var response = await client.GetAsync("/Group/Index");
    var html = await response.Content.ReadAsStringAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("group-cover--tacotablerealistic", html);
    Assert.Contains("Taco Table Photo", html);
    factory.BackendHandler.AssertDrained();
}
```

- [ ] **Step 2: Run MVC group tests to verify failures**

Run:

```powershell
dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~GroupMvcTests"
```

Expected: FAIL because the create cover picker and browse cover class markup are not present yet.

- [ ] **Step 3: Add cover picker to create form**

In `src/TasteBudz.Web.Mvc/Views/Group/CreateGroup.cshtml`, add this block after the privacy picker:

```cshtml
<div class="stack">
    <div class="group-section__header group-section__header--compact">
        <div>
            <p class="group-section__eyebrow">Background</p>
            <h2>Choose a Cover</h2>
        </div>
    </div>

    <div class="group-cover-picker" role="radiogroup" aria-label="Group background">
        @foreach (var option in Model.WallpaperOptions)
        {
            var inputId = $"WallpaperTheme_{option.Value}";
            <label class="group-cover-option @GroupCoverThemeClass(option.Value)">
                @Html.RadioButtonFor(
                    m => m.WallpaperTheme,
                    option.Value,
                    new
                    {
                        id = inputId,
                        @class = "group-cover-option__input",
                        data_group_cover_theme = GroupCoverThemeClass(option.Value),
                        data_group_cover_label = option.Label
                    })
                <span class="group-cover-option__body">
                    <span class="group-cover-option__swatch"></span>
                    <span class="group-cover-option__copy">
                        <span class="group-cover-option__headline">@option.Label</span>
                        <span class="group-cover-option__description">@option.Description</span>
                    </span>
                </span>
            </label>
        }
    </div>
</div>
```

Add this Razor helper near the top of the file after local variables:

```cshtml
@functions {
    private static string GroupCoverThemeClass(GroupWallpaperTheme theme) =>
        $"group-cover--{theme.ToString().ToLowerInvariant()}";
}
```

Update the preview card class:

```cshtml
<article class="group-showcase-card group-showcase-card--preview @Model.PreviewWallpaperCssClass" data-group-cover-preview>
```

Add this preview label inside `.group-showcase-card__meta`:

```cshtml
<span id="GroupPreviewCoverLabel">@Model.WallpaperOptions.First(option => option.Value == (Model.WallpaperTheme ?? GroupWallpaperTheme.Default)).Label</span>
```

- [ ] **Step 4: Update create page preview JavaScript**

In the existing script in `CreateGroup.cshtml`, add:

```javascript
var coverInputs = Array.prototype.slice.call(document.querySelectorAll('input[name="WallpaperTheme"]'));
var previewCard = document.querySelector('[data-group-cover-preview]');
var previewCoverLabel = document.getElementById('GroupPreviewCoverLabel');
```

Add inside `updatePreview()`:

```javascript
var selectedCover = coverInputs.find(function (input) { return input.checked; });
if (selectedCover && previewCard) {
    coverInputs.forEach(function (input) {
        if (input.dataset.groupCoverTheme) {
            previewCard.classList.remove(input.dataset.groupCoverTheme);
        }
    });
    previewCard.classList.add(selectedCover.dataset.groupCoverTheme);
    if (previewCoverLabel) {
        previewCoverLabel.textContent = selectedCover.dataset.groupCoverLabel || 'TasteBudz Warm';
    }
}
```

Update the event listener array:

```javascript
[nameInput, descriptionInput, publicOption, privateOption].concat(coverInputs).forEach(function (element) {
```

- [ ] **Step 5: Render cover classes on group index cards**

In `src/TasteBudz.Web.Mvc/Views/Group/Index.cshtml`, change my-groups article class:

```cshtml
<article class="summary-card interest-card group-my-card group-showcase-card @group.CoverCssClass">
```

Inside that card's chip list add:

```cshtml
<span class="interest-card__chip interest-card__chip--muted">@group.CoverLabel</span>
```

Change public card article class:

```cshtml
<article class="summary-card interest-card group-public-card group-showcase-card @group.CoverCssClass">
```

Inside public card chips add:

```cshtml
<span class="interest-card__chip interest-card__chip--muted">@group.CoverLabel</span>
```

- [ ] **Step 6: Run MVC group tests**

Run:

```powershell
dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~GroupMvcTests"
```

Expected: PASS.

- [ ] **Step 7: Commit create/browse UI slice**

Run:

```powershell
git add src/TasteBudz.Web.Mvc/Views/Group/CreateGroup.cshtml src/TasteBudz.Web.Mvc/Views/Group/Index.cshtml tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs
git commit -m "Add group cover picker to MVC UI"
```

---

### Task 4: B+C Visual Polish CSS

**Files:**
- Create: `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/pizza-night-realistic.png`
- Create: `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/sushi-bar-realistic.png`
- Create: `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/taco-table-realistic.png`
- Create: `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/coffee-brunch-realistic.png`
- Create: `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/garden-fresh-realistic.png`
- Create: `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/curry-night-realistic.png`
- Create: `src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/noodle-house-realistic.png`
- Modify: `src/TasteBudz.Web.Mvc/wwwroot/css/site.css`
- Modify: `src/TasteBudz.Web.Mvc/Views/Group/Manage.cshtml`
- Modify: `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs`

- [ ] **Step 1: Add a focused CSS smoke assertion**

Add this test to `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs`:

```csharp
[Fact]
public async Task Manage_UsesSelectedCoverOnGroupHub()
{
    using var factory = new TasteBudzMvcFactory();
    using var client = MvcTestHelpers.CreateClient(factory);
    var groupId = Guid.NewGuid();
    var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
    factory.BackendHandler.Requests.Clear();

    EnqueueGroupDetail(factory, groupId, session.CurrentUser.UserId, GroupVisibility.Public, GroupWallpaperTheme.SushiBarRealistic);
    factory.BackendHandler.Enqueue(
        HttpMethod.Get,
        $"/api/v1/groups/{groupId}/events?page=1&pageSize=50",
        (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new ListResponse<EventSummaryDto>(Array.Empty<EventSummaryDto>(), 0)));
    factory.BackendHandler.Enqueue(
        HttpMethod.Get,
        $"/api/v1/groups/{groupId}/announcements",
        (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new ListResponse<GroupAnnouncementDto>(Array.Empty<GroupAnnouncementDto>(), 0)));

    using var response = await client.GetAsync($"/Group/Manage?groupId={groupId}");
    var html = await response.Content.ReadAsStringAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("group-cover--sushibarrealistic", html);
    Assert.Contains("data-group-cover-shell", html);
    factory.BackendHandler.AssertDrained();
}
```

If `EnqueueGroupDetail` only accepts three parameters, update it to accept an optional theme:

```csharp
private static void EnqueueGroupDetail(
    TasteBudzMvcFactory factory,
    Guid groupId,
    Guid ownerUserId,
    GroupVisibility visibility,
    GroupWallpaperTheme wallpaperTheme = GroupWallpaperTheme.Default)
```

and pass `wallpaperTheme` into `new GroupDetailDto(...)`.

- [ ] **Step 2: Run the smoke test to verify failure**

Run:

```powershell
dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~Manage_UsesSelectedCoverOnGroupHub"
```

Expected: FAIL because `data-group-cover-shell` is not present yet.

- [ ] **Step 3: Generate and save local realistic cover assets**

Use the imagegen skill in built-in mode. Generate one 16:9 landscape asset per prompt, inspect it, and copy the selected output into the exact path shown in the filename line. All images must avoid text, logos, watermarks, visible brand marks, extreme bokeh, dark blur, and AI-looking food distortions.

```text
Filename: src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/pizza-night-realistic.png
Use case: photorealistic-natural
Asset type: TasteBudz group cover background
Primary request: realistic editorial photo of a shared pizza night table
Scene/backdrop: cozy casual restaurant table with two pizzas, basil, tomato sauce, small plates, and hands partially visible at the edge
Style/medium: photorealistic food and social dining photography
Composition/framing: wide 16:9 landscape, top-down three-quarter angle, clear central food subject, safe darker space along bottom for overlaid card text
Lighting/mood: warm natural evening light, polished but not glossy
Constraints: no text, no logos, no watermark, no distorted hands, no brand packaging
Avoid: generic stock-photo blur, oversaturated orange, messy clutter
```

```text
Filename: src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/sushi-bar-realistic.png
Use case: photorealistic-natural
Asset type: TasteBudz group cover background
Primary request: realistic editorial photo of a sushi bar shared plate
Scene/backdrop: clean sushi counter with nigiri, maki, soy dish, chopsticks, and subtle green garnish
Style/medium: photorealistic food photography
Composition/framing: wide 16:9 landscape, low three-quarter angle, food in the middle third, calm negative space near bottom
Lighting/mood: bright natural counter light, refined and fresh
Constraints: no text, no logos, no watermark, no distorted chopsticks or food
Avoid: neon restaurant lighting, heavy blur, cluttered plates
```

```text
Filename: src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/taco-table-realistic.png
Use case: photorealistic-natural
Asset type: TasteBudz group cover background
Primary request: realistic editorial photo of a shared taco table
Scene/backdrop: tacos on corn tortillas with lime, cilantro, salsa bowls, and casual shared plates
Style/medium: photorealistic food and social dining photography
Composition/framing: wide 16:9 landscape, overhead angle, colorful food across the top and center, readable lower overlay area
Lighting/mood: daylight, fresh, casual, social
Constraints: no text, no logos, no watermark, no distorted hands
Avoid: greasy fast-food look, excessive orange cast, messy table
```

```text
Filename: src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/coffee-brunch-realistic.png
Use case: photorealistic-natural
Asset type: TasteBudz group cover background
Primary request: realistic editorial photo of coffee brunch with pastries and shared plates
Scene/backdrop: cafe table with cappuccino cups, croissants, fruit, and brunch plates
Style/medium: photorealistic lifestyle food photography
Composition/framing: wide 16:9 landscape, gentle overhead angle, subject fills upper two thirds, lower area stays calm for text overlay
Lighting/mood: soft morning window light, relaxed and premium
Constraints: no text, no logos, no watermark, no distorted cups or utensils
Avoid: beige-only palette, harsh shadows, fake foam art
```

```text
Filename: src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/garden-fresh-realistic.png
Use case: photorealistic-natural
Asset type: TasteBudz group cover background
Primary request: realistic editorial photo of a fresh vegetarian shared table
Scene/backdrop: colorful seasonal vegetables, salad bowls, herbs, grains, and shared plates
Style/medium: photorealistic food photography
Composition/framing: wide 16:9 landscape, overhead angle, greens balanced with red and yellow produce, readable lower overlay area
Lighting/mood: bright natural market-table light, fresh and inviting
Constraints: no text, no logos, no watermark
Avoid: one-note green palette, plastic-looking vegetables, generic stock blur
```

```text
Filename: src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/curry-night-realistic.png
Use case: photorealistic-natural
Asset type: TasteBudz group cover background
Primary request: realistic editorial photo of curry night shared dishes
Scene/backdrop: bowls of curry, rice, naan, chutney, and small shared plates on a restaurant table
Style/medium: photorealistic food photography
Composition/framing: wide 16:9 landscape, three-quarter overhead angle, rich food detail in upper and center area, lower area usable for overlay
Lighting/mood: warm restaurant light with natural highlights
Constraints: no text, no logos, no watermark, no distorted bowls
Avoid: muddy brown palette, excessive steam, clutter
```

```text
Filename: src/TasteBudz.Web.Mvc/wwwroot/img/group-covers/noodle-house-realistic.png
Use case: photorealistic-natural
Asset type: TasteBudz group cover background
Primary request: realistic editorial photo of noodle bowls shared at a table
Scene/backdrop: ramen or noodle bowls, chopsticks, broth, greens, and shared appetizers
Style/medium: photorealistic food photography
Composition/framing: wide 16:9 landscape, low three-quarter angle, bowls in upper two thirds, stable lower overlay area
Lighting/mood: warm but clean restaurant light, social and appetizing
Constraints: no text, no logos, no watermark, no distorted noodles or chopsticks
Avoid: dark moody blur, oversaturated red, messy table
```

- [ ] **Step 4: Add a stable hook to the group hub**

In `src/TasteBudz.Web.Mvc/Views/Group/Manage.cshtml`, update the root section:

```cshtml
<section class="profile-shell group-page group-page--manage @Model.WallpaperCssClass" data-group-cover-shell>
```

Update the theme stat label to use a friendly label:

```cshtml
<strong>@Model.WallpaperOptions.First(option => option.Value == Model.WallpaperTheme).Label</strong>
```

- [ ] **Step 5: Add B+C cover CSS**

Append this block near the existing group CSS in `src/TasteBudz.Web.Mvc/wwwroot/css/site.css`:

```css
.group-cover--default {
    --group-cover-image:
        radial-gradient(circle at 14% 12%, rgba(255, 229, 212, 0.96), transparent 30%),
        linear-gradient(135deg, rgba(255, 249, 245, 0.96), rgba(255, 241, 230, 0.78));
    --group-cover-ink: #31211b;
}

.group-cover--pizzanightrealistic,
.group-cover--pizzanight {
    --group-cover-image:
        linear-gradient(180deg, rgba(49, 33, 27, 0.1), rgba(49, 33, 27, 0.72)),
        url("/img/group-covers/pizza-night-realistic.png");
}

.group-cover--sushibarrealistic,
.group-cover--sushibar {
    --group-cover-image:
        linear-gradient(180deg, rgba(25, 45, 42, 0.08), rgba(25, 45, 42, 0.72)),
        url("/img/group-covers/sushi-bar-realistic.png");
}

.group-cover--tacotablerealistic,
.group-cover--tacotable {
    --group-cover-image:
        linear-gradient(180deg, rgba(49, 33, 27, 0.08), rgba(49, 33, 27, 0.74)),
        url("/img/group-covers/taco-table-realistic.png");
}

.group-cover--coffeebrunchrealistic,
.group-cover--coffeebrunch {
    --group-cover-image:
        linear-gradient(180deg, rgba(66, 42, 30, 0.08), rgba(66, 42, 30, 0.74)),
        url("/img/group-covers/coffee-brunch-realistic.png");
}

.group-cover--gardenfreshrealistic,
.group-cover--gardenfresh {
    --group-cover-image:
        linear-gradient(180deg, rgba(31, 65, 43, 0.08), rgba(31, 65, 43, 0.74)),
        url("/img/group-covers/garden-fresh-realistic.png");
}

.group-cover--currynightrealistic {
    --group-cover-image:
        linear-gradient(180deg, rgba(61, 38, 17, 0.08), rgba(61, 38, 17, 0.76)),
        url("/img/group-covers/curry-night-realistic.png");
}

.group-cover--noodlehouserealistic {
    --group-cover-image:
        linear-gradient(180deg, rgba(43, 29, 22, 0.08), rgba(43, 29, 22, 0.76)),
        url("/img/group-covers/noodle-house-realistic.png");
}

.group-cover--pizzanightillustrated,
.group-cover--sushibarillustrated,
.group-cover--tacotableillustrated,
.group-cover--coffeebrunchillustrated,
.group-cover--gardenfreshillustrated,
.group-cover--currynightillustrated,
.group-cover--noodlehouseillustrated {
    --group-cover-image:
        radial-gradient(circle at 18% 16%, rgba(255, 255, 255, 0.5), transparent 18%),
        repeating-linear-gradient(135deg, rgba(255, 255, 255, 0.16) 0 2px, transparent 2px 14px),
        linear-gradient(135deg, #ef6b3f, #5e8061);
}

.group-showcase-card.group-cover--default::before,
.group-showcase-card[class*="group-cover--"]::before {
    height: 9.25rem;
    background: var(--group-cover-image);
    background-size: cover;
    background-position: center;
}

.group-page--manage[class*="group-cover--"] {
    background:
        linear-gradient(180deg, rgba(255, 250, 246, 0.88), rgba(255, 246, 238, 0.96)),
        var(--group-cover-image);
    background-size: cover;
    background-position: center top;
}

.group-manage-hero[class],
.group-page--manage .group-manage-hero {
    background: rgba(255, 255, 255, 0.9);
    backdrop-filter: blur(14px);
}

.group-cover-picker {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
    gap: 0.75rem;
}

.group-cover-option {
    display: block;
    cursor: pointer;
}

.group-cover-option__input {
    position: absolute;
    opacity: 0;
    pointer-events: none;
}

.group-cover-option__body {
    display: grid;
    grid-template-columns: 4.2rem minmax(0, 1fr);
    gap: 0.75rem;
    align-items: center;
    min-height: 5.4rem;
    padding: 0.75rem;
    border-radius: 1.05rem;
    background: rgba(255, 255, 255, 0.84);
    border: 1px solid rgba(193, 126, 81, 0.14);
    transition: border-color 160ms ease, box-shadow 160ms ease, transform 160ms ease;
}

.group-cover-option__swatch {
    height: 3.8rem;
    border-radius: 0.9rem;
    background: var(--group-cover-image);
    background-size: cover;
    background-position: center;
}

.group-cover-option__headline {
    display: block;
    color: var(--ink);
    font-weight: 800;
}

.group-cover-option__description {
    display: block;
    margin-top: 0.2rem;
    color: var(--muted);
    font-size: 0.86rem;
    line-height: 1.35;
}

.group-cover-option__input:checked + .group-cover-option__body {
    border-color: rgba(239, 107, 63, 0.42);
    box-shadow: 0 0 0 4px rgba(239, 107, 63, 0.12);
}
```

- [ ] **Step 6: Add mobile CSS constraints**

Append inside the existing `@media (max-width: 600px)` block:

```css
.group-cover-picker {
    grid-template-columns: 1fr;
}

.group-cover-option__body {
    grid-template-columns: 3.6rem minmax(0, 1fr);
}

.group-cover-option__swatch {
    height: 3.2rem;
}
```

- [ ] **Step 7: Run the MVC smoke test**

Run:

```powershell
dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~Manage_UsesSelectedCoverOnGroupHub"
```

Expected: PASS.

- [ ] **Step 8: Commit visual CSS slice**

Run:

```powershell
git add src/TasteBudz.Web.Mvc/wwwroot/img/group-covers src/TasteBudz.Web.Mvc/wwwroot/css/site.css src/TasteBudz.Web.Mvc/Views/Group/Manage.cshtml tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs
git commit -m "Polish group cover visuals"
```

---

### Task 5: Documentation and Full Verification

**Files:**
- Modify: `docs/TasteBudz_Functional_Requirements.md`
- Modify: `docs/backend/domain-model.md`
- Modify: `docs/backend/api-endpoints.md`
- Modify: `docs/backend/testing-strategy.md`

- [ ] **Step 1: Update functional requirements**

In `docs/TasteBudz_Functional_Requirements.md`, under group behavior, add:

```markdown
- Group owners can choose one built-in group background preset when creating or editing a group. Presets are visual metadata only and do not affect group visibility, membership, invites, events, chat, or moderation behavior.
```

- [ ] **Step 2: Update domain model**

In `docs/backend/domain-model.md`, under the Groups section or canonical group decisions, add:

```markdown
### Group cover theme is presentation metadata

`Group.WallpaperTheme` stores the selected built-in cover preset for the group page and group cards.

Rules:

- only the current group owner can change the selected preset
- the preset value is constrained to supported enum values
- cover selection does not affect group visibility, membership, event linkage, chat access, or moderation rules
- custom uploads and admin-managed cover libraries are outside the current MVP slice
```

- [ ] **Step 3: Update API docs**

In `docs/backend/api-endpoints.md`, under group DTO/contract notes, add:

```markdown
Group create/update contracts may include `wallpaperTheme`, a constrained built-in preset value such as `Default`, `TacoTableRealistic`, or `SushiBarIllustrated`. `GroupSummaryDto`, `DashboardGroupSummaryDto`, and `GroupDetailDto` include the selected preset when the caller can view the group.
```

- [ ] **Step 4: Update testing strategy**

In `docs/backend/testing-strategy.md`, under P1 Groups and messaging access, add:

```markdown
- group cover selection is owner-only, uses constrained preset values, and remains presentation metadata only
- group browse, dashboard group summaries, and group detail views preserve the selected cover preset
```

- [ ] **Step 5: Run focused automated tests**

Run:

```powershell
dotnet test tests/TasteBudz.Backend.UnitTests/TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~GroupServiceTests"
dotnet test tests/TasteBudz.Web.Mvc.IntegrationTests/TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~GroupApiServiceTests|FullyQualifiedName~GroupMvcTests"
```

Expected: PASS.

- [ ] **Step 6: Run build**

Run:

```powershell
dotnet build TasteBudz.sln --configuration Debug
```

Expected: PASS with no new errors.

- [ ] **Step 7: Start local app**

Run:

```powershell
.\start-dev.ps1
```

Expected: the MVC app and backend start. Note the MVC URL printed by the script.

- [ ] **Step 8: Verify visual appearance in browser**

Open the MVC app in the in-app browser. Verify these pages:

```text
/Group/Index
/Group/CreateGroup
/Group/Manage?groupId=<seed-or-created-group-id>
```

Minimum visual checks:

- desktop width around 1365px
- mobile width around 390px
- group cover picker has no clipping or horizontal scroll
- group cards show readable cover overlays
- group hub text remains readable over selected cover backgrounds
- floating support/chat buttons do not cover group actions on mobile
- buttons and chips do not resize cards unexpectedly on hover
- the page does not look like a one-note orange/cream palette

- [ ] **Step 9: Capture evidence**

Save screenshots with descriptive names at repo root or `output/ui-verification/`:

```text
tastebudz-groups-bc-index-desktop.png
tastebudz-groups-bc-index-mobile.png
tastebudz-group-create-cover-picker-desktop.png
tastebudz-group-create-cover-picker-mobile.png
tastebudz-group-manage-cover-desktop.png
tastebudz-group-manage-cover-mobile.png
```

- [ ] **Step 10: Commit documentation and verification-ready changes**

Run:

```powershell
git add docs/TasteBudz_Functional_Requirements.md docs/backend/domain-model.md docs/backend/api-endpoints.md docs/backend/testing-strategy.md
git commit -m "Document group cover preset behavior"
```

If screenshots are intentionally tracked for this repository, add and commit them separately:

```powershell
git add tastebudz-groups-bc-index-desktop.png tastebudz-groups-bc-index-mobile.png tastebudz-group-create-cover-picker-desktop.png tastebudz-group-create-cover-picker-mobile.png tastebudz-group-manage-cover-desktop.png tastebudz-group-manage-cover-mobile.png
git commit -m "Add group cover visual verification screenshots"
```

If screenshots are not tracked, leave them uncommitted and summarize their paths in the final handoff.

---

## Self-Review

Spec coverage:

- B+C visual direction: covered by Tasks 3 and 4.
- Realistic general/group imagery: covered by Task 4 realistic cover classes.
- Both realistic and illustrated group preset styles: covered by Tasks 1, 2, and 4.
- Group owner chooses preset: covered by Tasks 1, 2, and 3.
- No admin management: preserved by no admin files or endpoints in the plan.
- Visual appearance check: covered by Task 5 browser verification and screenshot evidence.

No placeholders:

- The plan contains concrete file paths, code snippets, commands, and expected outcomes.
- No task requires custom uploads, admin tooling, or product image generation inside the running app.

Type consistency:

- `GroupWallpaperTheme`, `WallpaperTheme`, `GroupSummaryDto.WallpaperTheme`, `DashboardGroupSummaryDto.WallpaperTheme`, `GroupCoverThemeFormatting.ToCssClass`, and CSS class names all use the same enum-backed preset model.
