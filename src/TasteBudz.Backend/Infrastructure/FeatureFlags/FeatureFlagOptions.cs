// Configuration object that mirrors the FeatureFlags section in appsettings.
namespace TasteBudz.Backend.Infrastructure.FeatureFlags;

/// <summary>
/// Keeps future or deferred features explicit without enabling them in code by accident.
/// </summary>
public sealed class FeatureFlagOptions
{
    public bool MessagingDirectChatEnabled { get; init; }

    public bool MessagingGroupChatEnabled { get; init; }

    public bool NotificationsPushEnabled { get; init; }

    public bool RestaurantsOperationsEnabled { get; init; }

    public bool RestaurantsSlotsEnabled { get; init; }

    public bool RestaurantsDiscountsEnabled { get; init; }

    public bool DiscoveryExperimentalSuggestionsEnabled { get; init; }
}