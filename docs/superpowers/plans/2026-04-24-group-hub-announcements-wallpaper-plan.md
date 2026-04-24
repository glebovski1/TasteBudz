# Group Hub Announcements and Food Wallpaper Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add owner-authored group announcements, system event announcements, and owner-selected food-themed wallpaper presets while preserving the current TasteBudz UI style.

**Architecture:** Keep the feature inside the modular monolith. Groups owns announcement and wallpaper policy; Events calls a narrow Groups service method after successful group-linked event creation; MVC consumes explicit DTOs and renders the group hub responsively.

**Tech Stack:** ASP.NET Core/.NET 9, C# records and services, EF Core-backed repositories, source-controlled SQLite and SQL Server scripts, Razor MVC, xUnit integration/unit tests, Playwright visual verification.

---

## File Structure

- Modify `src/TasteBudz.Backend/Domain/CommonEnums.cs` to add `GroupWallpaperTheme` and `GroupAnnouncementKind`.
- Modify `src/TasteBudz.Backend/Domain/GroupModels.cs` to add `WallpaperTheme` to `Group` and add `GroupAnnouncement`.
- Modify `src/TasteBudz.Backend/Modules/Groups/DTOs/GroupDetailDto.cs` to expose `WallpaperTheme`.
- Modify `src/TasteBudz.Backend/Modules/Groups/DTOs/UpdateGroupRequest.cs` to accept `WallpaperTheme`.
- Create `src/TasteBudz.Backend/Modules/Groups/DTOs/CreateGroupAnnouncementRequest.cs`.
- Create `src/TasteBudz.Backend/Modules/Groups/DTOs/GroupAnnouncementDto.cs`.
- Modify `src/TasteBudz.Backend/Modules/Groups/IGroupRepository.cs` to add announcement persistence methods.
- Modify `src/TasteBudz.Backend/Infrastructure/Persistence/InMemory/InMemoryTasteBudzStore.cs` to store announcements.
- Modify `src/TasteBudz.Backend/Modules/Groups/InMemoryGroupRepository.cs` to support announcements.
- Modify `src/TasteBudz.Backend/Infrastructure/Persistence/Sqlite/SqliteEntities.cs` and `TasteBudzDbContext.cs` to map `GroupAnnouncement` and `WallpaperTheme`.
- Modify `src/TasteBudz.Backend/Modules/Groups/SqliteGroupRepository.cs` to persist and query announcements.
- Modify `src/TasteBudz.Database/sqlite/dbTasteBudz.sqlite.sql` and `src/TasteBudz.Database/sqlserver/010_schema.sql` to add `WallpaperTheme` and `GroupAnnouncements`.
- Modify `src/TasteBudz.Backend/Modules/Groups/GroupService.cs` to validate themes, list/create manual announcements, and create system event announcements.
- Modify `src/TasteBudz.Backend/Controllers/GroupsController.cs` to expose announcement endpoints.
- Modify `src/TasteBudz.Backend/Modules/Events/EventService.cs` and DI setup if needed to call the Groups service after group-linked event creation.
- Modify `src/TasteBudz.Web.Mvc/Services/BackendApi/GroupApiService.cs` to call announcement endpoints.
- Modify `src/TasteBudz.Web.Mvc/ViewModels/GroupViewModels.cs` to add wallpaper and announcement view models.
- Modify `src/TasteBudz.Web.Mvc/Controllers/GroupController.cs` to load announcements and post owner announcements.
- Modify `src/TasteBudz.Web.Mvc/Views/Group/Manage.cshtml` and `src/TasteBudz.Web.Mvc/wwwroot/css/site.css` for the app-style group hub.
- Modify docs listed in the design spec.
- Add/update unit, API integration, MVC service, and MVC page tests.

---

### Task 1: Add Group Announcement and Wallpaper Contracts

**Files:**
- Modify: `src/TasteBudz.Backend/Domain/CommonEnums.cs`
- Modify: `src/TasteBudz.Backend/Domain/GroupModels.cs`
- Modify: `src/TasteBudz.Backend/Modules/Groups/DTOs/GroupDetailDto.cs`
- Modify: `src/TasteBudz.Backend/Modules/Groups/DTOs/UpdateGroupRequest.cs`
- Create: `src/TasteBudz.Backend/Modules/Groups/DTOs/CreateGroupAnnouncementRequest.cs`
- Create: `src/TasteBudz.Backend/Modules/Groups/DTOs/GroupAnnouncementDto.cs`
- Test: `tests/TasteBudz.Backend.UnitTests/Groups/GroupServiceTests.cs`

- [ ] **Step 1: Write failing unit tests for wallpaper default and invalid update**

Add these tests to `GroupServiceTests`:

```csharp
[Fact]
public async Task CreateAsync_DefaultsWallpaperTheme()
{
    var clock = new TestClock(new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");

    var detail = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
    {
        Name = "Wallpaper Crew",
        Visibility = GroupVisibility.Public,
    });

    Assert.Equal(GroupWallpaperTheme.Default, detail.WallpaperTheme);
}

[Fact]
public async Task UpdateAsync_OwnerCanChangeWallpaperTheme()
{
    var clock = new TestClock(new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
    var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
    {
        Name = "Theme Crew",
        Visibility = GroupVisibility.Public,
    });

    var updated = await services.GroupService.UpdateAsync(ToCurrentUser(owner), group.GroupId, new UpdateGroupRequest
    {
        WallpaperTheme = GroupWallpaperTheme.Sushi,
    });

    Assert.Equal(GroupWallpaperTheme.Sushi, updated.WallpaperTheme);
}
```

- [ ] **Step 2: Run unit tests and verify they fail**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.UnitTests\TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~GroupServiceTests"
```

Expected: compile failure because `GroupWallpaperTheme`, `WallpaperTheme`, or `UpdateGroupRequest.WallpaperTheme` does not exist.

- [ ] **Step 3: Add enum values**

In `CommonEnums.cs`, add:

```csharp
public enum GroupWallpaperTheme
{
    Default = 0,
    Sushi = 1,
    Tacos = 2,
    Brunch = 3,
    Ramen = 4,
    Pizza = 5,
    Dumplings = 6,
}

public enum GroupAnnouncementKind
{
    Manual = 0,
    System = 1,
}
```

- [ ] **Step 4: Update domain records**

Change `Group` in `GroupModels.cs` to include `GroupWallpaperTheme WallpaperTheme` before timestamps:

```csharp
public sealed record Group(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupLifecycleState LifecycleState,
    GroupWallpaperTheme WallpaperTheme,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
```

Add:

```csharp
public sealed record GroupAnnouncement(
    Guid Id,
    Guid GroupId,
    Guid? AuthorUserId,
    GroupAnnouncementKind Kind,
    string Title,
    string Body,
    DateTimeOffset CreatedAtUtc);
```

- [ ] **Step 5: Update DTOs and requests**

Change `GroupDetailDto` to include wallpaper theme:

```csharp
public sealed record GroupDetailDto(
    Guid GroupId,
    Guid OwnerUserId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupLifecycleState LifecycleState,
    GroupWallpaperTheme WallpaperTheme,
    bool IsCurrentUserMember,
    IReadOnlyCollection<GroupMemberDto> Members);
```

Add `WallpaperTheme` to `UpdateGroupRequest`:

```csharp
public sealed class UpdateGroupRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public GroupVisibility? Visibility { get; init; }
    public GroupWallpaperTheme? WallpaperTheme { get; init; }
}
```

Create `CreateGroupAnnouncementRequest.cs`:

```csharp
namespace TasteBudz.Backend.Modules.Groups;

public sealed class CreateGroupAnnouncementRequest
{
    public string? Title { get; init; }
    public string? Body { get; init; }
}
```

Create `GroupAnnouncementDto.cs`:

```csharp
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed record GroupAnnouncementDto(
    Guid AnnouncementId,
    Guid GroupId,
    Guid? AuthorUserId,
    string? AuthorUsername,
    string? AuthorDisplayName,
    GroupAnnouncementKind Kind,
    string Title,
    string Body,
    DateTimeOffset CreatedAtUtc);
```

- [ ] **Step 6: Update compile sites for new `Group` constructor**

Every `new Group(...)` call must pass `GroupWallpaperTheme.Default` unless a persisted value is mapped. Use this constructor order:

```csharp
new Group(groupId, ownerId, name, description, visibility, GroupLifecycleState.Active, GroupWallpaperTheme.Default, now, now)
```

- [ ] **Step 7: Run unit tests and verify contract task passes**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.UnitTests\TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~GroupServiceTests"
```

Expected: tests compile; wallpaper tests may still fail until `GroupService.UpdateAsync` maps the field.

- [ ] **Step 8: Implement wallpaper handling in `GroupService`**

In `CreateAsync`, construct groups with `GroupWallpaperTheme.Default`.

In `UpdateAsync`, add:

```csharp
WallpaperTheme = request.WallpaperTheme ?? group.WallpaperTheme,
```

- [ ] **Step 9: Run unit tests and commit task**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.UnitTests\TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~GroupServiceTests"
```

Expected: `GroupServiceTests` pass.

Commit:

```powershell
git add src\TasteBudz.Backend\Domain\CommonEnums.cs src\TasteBudz.Backend\Domain\GroupModels.cs src\TasteBudz.Backend\Modules\Groups\DTOs src\TasteBudz.Backend\Modules\Groups\GroupService.cs tests\TasteBudz.Backend.UnitTests\Groups\GroupServiceTests.cs
git commit -m "feat: add group wallpaper and announcement contracts"
```

---

### Task 2: Add Announcement Repository Support

**Files:**
- Modify: `src/TasteBudz.Backend/Modules/Groups/IGroupRepository.cs`
- Modify: `src/TasteBudz.Backend/Infrastructure/Persistence/InMemory/InMemoryTasteBudzStore.cs`
- Modify: `src/TasteBudz.Backend/Modules/Groups/InMemoryGroupRepository.cs`
- Test: `tests/TasteBudz.Backend.UnitTests/Groups/GroupServiceTests.cs`

- [ ] **Step 1: Write failing owner announcement test**

Add:

```csharp
[Fact]
public async Task CreateAnnouncementAsync_OwnerCreatesManualAnnouncement()
{
    var clock = new TestClock(new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
    var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
    {
        Name = "Announcement Crew",
        Visibility = GroupVisibility.Public,
    });

    var announcement = await services.GroupService.CreateAnnouncementAsync(ToCurrentUser(owner), group.GroupId, new CreateGroupAnnouncementRequest
    {
        Title = "Friday plan",
        Body = "Poll is open for Friday.",
    });
    var announcements = await services.GroupService.ListAnnouncementsAsync(owner.CurrentUser.UserId, group.GroupId);

    Assert.Equal(GroupAnnouncementKind.Manual, announcement.Kind);
    Assert.Equal(owner.CurrentUser.UserId, announcement.AuthorUserId);
    Assert.Equal("Friday plan", announcement.Title);
    Assert.Single(announcements.Items);
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.UnitTests\TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~CreateAnnouncementAsync_OwnerCreatesManualAnnouncement"
```

Expected: compile failure because `CreateAnnouncementAsync` and repository methods do not exist.

- [ ] **Step 3: Extend repository interface**

Add to `IGroupRepository`:

```csharp
Task<IReadOnlyCollection<GroupAnnouncement>> ListAnnouncementsAsync(Guid groupId, CancellationToken cancellationToken = default);

Task SaveAnnouncementAsync(GroupAnnouncement announcement, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Add in-memory storage**

In `InMemoryTasteBudzStore`, add:

```csharp
public List<GroupAnnouncement> GroupAnnouncements { get; } = [];
```

In the reset method, clear it:

```csharp
GroupAnnouncements.Clear();
```

- [ ] **Step 5: Implement in-memory repository methods**

In `InMemoryGroupRepository`, add:

```csharp
public Task<IReadOnlyCollection<GroupAnnouncement>> ListAnnouncementsAsync(Guid groupId, CancellationToken cancellationToken = default)
{
    IReadOnlyCollection<GroupAnnouncement> result = store.GroupAnnouncements
        .Where(announcement => announcement.GroupId == groupId)
        .OrderByDescending(announcement => announcement.CreatedAtUtc)
        .ToArray();

    return Task.FromResult(result);
}

public Task SaveAnnouncementAsync(GroupAnnouncement announcement, CancellationToken cancellationToken = default)
{
    var existingIndex = store.GroupAnnouncements.FindIndex(existing => existing.Id == announcement.Id);
    if (existingIndex >= 0)
    {
        store.GroupAnnouncements[existingIndex] = announcement;
    }
    else
    {
        store.GroupAnnouncements.Add(announcement);
    }

    return Task.CompletedTask;
}
```

- [ ] **Step 6: Add service methods**

In `GroupService`, add:

```csharp
public async Task<ListResponse<GroupAnnouncementDto>> ListAnnouncementsAsync(
    Guid currentUserId,
    Guid groupId,
    CancellationToken cancellationToken = default)
{
    var group = await GetActiveGroupAsync(groupId, cancellationToken);
    await EnsureCanViewAsync(currentUserId, group, cancellationToken);
    var announcements = await groupRepository.ListAnnouncementsAsync(groupId, cancellationToken);
    var mapped = await MapAnnouncementsAsync(announcements, cancellationToken);
    return new ListResponse<GroupAnnouncementDto>(mapped, mapped.Count);
}

public async Task<GroupAnnouncementDto> CreateAnnouncementAsync(
    CurrentUser currentUser,
    Guid groupId,
    CreateGroupAnnouncementRequest request,
    CancellationToken cancellationToken = default)
{
    var group = await GetActiveGroupAsync(groupId, cancellationToken);
    EnsureOwner(currentUser.UserId, group);
    var announcement = new GroupAnnouncement(
        Guid.NewGuid(),
        groupId,
        currentUser.UserId,
        GroupAnnouncementKind.Manual,
        NormalizeAnnouncementTitle(request.Title),
        NormalizeAnnouncementBody(request.Body),
        clock.UtcNow);

    await groupRepository.SaveAnnouncementAsync(announcement, cancellationToken);
    return (await MapAnnouncementsAsync(new[] { announcement }, cancellationToken)).Single();
}
```

Add helpers:

```csharp
private async Task<IReadOnlyList<GroupAnnouncementDto>> MapAnnouncementsAsync(
    IEnumerable<GroupAnnouncement> announcements,
    CancellationToken cancellationToken)
{
    var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);
    var profiles = (await profileRepository.ListProfilesAsync(cancellationToken)).ToDictionary(profile => profile.UserId);

    return announcements
        .OrderByDescending(announcement => announcement.CreatedAtUtc)
        .Select(announcement =>
        {
            var account = announcement.AuthorUserId.HasValue && accounts.TryGetValue(announcement.AuthorUserId.Value, out var foundAccount)
                ? foundAccount
                : null;
            var profile = announcement.AuthorUserId.HasValue
                ? profiles.GetValueOrDefault(announcement.AuthorUserId.Value)
                : null;

            return new GroupAnnouncementDto(
                announcement.Id,
                announcement.GroupId,
                announcement.AuthorUserId,
                account?.Username,
                profile?.DisplayName ?? account?.Username,
                announcement.Kind,
                announcement.Title,
                announcement.Body,
                announcement.CreatedAtUtc);
        })
        .ToArray();
}

private static string NormalizeAnnouncementTitle(string? value)
{
    var normalized = value?.Trim();
    if (string.IsNullOrWhiteSpace(normalized))
    {
        throw ApiException.BadRequest("title is required.");
    }

    return normalized.Length <= 120 ? normalized : throw ApiException.BadRequest("title cannot exceed 120 characters.");
}

private static string NormalizeAnnouncementBody(string? value)
{
    var normalized = value?.Trim();
    if (string.IsNullOrWhiteSpace(normalized))
    {
        throw ApiException.BadRequest("body is required.");
    }

    return normalized.Length <= 1000 ? normalized : throw ApiException.BadRequest("body cannot exceed 1000 characters.");
}
```

- [ ] **Step 7: Run unit test and commit task**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.UnitTests\TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~CreateAnnouncementAsync_OwnerCreatesManualAnnouncement"
```

Expected: test passes.

Commit:

```powershell
git add src\TasteBudz.Backend\Modules\Groups\IGroupRepository.cs src\TasteBudz.Backend\Infrastructure\Persistence\InMemory\InMemoryTasteBudzStore.cs src\TasteBudz.Backend\Modules\Groups\InMemoryGroupRepository.cs src\TasteBudz.Backend\Modules\Groups\GroupService.cs tests\TasteBudz.Backend.UnitTests\Groups\GroupServiceTests.cs
git commit -m "feat: persist in-memory group announcements"
```

---

### Task 3: Add API Endpoints and Authorization Tests

**Files:**
- Modify: `src/TasteBudz.Backend/Controllers/GroupsController.cs`
- Test: `tests/TasteBudz.Backend.UnitTests/Groups/GroupServiceTests.cs`
- Test: `tests/TasteBudz.Backend.IntegrationTests/Api/GroupsApiTests.cs`

- [ ] **Step 1: Write service test for non-owner rejection**

Add:

```csharp
[Fact]
public async Task CreateAnnouncementAsync_NonOwnerIsForbidden()
{
    var clock = new TestClock(new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero));
    var services = CreateServices(clock);
    var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
    var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
    var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
    {
        Name = "Guarded Announcements",
        Visibility = GroupVisibility.Public,
    });

    var exception = await Assert.ThrowsAsync<ApiException>(() =>
        services.GroupService.CreateAnnouncementAsync(ToCurrentUser(guest), group.GroupId, new CreateGroupAnnouncementRequest
        {
            Title = "Not allowed",
            Body = "Guest cannot post.",
        }));

    Assert.Equal(403, exception.StatusCode);
}
```

- [ ] **Step 2: Write API integration test**

Add to `GroupsApiTests`:

```csharp
[Fact]
public async Task GroupAnnouncements_OnlyOwnerCanCreateAndViewersCanList()
{
    factory.ResetState();
    using var ownerClient = factory.CreateClient();
    using var guestClient = factory.CreateClient();

    var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
    var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
    ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
    ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

    var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
    {
        Name = "Announcement API",
        Visibility = GroupVisibility.Public,
    });
    var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

    var forbiddenResponse = await guestClient.PostAsJsonAsync($"/api/v1/groups/{group!.GroupId}/announcements", new CreateGroupAnnouncementRequest
    {
        Title = "Guest post",
        Body = "This should fail.",
    });
    var createAnnouncementResponse = await ownerClient.PostAsJsonAsync($"/api/v1/groups/{group.GroupId}/announcements", new CreateGroupAnnouncementRequest
    {
        Title = "Owner post",
        Body = "Friday plan is open.",
    });
    var listResponse = await guestClient.GetAsync($"/api/v1/groups/{group.GroupId}/announcements");
    var announcements = await listResponse.Content.ReadFromJsonAsync<ListResponse<GroupAnnouncementDto>>(ApiTestHelpers.JsonOptions);

    Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, createAnnouncementResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    Assert.Contains(announcements!.Items, item => item.Title == "Owner post" && item.Kind == GroupAnnouncementKind.Manual);
}
```

- [ ] **Step 3: Run tests and verify failure**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.IntegrationTests\TasteBudz.Backend.IntegrationTests.csproj --filter "FullyQualifiedName~GroupAnnouncements_OnlyOwnerCanCreateAndViewersCanList"
```

Expected: route returns `404` because endpoints are missing.

- [ ] **Step 4: Add controller endpoints**

In `GroupsController`, add:

```csharp
[HttpGet("{groupId:guid}/announcements")]
public Task<ListResponse<GroupAnnouncementDto>> ListAnnouncements(Guid groupId, CancellationToken cancellationToken) =>
    groupService.ListAnnouncementsAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, groupId, cancellationToken);

[HttpPost("{groupId:guid}/announcements")]
public Task<GroupAnnouncementDto> CreateAnnouncement(Guid groupId, [FromBody] CreateGroupAnnouncementRequest request, CancellationToken cancellationToken) =>
    groupService.CreateAnnouncementAsync(currentUserAccessor.GetRequiredCurrentUser(), groupId, request, cancellationToken);
```

- [ ] **Step 5: Run tests and commit task**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.UnitTests\TasteBudz.Backend.UnitTests.csproj --filter "FullyQualifiedName~CreateAnnouncementAsync_NonOwnerIsForbidden"
dotnet test tests\TasteBudz.Backend.IntegrationTests\TasteBudz.Backend.IntegrationTests.csproj --filter "FullyQualifiedName~GroupAnnouncements_OnlyOwnerCanCreateAndViewersCanList"
```

Expected: both pass.

Commit:

```powershell
git add src\TasteBudz.Backend\Controllers\GroupsController.cs tests\TasteBudz.Backend.UnitTests\Groups\GroupServiceTests.cs tests\TasteBudz.Backend.IntegrationTests\Api\GroupsApiTests.cs
git commit -m "feat: expose group announcement endpoints"
```

---

### Task 4: Add Relational Persistence and Schema

**Files:**
- Modify: `src/TasteBudz.Database/sqlite/dbTasteBudz.sqlite.sql`
- Modify: `src/TasteBudz.Database/sqlserver/010_schema.sql`
- Modify: `src/TasteBudz.Backend/Infrastructure/Persistence/Sqlite/SqliteEntities.cs`
- Modify: `src/TasteBudz.Backend/Infrastructure/Persistence/Sqlite/TasteBudzDbContext.cs`
- Modify: `src/TasteBudz.Backend/Modules/Groups/SqliteGroupRepository.cs`
- Test: `tests/TasteBudz.Backend.IntegrationTests/Api/GroupsApiTests.cs`

- [ ] **Step 1: Run API test against current relational path**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.IntegrationTests\TasteBudz.Backend.IntegrationTests.csproj --filter "FullyQualifiedName~GroupAnnouncements_OnlyOwnerCanCreateAndViewersCanList"
```

Expected: failure once the test hits SQLite because announcements are not persisted in the SQLite repository/schema.

- [ ] **Step 2: Update SQLite schema**

In `dbTasteBudz.sqlite.sql`, add `WallpaperTheme INTEGER NOT NULL DEFAULT 0` to the `Groups` table.

Add:

```sql
CREATE TABLE IF NOT EXISTS GroupAnnouncements (
    AnnouncementId TEXT PRIMARY KEY,
    GroupId TEXT NOT NULL,
    AuthorUserId TEXT NULL,
    Kind INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Body TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    CONSTRAINT FK_GroupAnnouncements_Groups FOREIGN KEY (GroupId) REFERENCES Groups(GroupId) ON DELETE CASCADE,
    CONSTRAINT FK_GroupAnnouncements_UserAccounts FOREIGN KEY (AuthorUserId) REFERENCES UserAccounts(UserId) ON DELETE SET NULL,
    CONSTRAINT CK_GroupAnnouncements_Kind CHECK (Kind IN (0, 1)),
    CONSTRAINT CK_GroupAnnouncements_Title_NotBlank CHECK (length(trim(Title)) > 0),
    CONSTRAINT CK_GroupAnnouncements_Body_NotBlank CHECK (length(trim(Body)) > 0)
);

CREATE INDEX IF NOT EXISTS IX_GroupAnnouncements_GroupId_CreatedAtUtc
ON GroupAnnouncements(GroupId, CreatedAtUtc DESC);
```

- [ ] **Step 3: Update SQL Server schema**

In `010_schema.sql`, add `WallpaperTheme int NOT NULL CONSTRAINT DF_Groups_WallpaperTheme DEFAULT 0` to `Groups`.

Add:

```sql
CREATE TABLE dbo.GroupAnnouncements (
    AnnouncementId uniqueidentifier NOT NULL CONSTRAINT PK_GroupAnnouncements PRIMARY KEY,
    GroupId uniqueidentifier NOT NULL,
    AuthorUserId uniqueidentifier NULL,
    Kind int NOT NULL,
    Title nvarchar(120) NOT NULL,
    Body nvarchar(1000) NOT NULL,
    CreatedAtUtc datetimeoffset NOT NULL,
    CONSTRAINT FK_GroupAnnouncements_Groups FOREIGN KEY (GroupId) REFERENCES dbo.Groups(GroupId),
    CONSTRAINT FK_GroupAnnouncements_UserAccounts FOREIGN KEY (AuthorUserId) REFERENCES dbo.UserAccounts(UserId),
    CONSTRAINT CK_GroupAnnouncements_Kind CHECK (Kind IN (0, 1)),
    CONSTRAINT CK_GroupAnnouncements_Title_NotBlank CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
    CONSTRAINT CK_GroupAnnouncements_Body_NotBlank CHECK (LEN(LTRIM(RTRIM(Body))) > 0)
);

CREATE INDEX IX_GroupAnnouncements_GroupId_CreatedAtUtc
ON dbo.GroupAnnouncements(GroupId, CreatedAtUtc DESC);
```

- [ ] **Step 4: Add EF persistence entity**

In `SqliteEntities.cs`, add:

```csharp
public sealed class GroupAnnouncementEntity
{
    public Guid AnnouncementId { get; set; }
    public Guid GroupId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public int Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
```

Add `public int WallpaperTheme { get; set; }` to the group entity.

- [ ] **Step 5: Map EF entity**

In `TasteBudzDbContext.cs`, add:

```csharp
public DbSet<GroupAnnouncementEntity> GroupAnnouncements => Set<GroupAnnouncementEntity>();
```

In `OnModelCreating`, configure:

```csharp
modelBuilder.Entity<GroupAnnouncementEntity>(entity =>
{
    entity.ToTable("GroupAnnouncements");
    entity.HasKey(item => item.AnnouncementId);
    entity.Property(item => item.Title).HasMaxLength(120).IsRequired();
    entity.Property(item => item.Body).HasMaxLength(1000).IsRequired();
    entity.HasIndex(item => new { item.GroupId, item.CreatedAtUtc });
});
```

Also map `WallpaperTheme` on the group entity.

- [ ] **Step 6: Update SQLite repository mapping**

In `SqliteGroupRepository`, map group theme both directions:

```csharp
WallpaperTheme = (int)group.WallpaperTheme,
```

and:

```csharp
(GroupWallpaperTheme)entity.WallpaperTheme
```

Add announcement methods:

```csharp
public async Task<IReadOnlyCollection<GroupAnnouncement>> ListAnnouncementsAsync(Guid groupId, CancellationToken cancellationToken = default) =>
    await dbContext.GroupAnnouncements
        .AsNoTracking()
        .Where(item => item.GroupId == groupId)
        .OrderByDescending(item => item.CreatedAtUtc)
        .Select(item => new GroupAnnouncement(
            item.AnnouncementId,
            item.GroupId,
            item.AuthorUserId,
            (GroupAnnouncementKind)item.Kind,
            item.Title,
            item.Body,
            item.CreatedAtUtc))
        .ToArrayAsync(cancellationToken);

public async Task SaveAnnouncementAsync(GroupAnnouncement announcement, CancellationToken cancellationToken = default)
{
    var entity = await dbContext.GroupAnnouncements.FindAsync([announcement.Id], cancellationToken);
    if (entity is null)
    {
        dbContext.GroupAnnouncements.Add(new GroupAnnouncementEntity
        {
            AnnouncementId = announcement.Id,
            GroupId = announcement.GroupId,
            AuthorUserId = announcement.AuthorUserId,
            Kind = (int)announcement.Kind,
            Title = announcement.Title,
            Body = announcement.Body,
            CreatedAtUtc = announcement.CreatedAtUtc,
        });
    }
    else
    {
        entity.AuthorUserId = announcement.AuthorUserId;
        entity.Kind = (int)announcement.Kind;
        entity.Title = announcement.Title;
        entity.Body = announcement.Body;
        entity.CreatedAtUtc = announcement.CreatedAtUtc;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
}
```

- [ ] **Step 7: Run integration tests and commit task**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.IntegrationTests\TasteBudz.Backend.IntegrationTests.csproj --filter "FullyQualifiedName~GroupsApiTests"
```

Expected: group API tests pass.

Commit:

```powershell
git add src\TasteBudz.Database\sqlite\dbTasteBudz.sqlite.sql src\TasteBudz.Database\sqlserver\010_schema.sql src\TasteBudz.Backend\Infrastructure\Persistence\Sqlite src\TasteBudz.Backend\Modules\Groups\SqliteGroupRepository.cs tests\TasteBudz.Backend.IntegrationTests\Api\GroupsApiTests.cs
git commit -m "feat: store group announcements in relational schema"
```

---

### Task 5: Create System Announcements for Group Events

**Files:**
- Modify: `src/TasteBudz.Backend/Modules/Groups/GroupService.cs`
- Modify: `src/TasteBudz.Backend/Modules/Events/EventService.cs`
- Modify: dependency registration file that currently registers Events/Groups services
- Test: `tests/TasteBudz.Backend.IntegrationTests/Api/GroupsApiTests.cs`

- [ ] **Step 1: Write failing integration test**

Add to `GroupsApiTests`:

```csharp
[Fact]
public async Task CreatingGroupLinkedEvent_CreatesSystemAnnouncement()
{
    factory.ResetState();
    using var ownerClient = factory.CreateClient();

    var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
    ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);

    var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
    {
        Name = "Event Announcements",
        Visibility = GroupVisibility.Public,
    });
    var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

    var createEventResponse = await ownerClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
    {
        Title = "Ramen Backup Plan",
        EventType = EventType.Open,
        EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        Capacity = 4,
        SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        GroupId = group!.GroupId,
    });
    var listResponse = await ownerClient.GetAsync($"/api/v1/groups/{group.GroupId}/announcements");
    var announcements = await listResponse.Content.ReadFromJsonAsync<ListResponse<GroupAnnouncementDto>>(ApiTestHelpers.JsonOptions);

    Assert.Equal(HttpStatusCode.Created, createEventResponse.StatusCode);
    Assert.Contains(announcements!.Items, item =>
        item.Kind == GroupAnnouncementKind.System &&
        item.Title == "New group event" &&
        item.Body.Contains("Ramen Backup Plan", StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.IntegrationTests\TasteBudz.Backend.IntegrationTests.csproj --filter "FullyQualifiedName~CreatingGroupLinkedEvent_CreatesSystemAnnouncement"
```

Expected: assertion fails because no system announcement exists.

- [ ] **Step 3: Add Groups service system method**

In `GroupService`, add:

```csharp
public async Task CreateSystemEventAnnouncementAsync(
    Guid groupId,
    string eventTitle,
    CancellationToken cancellationToken = default)
{
    var group = await GetActiveGroupAsync(groupId, cancellationToken);
    var safeTitle = string.IsNullOrWhiteSpace(eventTitle) ? "Untitled event" : eventTitle.Trim();
    var announcement = new GroupAnnouncement(
        Guid.NewGuid(),
        group.Id,
        null,
        GroupAnnouncementKind.System,
        "New group event",
        $"New group event created: {safeTitle}.",
        clock.UtcNow);

    await groupRepository.SaveAnnouncementAsync(announcement, cancellationToken);
}
```

- [ ] **Step 4: Call after event creation**

In `EventService`, inject `GroupService` or a smaller interface such as `IGroupAnnouncementService`. Prefer the smaller interface if adding it is straightforward:

```csharp
public interface IGroupAnnouncementService
{
    Task CreateSystemEventAnnouncementAsync(Guid groupId, string eventTitle, CancellationToken cancellationToken = default);
}
```

Have `GroupService` implement it. After an event is successfully saved and host participant is created, add:

```csharp
if (created.GroupId.HasValue)
{
    await groupAnnouncementService.CreateSystemEventAnnouncementAsync(
        created.GroupId.Value,
        string.IsNullOrWhiteSpace(created.Title) ? "Untitled event" : created.Title,
        cancellationToken);
}
```

- [ ] **Step 5: Run tests and commit task**

Run:

```powershell
dotnet test tests\TasteBudz.Backend.IntegrationTests\TasteBudz.Backend.IntegrationTests.csproj --filter "FullyQualifiedName~CreatingGroupLinkedEvent_CreatesSystemAnnouncement|FullyQualifiedName~PublicGroupBrowseAndLinkedEvents_ReturnExpectedResults"
```

Expected: both pass.

Commit:

```powershell
git add src\TasteBudz.Backend\Modules\Groups src\TasteBudz.Backend\Modules\Events\EventService.cs tests\TasteBudz.Backend.IntegrationTests\Api\GroupsApiTests.cs
git commit -m "feat: announce new group events"
```

---

### Task 6: Add MVC API Client and View Models

**Files:**
- Modify: `src/TasteBudz.Web.Mvc/Services/BackendApi/GroupApiService.cs`
- Modify: `src/TasteBudz.Web.Mvc/ViewModels/GroupViewModels.cs`
- Test: `tests/TasteBudz.Web.Mvc.IntegrationTests/Services/GroupApiServiceTests.cs`

- [ ] **Step 1: Write failing MVC API service test**

In `GroupApiServiceTests`, extend `BrowseDetailAndLinkedEvents_SendExpectedRoutes` or add:

```csharp
[Fact]
public async Task AnnouncementEndpoints_SendExpectedRoutesAndBodies()
{
    var context = new BackendApiServiceTestContext();
    await context.SignInAsync();
    var service = context.CreateService(client => new GroupApiService(client));
    var groupId = Guid.NewGuid();
    var announcementId = Guid.NewGuid();

    context.BackendHandler.Enqueue(
        HttpMethod.Get,
        $"/api/v1/groups/{groupId}/announcements",
        (_, _) => StubBackendApiHandler.Json(
            HttpStatusCode.OK,
            new ListResponse<GroupAnnouncementDto>(
                new[]
                {
                    new GroupAnnouncementDto(announcementId, groupId, null, null, null, GroupAnnouncementKind.System, "New group event", "New group event created: Ramen.", DateTimeOffset.UtcNow),
                },
                1)));
    context.BackendHandler.Enqueue(
        HttpMethod.Post,
        $"/api/v1/groups/{groupId}/announcements",
        (_, _) => StubBackendApiHandler.Json(
            HttpStatusCode.OK,
            new GroupAnnouncementDto(announcementId, groupId, Guid.NewGuid(), "alex", "Alex Carter", GroupAnnouncementKind.Manual, "Friday plan", "Poll is open.", DateTimeOffset.UtcNow)));

    var announcements = await service.ListAnnouncementsAsync(groupId);
    await service.CreateAnnouncementAsync(groupId, new CreateGroupAnnouncementRequest
    {
        Title = "Friday plan",
        Body = "Poll is open.",
    });

    Assert.Single(announcements.Items);
    Assert.Contains(
        "\"title\":\"Friday plan\"",
        context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/groups/{groupId}/announcements" && request.Method == HttpMethod.Post).Body);
    context.BackendHandler.AssertDrained();
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests\TasteBudz.Web.Mvc.IntegrationTests\TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~AnnouncementEndpoints_SendExpectedRoutesAndBodies"
```

Expected: compile failure because MVC client methods are missing.

- [ ] **Step 3: Add MVC client methods**

In `GroupApiService`, add:

```csharp
public Task<ListResponse<GroupAnnouncementDto>> ListAnnouncementsAsync(Guid groupId, CancellationToken cancellationToken = default) =>
    backendHttpClient.GetAsync<ListResponse<GroupAnnouncementDto>>(
        $"/api/v1/groups/{groupId}/announcements",
        cancellationToken);

public Task<GroupAnnouncementDto> CreateAnnouncementAsync(
    Guid groupId,
    CreateGroupAnnouncementRequest request,
    CancellationToken cancellationToken = default) =>
    backendHttpClient.PostAsync<CreateGroupAnnouncementRequest, GroupAnnouncementDto>(
        $"/api/v1/groups/{groupId}/announcements",
        request,
        cancellationToken: cancellationToken);
```

- [ ] **Step 4: Add view model fields**

In `GroupManageViewModel`, add:

```csharp
public GroupWallpaperTheme WallpaperTheme { get; init; }
public GroupWallpaperTheme? EditWallpaperTheme { get; set; }
public IReadOnlyList<GroupAnnouncementItem> Announcements { get; init; } = [];
public string? AnnouncementTitle { get; set; }
public string? AnnouncementBody { get; set; }
public static IReadOnlyList<GroupWallpaperTheme> AvailableWallpaperThemes { get; } =
[
    GroupWallpaperTheme.Default,
    GroupWallpaperTheme.Sushi,
    GroupWallpaperTheme.Tacos,
    GroupWallpaperTheme.Brunch,
    GroupWallpaperTheme.Ramen,
    GroupWallpaperTheme.Pizza,
    GroupWallpaperTheme.Dumplings,
];
```

Add `announcements` parameter to `FromDto` and map `WallpaperTheme`.

Create `GroupAnnouncementItem`:

```csharp
public sealed class GroupAnnouncementItem
{
    public Guid AnnouncementId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string AuthorLabel { get; init; } = string.Empty;
    public string CreatedLabel { get; init; } = string.Empty;
    public bool IsSystem { get; init; }

    public static GroupAnnouncementItem FromDto(GroupAnnouncementDto dto) => new()
    {
        AnnouncementId = dto.AnnouncementId,
        Title = dto.Title,
        Body = dto.Body,
        Kind = dto.Kind.ToString(),
        AuthorLabel = dto.Kind == GroupAnnouncementKind.System
            ? "TasteBudz"
            : dto.AuthorDisplayName ?? dto.AuthorUsername ?? "Group owner",
        CreatedLabel = dto.CreatedAtUtc.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture),
        IsSystem = dto.Kind == GroupAnnouncementKind.System,
    };
}
```

- [ ] **Step 5: Run MVC service tests and commit task**

Run:

```powershell
dotnet test tests\TasteBudz.Web.Mvc.IntegrationTests\TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~GroupApiServiceTests"
```

Expected: tests pass.

Commit:

```powershell
git add src\TasteBudz.Web.Mvc\Services\BackendApi\GroupApiService.cs src\TasteBudz.Web.Mvc\ViewModels\GroupViewModels.cs tests\TasteBudz.Web.Mvc.IntegrationTests\Services\GroupApiServiceTests.cs
git commit -m "feat: add MVC group announcement client models"
```

---

### Task 7: Render Group Hub UI and Owner Forms

**Files:**
- Modify: `src/TasteBudz.Web.Mvc/Controllers/GroupController.cs`
- Modify: `src/TasteBudz.Web.Mvc/Views/Group/Manage.cshtml`
- Modify: `src/TasteBudz.Web.Mvc/wwwroot/css/site.css`
- Test: `tests/TasteBudz.Web.Mvc.IntegrationTests/Api/GroupMvcTests.cs`

- [ ] **Step 1: Write failing MVC page test**

Add to `GroupMvcTests`:

```csharp
[Fact]
public async Task Manage_ForOwner_RendersAnnouncementsAndWallpaperTools()
{
    using var factory = new TasteBudzMvcFactory();
    using var client = MvcTestHelpers.CreateClient(factory);
    var groupId = Guid.NewGuid();
    var announcementId = Guid.NewGuid();

    var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
    factory.BackendHandler.Requests.Clear();

    factory.BackendHandler.Enqueue(
        HttpMethod.Get,
        $"/api/v1/groups/{groupId}",
        (_, _) => StubBackendApiHandler.Json(
            HttpStatusCode.OK,
            new GroupDetailDto(
                groupId,
                session.CurrentUser.UserId,
                "Late Night Sushi Club",
                "Dinner club",
                GroupVisibility.Public,
                GroupLifecycleState.Active,
                GroupWallpaperTheme.Sushi,
                true,
                new[]
                {
                    new GroupMemberDto(session.CurrentUser.UserId, "alex", "Alex Carter", GroupMemberState.Active, DateTimeOffset.UtcNow),
                })));
    factory.BackendHandler.Enqueue(
        HttpMethod.Get,
        $"/api/v1/groups/{groupId}/events?page=1&pageSize=50",
        (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new ListResponse<EventSummaryDto>(Array.Empty<EventSummaryDto>(), 0)));
    factory.BackendHandler.Enqueue(
        HttpMethod.Get,
        $"/api/v1/groups/{groupId}/announcements",
        (_, _) => StubBackendApiHandler.Json(
            HttpStatusCode.OK,
            new ListResponse<GroupAnnouncementDto>(
                new[]
                {
                    new GroupAnnouncementDto(announcementId, groupId, session.CurrentUser.UserId, "alex", "Alex Carter", GroupAnnouncementKind.Manual, "Friday plan", "Poll is open.", DateTimeOffset.UtcNow),
                },
                1)));

    using var response = await client.GetAsync($"/Group/Manage?groupId={groupId}");
    var html = await response.Content.ReadAsStringAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("Late Night Sushi Club", html);
    Assert.Contains("Member Gallery", html);
    Assert.Contains("Announcements", html);
    Assert.Contains("Friday plan", html);
    Assert.Contains("Food Background", html);
    Assert.Contains("Sushi", html);
    Assert.Contains("AnnouncementTitle", html);
    factory.BackendHandler.AssertDrained();
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests\TasteBudz.Web.Mvc.IntegrationTests\TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~Manage_ForOwner_RendersAnnouncementsAndWallpaperTools"
```

Expected: compile failure or assertion failure because announcements are not loaded/rendered.

- [ ] **Step 3: Load announcements in controller**

In `GroupController.Manage`, add:

```csharp
var announcements = await TryListAnnouncementsAsync(groupId, cancellationToken);
return View(GroupManageViewModel.FromDto(detail, currentUserId, eventHistory, announcements));
```

Add:

```csharp
private async Task<IReadOnlyCollection<GroupAnnouncementDto>> TryListAnnouncementsAsync(
    Guid groupId,
    CancellationToken cancellationToken)
{
    try
    {
        var result = await groupApiService.ListAnnouncementsAsync(groupId, cancellationToken);
        return result.Items;
    }
    catch (BackendAuthenticationExpiredException)
    {
        throw;
    }
    catch (BackendApiException)
    {
        return [];
    }
}
```

In `UpdateSettings`, pass `WallpaperTheme = model.EditWallpaperTheme`.

Add a new action:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateAnnouncement(Guid groupId, GroupManageViewModel model, CancellationToken cancellationToken)
{
    try
    {
        await groupApiService.CreateAnnouncementAsync(groupId, new CreateGroupAnnouncementRequest
        {
            Title = model.AnnouncementTitle,
            Body = model.AnnouncementBody,
        }, cancellationToken);
        TempData["StatusMessage"] = "Announcement posted.";
    }
    catch (BackendAuthenticationExpiredException)
    {
        return await RedirectToLoginAsync(cancellationToken);
    }
    catch (BackendApiException ex)
    {
        TempData["StatusMessage"] = $"Could not post announcement: {ex.Message}";
    }

    return RedirectToAction(nameof(Manage), new { groupId });
}
```

- [ ] **Step 4: Update Razor layout**

In `Manage.cshtml`, keep the current app classes but rename/reorganize copy:

```razor
<section class="profile-shell group-page group-page--manage group-wallpaper group-wallpaper--@Model.WallpaperTheme.ToString().ToLowerInvariant()">
```

Render announcement panel:

```razor
<section class="panel stack group-announcements-panel">
    <div class="group-section__header">
        <div>
            <p class="group-section__eyebrow">Announcements</p>
            <h2>Owner Posts</h2>
        </div>
    </div>

    @if (Model.Announcements.Count == 0)
    {
        <p class="empty-state">No announcements yet.</p>
    }
    else
    {
        <div class="group-announcement-list">
            @foreach (var announcement in Model.Announcements)
            {
                <article class="summary-card group-announcement @(announcement.IsSystem ? "group-announcement--system" : null)">
                    <div class="group-announcement__meta">
                        <span>@announcement.AuthorLabel</span>
                        <span>@announcement.CreatedLabel</span>
                    </div>
                    <h3>@announcement.Title</h3>
                    <p>@announcement.Body</p>
                </article>
            }
        </div>
    }
</section>
```

For owner composer:

```razor
@if (Model.IsCurrentUserOwner)
{
    <section class="panel stack">
        <div class="group-section__header group-section__header--compact">
            <div>
                <p class="group-section__eyebrow">Announcement</p>
                <h2>Post Update</h2>
            </div>
        </div>
        <form asp-action="CreateAnnouncement" asp-route-groupId="@Model.GroupId" method="post" class="stack">
            @Html.AntiForgeryToken()
            <div class="field">
                <label for="AnnouncementTitle">Title</label>
                <input id="AnnouncementTitle" name="AnnouncementTitle" maxlength="120" />
            </div>
            <div class="field">
                <label for="AnnouncementBody">Message</label>
                <textarea id="AnnouncementBody" name="AnnouncementBody" maxlength="1000"></textarea>
            </div>
            <button type="submit" class="button button--primary">Post Announcement</button>
        </form>
    </section>
}
```

Add wallpaper select to owner settings:

```razor
<div class="field">
    <label for="EditWallpaperTheme">Food Background</label>
    <select id="EditWallpaperTheme" name="EditWallpaperTheme">
        @foreach (var theme in GroupManageViewModel.AvailableWallpaperThemes)
        {
            <option value="@theme" selected="@(Model.EditWallpaperTheme == theme ? "selected" : null)">@theme</option>
        }
    </select>
</div>
```

- [ ] **Step 5: Add CSS**

Append focused classes to the groups UI section in `site.css`:

```css
.group-wallpaper .group-manage-hero {
    border: 1px solid rgba(193, 126, 81, 0.14);
}

.group-wallpaper--sushi .group-manage-hero::before {
    background:
        radial-gradient(circle at 82% 18%, rgba(255, 190, 120, 0.78), transparent 25%),
        radial-gradient(circle at 15% 15%, rgba(255, 235, 210, 0.92), transparent 28%),
        linear-gradient(135deg, #fff1e4 0%, #ffd6bd 48%, #fff8ef 100%);
}

.group-wallpaper--tacos .group-manage-hero::before {
    background:
        radial-gradient(circle at 20% 20%, rgba(245, 200, 81, 0.52), transparent 32%),
        linear-gradient(135deg, #fff4d8, #ffd0a8);
}

.group-wallpaper--brunch .group-manage-hero::before {
    background:
        radial-gradient(circle at 80% 30%, rgba(248, 217, 160, 0.7), transparent 36%),
        linear-gradient(135deg, #fff7e8, #dfead6);
}

.group-wallpaper--ramen .group-manage-hero::before {
    background:
        radial-gradient(circle at 70% 25%, rgba(255, 159, 122, 0.52), transparent 36%),
        linear-gradient(135deg, #fff0df, #f2b06b);
}

.group-wallpaper--pizza .group-manage-hero::before {
    background:
        radial-gradient(circle at 76% 20%, rgba(226, 92, 64, 0.36), transparent 34%),
        linear-gradient(135deg, #fff2d6, #ffd59c);
}

.group-wallpaper--dumplings .group-manage-hero::before {
    background:
        radial-gradient(circle at 20% 18%, rgba(229, 238, 224, 0.92), transparent 35%),
        linear-gradient(135deg, #fff8ef, #e9efd9);
}

.group-announcement-list {
    display: grid;
    gap: 0.85rem;
}

.group-announcement {
    display: grid;
    gap: 0.55rem;
}

.group-announcement--system {
    background: linear-gradient(135deg, rgba(255, 240, 227, 0.98), rgba(255, 250, 245, 0.94));
    border-color: rgba(239, 107, 63, 0.18);
}

.group-announcement__meta {
    display: flex;
    justify-content: space-between;
    gap: 0.75rem;
    color: var(--muted);
    font-size: 0.82rem;
    font-weight: 700;
    flex-wrap: wrap;
}

.group-announcement h3,
.group-announcement p {
    margin: 0;
}
```

- [ ] **Step 6: Run MVC tests and commit task**

Run:

```powershell
dotnet test tests\TasteBudz.Web.Mvc.IntegrationTests\TasteBudz.Web.Mvc.IntegrationTests.csproj --filter "FullyQualifiedName~GroupMvcTests"
```

Expected: group MVC tests pass.

Commit:

```powershell
git add src\TasteBudz.Web.Mvc\Controllers\GroupController.cs src\TasteBudz.Web.Mvc\ViewModels\GroupViewModels.cs src\TasteBudz.Web.Mvc\Views\Group\Manage.cshtml src\TasteBudz.Web.Mvc\wwwroot\css\site.css tests\TasteBudz.Web.Mvc.IntegrationTests\Api\GroupMvcTests.cs
git commit -m "feat: render group hub announcements"
```

---

### Task 8: Update Authoritative Documentation

**Files:**
- Modify: `docs/TasteBudz_Functional_Requirements.md`
- Modify: `docs/backend/backend-decisions.md`
- Modify: `docs/backend/domain-model.md`
- Modify: `docs/backend/api-endpoints.md`
- Modify: `docs/backend/testing-strategy.md`

- [ ] **Step 1: Update functional requirements**

In FR-011/FR-012 area, add acceptance bullets:

```markdown
- Group owners can publish simple text announcements visible wherever the group page is visible.
- When a new event is created with a group context, the system creates a group announcement for that event.
- Group owners can choose one preset food-themed wallpaper for group personalization in MVP.
- Uploaded custom group wallpaper images remain later work.
```

- [ ] **Step 2: Add backend decision**

Append to `backend-decisions.md`:

```markdown
## [ADR-032] Group Announcements and Preset Wallpapers Are MVP Group Personalization

- Date: 2026-04-24
- Status: Accepted
- Owners: Backend team

### Context
Groups need a more complete page experience with owner-authored updates and lightweight personalization, but uploaded group media would expand storage, moderation, and authorization scope.

### Decision
MVP groups support first-class text announcements and preset food-themed wallpaper selection. Group owners can create manual announcements. The system creates a system announcement when a group-linked event is created. Announcement visibility follows existing group page visibility. Wallpaper is stored as a constrained preset enum on the group.

### Consequences
- Group announcements are distinct from group chat and remain owner/system-authored.
- Preset wallpapers personalize groups without adding a new media context.
- Uploaded custom group wallpaper images require a future decision and `GroupWallpaper` media context.
```

- [ ] **Step 3: Update domain model**

Add `GroupAnnouncement` to group entities and relationships, add `GroupWallpaperTheme` to enums, and add invariants:

```markdown
- Manual group announcements are owner-authored.
- System group announcements are server-created.
- Announcement visibility follows group visibility.
- Group wallpaper theme is presentation metadata and does not affect access rules.
```

- [ ] **Step 4: Update API endpoints**

Under Groups, add:

```markdown
| List Group Announcements | GET | `/api/v1/groups/{groupId}/announcements` | List announcements visible to the caller | Yes |
| Create Group Announcement | POST | `/api/v1/groups/{groupId}/announcements` | Owner creates a manual group announcement | GroupOwner |
```

Add `wallpaperTheme` to group update contract examples.

- [ ] **Step 5: Update testing strategy**

Under Groups and messaging access P1, add:

```markdown
- group owner can create announcements
- non-owner cannot create announcements
- private-group announcements follow private group access
- creating a group-linked event creates a system announcement
- wallpaper theme update is owner-only and rejects invalid themes
```

- [ ] **Step 6: Run docs grep and commit**

Run:

```powershell
Select-String -Path docs\TasteBudz_Functional_Requirements.md,docs\backend\backend-decisions.md,docs\backend\domain-model.md,docs\backend\api-endpoints.md,docs\backend\testing-strategy.md -Pattern "announcement|wallpaper" -CaseSensitive:$false
```

Expected: each authoritative doc has relevant entries.

Commit:

```powershell
git add docs\TasteBudz_Functional_Requirements.md docs\backend\backend-decisions.md docs\backend\domain-model.md docs\backend\api-endpoints.md docs\backend\testing-strategy.md
git commit -m "docs: document group announcements and wallpapers"
```

---

### Task 9: Full Verification and Responsive Review

**Files:**
- No required source changes unless verification finds defects.
- Artifacts: use `output/playwright/`.

- [ ] **Step 1: Run backend and MVC tests**

Run:

```powershell
dotnet test TasteBudz.sln
```

Expected: all tests pass.

- [ ] **Step 2: Build release**

Run:

```powershell
dotnet build TasteBudz.sln -c Release
```

Expected: build succeeds with no new warnings caused by this work.

- [ ] **Step 3: Launch local app**

Run:

```powershell
.\start-dev.ps1
```

Expected: MVC app and backend start locally.

- [ ] **Step 4: Playwright desktop screenshot**

Open the group manage page for a seeded or newly created group. Capture:

```powershell
$env:CODEX_HOME="C:\Users\uslep\.codex"
$env:PWCLI="$env:CODEX_HOME\skills\playwright\scripts\playwright_cli.sh"
```

If the shell wrapper is unavailable on Windows, use the existing project Playwright/MCP workflow already producing `output/playwright/groups-manage-*.png`.

Required desktop artifact:

```text
output/playwright/groups-manage-announcements-desktop.png
```

Expected: hero wallpaper aligns with app style; member gallery, announcements, event history, and owner panels are aligned with no overlap.

- [ ] **Step 5: Playwright mobile screenshot**

Capture narrow viewport artifact:

```text
output/playwright/groups-manage-announcements-mobile.png
```

Expected: one-column layout, full-width actions, no horizontal scroll, no clipped controls.

- [ ] **Step 6: Fix any visual defects and rerun screenshots**

If screenshots show overlap, clipping, poor spacing, or misalignment, fix `site.css` and rerun the same screenshots.

- [ ] **Step 7: Final consistency check**

Confirm:

```powershell
git diff --check
dotnet test TasteBudz.sln
```

Expected: no whitespace errors; all tests pass.

- [ ] **Step 8: Commit verification fixes**

Only if source files changed during verification:

```powershell
git add src\TasteBudz.Web.Mvc\wwwroot\css\site.css src\TasteBudz.Web.Mvc\Views\Group\Manage.cshtml tests
git commit -m "fix: polish responsive group hub layout"
```

## Plan Self-Review

Spec coverage:

- Owner announcements are covered in Tasks 2, 3, 6, 7, and 8.
- System event announcements are covered in Task 5.
- Preset food wallpaper is covered in Tasks 1, 4, 6, 7, and 8.
- Existing app style and responsive review are covered in Tasks 7 and 9.
- Uploaded wallpaper is documented as out of scope in Task 8.

Placeholder scan:

- The plan contains no `TBD`, `TODO`, or unbounded "handle later" implementation steps.

Type consistency:

- `GroupWallpaperTheme`, `GroupAnnouncementKind`, `GroupAnnouncement`, `GroupAnnouncementDto`, and `CreateGroupAnnouncementRequest` are consistently named across tasks.
