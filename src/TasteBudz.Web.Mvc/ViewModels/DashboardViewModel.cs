using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Profiles;
using System.Globalization;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class DashboardViewModel
{
    public required string Username { get; init; }

    public required string DisplayName { get; init; }

    public required string Email { get; init; }

    public string? Bio { get; init; }

    public required string HomeAreaZipCode { get; init; }

    public SocialGoal? SocialGoal { get; init; }

    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> DietaryFlags { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<DashboardEventCardViewModel> MyEvents { get; init; } = Array.Empty<DashboardEventCardViewModel>();

    public IReadOnlyCollection<DashboardEventCardViewModel> UpcomingEvents =>
        MyEvents
            .Where(item => item.IsUpcoming)
            .OrderBy(item => item.EventStartAtUtc)
            .ToArray();

    public IReadOnlyCollection<DashboardGroupCardViewModel> ActiveGroups { get; init; } = Array.Empty<DashboardGroupCardViewModel>();

    public IReadOnlyCollection<DashboardBudCardViewModel> Budz { get; init; } = Array.Empty<DashboardBudCardViewModel>();

    public IReadOnlyList<string> PublicFoodTags => DashboardCardFormatting.BuildPublicFoodTags(CuisineTags, DietaryFlags);

    public string PersonalityText => DashboardCardFormatting.GetPersonalityText(Bio);

    public string GoalsText => DashboardCardFormatting.GetSocialGoalDescription(SocialGoal);

    public static DashboardViewModel FromDto(DashboardDto dto)
    {
        var myEvents = dto.MyEvents
            .Select(item => new DashboardEventCardViewModel(
                item.EventId,
                item.Title ?? "Untitled Event",
                item.EventType,
                item.Status,
                item.EventStartAtUtc,
                item.CuisineTarget,
                item.GroupId,
                item.IsHosted,
                item.IsJoined,
                item.IsInvited,
                item.IsGroupLinked))
            .OrderBy(item => item.EventStartAtUtc)
            .ToArray();

        return new()
        {
            Username = dto.Profile.Username,
            DisplayName = dto.Profile.DisplayName,
            Email = dto.Profile.Email,
            Bio = dto.Profile.Bio,
            HomeAreaZipCode = dto.Profile.HomeAreaZipCode,
            SocialGoal = dto.Profile.SocialGoal,
            CuisineTags = dto.Profile.CuisineTags,
            DietaryFlags = dto.Profile.DietaryFlags,
            MyEvents = myEvents,
            ActiveGroups = dto.ActiveGroups
                .Select(item => new DashboardGroupCardViewModel(item.GroupId, item.Name, item.Description, item.Visibility, item.WallpaperTheme, item.ActiveMemberCount))
                .ToArray(),
            Budz = dto.Budz
                .Select(item => new DashboardBudCardViewModel(item.UserId, item.Username, item.DisplayName, item.Bio, item.SocialGoal, item.HomeAreaZipCode, item.AvatarMediaAssetId, item.CuisineTags, item.DietaryFlags, item.ConnectedAtUtc))
                .ToArray(),
        };
    }
}

public sealed record DashboardEventCardViewModel(
    Guid EventId,
    string Title,
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    string? CuisineTarget,
    Guid? GroupId,
    bool IsHosted,
    bool IsJoined,
    bool IsInvited,
    bool IsGroupLinked)
{
    public string MonthLabel => EventStartAtUtc.ToLocalTime().ToString("MMM", CultureInfo.InvariantCulture).ToUpperInvariant();

    public string DayLabel => EventStartAtUtc.ToLocalTime().ToString("%d", CultureInfo.InvariantCulture);

    public string ScheduleLabel => EventStartAtUtc.ToLocalTime().ToString("ddd, MMM d", CultureInfo.InvariantCulture);

    public string TimeLabel => EventStartAtUtc.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture);

    public string TimingChipLabel => DashboardCardFormatting.GetRelativeDayLabel(EventStartAtUtc);

    public string EventTypeLabel => EventType == EventType.Open ? "Open event" : "Invite event";

    public bool HasGroupScope => IsGroupLinked || GroupId.HasValue;

    public string ScopeLabel => HasGroupScope ? "Group event" : string.Empty;

    public string ScopeFilterValue => HasGroupScope ? "group" : "ordinary";

    public string StatusFilterValue => Status.ToString().ToLowerInvariant();

    public bool IsUpcoming =>
        EventStartAtUtc >= DateTimeOffset.UtcNow &&
        Status is not EventStatus.Cancelled and not EventStatus.Completed;

    public IReadOnlyList<string> RelationshipLabels
    {
        get
        {
            var labels = new List<string>();

            if (IsHosted)
            {
                labels.Add("Hosted");
            }

            if (IsJoined)
            {
                labels.Add("Joined");
            }

            if (IsInvited)
            {
                labels.Add("Invited");
            }

            if (HasGroupScope)
            {
                labels.Add("Group event");
            }

            return labels.Count == 0 ? Array.Empty<string>() : labels;
        }
    }

    public string TimingFactLabel => $"{ScheduleLabel} at {TimeLabel}";

    public string SummaryText =>
        string.IsNullOrWhiteSpace(CuisineTarget)
            ? $"Starts {TimingFactLabel}"
            : $"Cuisine focus: {CuisineTarget}";
}

public sealed record DashboardGroupCardViewModel(
    Guid GroupId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupWallpaperTheme WallpaperTheme,
    int ActiveMemberCount)
{
    public string Initial => DashboardCardFormatting.GetInitial(Name);

    public string VisibilityLabel => Visibility == GroupVisibility.Public ? "Public" : "Private";

    public string MembershipLabel => ActiveMemberCount == 1 ? "1 member" : $"{ActiveMemberCount} members";

    public string AccessLabel => Visibility == GroupVisibility.Public ? "Open to direct joins" : "Invite-only membership";

    public string MembershipHint => "You're in";

    public string VisibilitySummary => Visibility == GroupVisibility.Public ? "Open group" : "Invite only";

    public string WallpaperCssClass => $"group-wallpaper--{WallpaperTheme.ToString().ToLowerInvariant()}";

    public string SummaryText =>
        string.IsNullOrWhiteSpace(Description)
            ? AccessLabel
            : DashboardCardFormatting.Truncate(Description, 120);
}

public sealed record DashboardBudCardViewModel(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Bio,
    SocialGoal? SocialGoal,
    string? HomeAreaZipCode,
    Guid? AvatarMediaAssetId,
    IReadOnlyCollection<string> CuisineTags,
    IReadOnlyCollection<string> DietaryFlags,
    DateTimeOffset ConnectedAtUtc)
{
    public string Initial => DashboardCardFormatting.GetInitial(DisplayName);

    public string? AvatarUrl => DashboardCardFormatting.ToMediaUrl(AvatarMediaAssetId);

    public string? GoalLabel => DashboardCardFormatting.GetSocialGoalLabel(SocialGoal);

    public string? ZipLabel => DashboardCardFormatting.GetZipLabel(HomeAreaZipCode);

    public string ConnectedChipLabel => $"Since {ConnectedAtUtc.ToLocalTime().ToString("MMM d", CultureInfo.InvariantCulture)}";

    public string ConnectedLabel => $"Connected {ConnectedAtUtc.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";

    public IReadOnlyList<string> PublicFoodTags => DashboardCardFormatting.BuildPublicFoodTags(CuisineTags, DietaryFlags);

    public IReadOnlyList<string> PreviewFoodTags => PublicFoodTags.Take(6).ToArray();

    public int HiddenFoodTagCount => Math.Max(0, PublicFoodTags.Count - PreviewFoodTags.Count);

    public string PersonalityText => DashboardCardFormatting.GetPersonalityText(Bio);

    public string GoalsText => DashboardCardFormatting.GetSocialGoalDescription(SocialGoal);
}
