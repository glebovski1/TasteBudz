namespace TasteBudz.Web.Mvc.Services.Backend.Contracts;

public sealed record OnboardingStatusDto(bool IsComplete, IReadOnlyCollection<string> MissingRequiredFields);

public sealed record ProfileDto(
    Guid UserId,
    string Username,
    string Email,
    string DisplayName,
    string? Bio,
    string HomeAreaZipCode,
    SocialGoal? SocialGoal);

public sealed class UpdateMyProfileRequest
{
    public string? Username { get; init; }

    public string? DisplayName { get; init; }

    public string? Bio { get; init; }

    public string? HomeAreaZipCode { get; init; }

    public SocialGoal? SocialGoal { get; init; }
}

public sealed record PreferenceDto(
    IReadOnlyCollection<string> CuisineTags,
    SpiceTolerance? SpiceTolerance,
    IReadOnlyCollection<string> DietaryFlags,
    IReadOnlyCollection<string> Allergies);

public sealed class ReplacePreferencesRequest
{
    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();

    public SpiceTolerance? SpiceTolerance { get; init; }

    public IReadOnlyCollection<string> DietaryFlags { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Allergies { get; init; } = Array.Empty<string>();
}

public sealed record PrivacySettingsDto(bool DiscoveryEnabled);

public sealed class UpdatePrivacySettingsRequest
{
    public bool? DiscoveryEnabled { get; init; }
}

public sealed record DashboardEventSummaryDto(
    Guid EventId,
    string? Title,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc);

public sealed record DashboardGroupSummaryDto(
    Guid GroupId,
    string Name,
    GroupVisibility Visibility);

public sealed record DashboardBudSummaryDto(
    Guid UserId,
    string Username,
    string DisplayName);

public sealed record DashboardDto(
    ProfileDto Profile,
    IReadOnlyCollection<DashboardEventSummaryDto> ActiveEvents,
    IReadOnlyCollection<DashboardGroupSummaryDto> ActiveGroups,
    IReadOnlyCollection<DashboardBudSummaryDto> Budz);
