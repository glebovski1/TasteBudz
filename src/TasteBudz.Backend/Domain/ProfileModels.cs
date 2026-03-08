// Profile, preference, availability, and blocking records.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Public-facing user profile data.
/// </summary>
public sealed record UserProfile(
    Guid UserId,
    string DisplayName,
    string? Bio,
    string HomeAreaZipCode,
    SocialGoal? SocialGoal,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Preference data used for onboarding, browsing, and recommendations.
/// </summary>
public sealed record UserPreferences(
    Guid UserId,
    IReadOnlyCollection<string> CuisineTags,
    SpiceTolerance? SpiceTolerance,
    IReadOnlyCollection<string> DietaryFlags,
    IReadOnlyCollection<string> Allergies,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Repeating weekly availability window.
/// </summary>
public sealed record RecurringAvailabilityWindow(
    Guid Id,
    Guid UserId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Label,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Single date-range availability override.
/// </summary>
public sealed record OneOffAvailabilityWindow(
    Guid Id,
    Guid UserId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Label,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Per-user privacy settings that affect discovery visibility.
/// </summary>
public sealed record PrivacySettings(
    Guid UserId,
    bool DiscoveryEnabled,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Safety record preventing interaction between two users.
/// </summary>
public sealed record UserBlock(
    Guid BlockerUserId,
    Guid BlockedUserId,
    DateTimeOffset CreatedAtUtc);