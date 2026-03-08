// Request and response contracts for profile, onboarding, availability, privacy, blocking, and dashboard endpoints.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Reports whether the required onboarding fields have been filled in.
/// </summary>
public sealed record OnboardingStatusDto(bool IsComplete, IReadOnlyCollection<string> MissingRequiredFields);

/// <summary>
/// Public profile view returned by profile endpoints.
/// </summary>
public sealed record ProfileDto(
    Guid UserId,
    string Username,
    string Email,
    string DisplayName,
    string? Bio,
    string HomeAreaZipCode,
    SocialGoal? SocialGoal);

/// <summary>
/// Partial update payload for the current user's profile.
/// </summary>
public sealed class UpdateMyProfileRequest
{
    [MinLength(3)]
    [MaxLength(32)]
    public string? Username { get; init; }

    [MinLength(1)]
    [MaxLength(64)]
    public string? DisplayName { get; init; }

    [MaxLength(500)]
    public string? Bio { get; init; }

    [RegularExpression("^[0-9]{5}$")]
    public string? HomeAreaZipCode { get; init; }

    public SocialGoal? SocialGoal { get; init; }
}

/// <summary>
/// Preference view returned by the preferences endpoint.
/// </summary>
public sealed record PreferenceDto(
    IReadOnlyCollection<string> CuisineTags,
    SpiceTolerance? SpiceTolerance,
    IReadOnlyCollection<string> DietaryFlags,
    IReadOnlyCollection<string> Allergies);

/// <summary>
/// Full replacement payload for preference data.
/// </summary>
public sealed class ReplacePreferencesRequest
{
    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();

    public SpiceTolerance? SpiceTolerance { get; init; }

    public IReadOnlyCollection<string> DietaryFlags { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Allergies { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Recurring weekly availability window returned by the API.
/// </summary>
public sealed record RecurringAvailabilityWindowDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Label);

/// <summary>
/// Create/update payload for recurring weekly availability.
/// </summary>
public sealed class UpsertRecurringAvailabilityWindowRequest
{
    [Required]
    public DayOfWeek? DayOfWeek { get; init; }

    [Required]
    public TimeOnly? StartTime { get; init; }

    [Required]
    public TimeOnly? EndTime { get; init; }

    [MaxLength(100)]
    public string? Label { get; init; }
}

/// <summary>
/// One-off availability window returned by the API.
/// </summary>
public sealed record OneOffAvailabilityWindowDto(
    Guid Id,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Label);

/// <summary>
/// Create/update payload for one-off date-range availability.
/// </summary>
public sealed class UpsertOneOffAvailabilityWindowRequest
{
    [Required]
    public DateTimeOffset? StartsAtUtc { get; init; }

    [Required]
    public DateTimeOffset? EndsAtUtc { get; init; }

    [MaxLength(100)]
    public string? Label { get; init; }
}

/// <summary>
/// Privacy view exposed to the current user.
/// </summary>
public sealed record PrivacySettingsDto(bool DiscoveryEnabled);

/// <summary>
/// Partial update payload for privacy settings.
/// </summary>
public sealed class UpdatePrivacySettingsRequest
{
    [Required]
    public bool? DiscoveryEnabled { get; init; }
}

/// <summary>
/// Block list item returned by the safety endpoints.
/// </summary>
public sealed record BlockedUserDto(
    Guid UserId,
    string Username,
    string DisplayName,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Request body for blocking another user.
/// </summary>
public sealed class CreateBlockRequest
{
    [Required]
    public Guid? BlockedUserId { get; init; }
}

/// <summary>
/// Small event card shown on the current user's dashboard.
/// </summary>
public sealed record DashboardEventSummaryDto(
    Guid EventId,
    string? Title,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc);

/// <summary>
/// Small group card shown on the current user's dashboard.
/// </summary>
public sealed record DashboardGroupSummaryDto(
    Guid GroupId,
    string Name,
    GroupVisibility Visibility);

/// <summary>
/// Small Bud connection card shown on the current user's dashboard.
/// </summary>
public sealed record DashboardBudSummaryDto(
    Guid UserId,
    string Username,
    string DisplayName);

/// <summary>
/// Combined dashboard payload.
/// </summary>
public sealed record DashboardDto(
    ProfileDto Profile,
    IReadOnlyCollection<DashboardEventSummaryDto> ActiveEvents,
    IReadOnlyCollection<DashboardGroupSummaryDto> ActiveGroups,
    IReadOnlyCollection<DashboardBudSummaryDto> Budz);

/// <summary>
/// Closed-event invite summary shown to the current user.
/// </summary>
public sealed record EventInviteDto(
    Guid EventId,
    string? Title,
    EventType EventType,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset? InvitedAtUtc);