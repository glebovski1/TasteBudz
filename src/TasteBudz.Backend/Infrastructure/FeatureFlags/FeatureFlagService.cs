// Thin adapter over FeatureFlagOptions so callers depend on an interface instead of raw configuration.
using Microsoft.Extensions.Options;

namespace TasteBudz.Backend.Infrastructure.FeatureFlags;

/// <summary>
/// Reads the current feature-flag values from the configured options snapshot.
/// </summary>
public sealed class FeatureFlagService(IOptions<FeatureFlagOptions> options) : IFeatureFlagService
{
    private readonly FeatureFlagOptions featureFlags = options.Value;

    public bool IsMessagingDirectChatEnabled() => featureFlags.MessagingDirectChatEnabled;

    public bool IsMessagingGroupChatEnabled() => featureFlags.MessagingGroupChatEnabled;

    public bool IsNotificationsPushEnabled() => featureFlags.NotificationsPushEnabled;

    public bool IsRestaurantsOperationsEnabled() => featureFlags.RestaurantsOperationsEnabled;

    public bool IsRestaurantsSlotsEnabled() => featureFlags.RestaurantsSlotsEnabled;

    public bool IsRestaurantsDiscountsEnabled() => featureFlags.RestaurantsDiscountsEnabled;

    public bool IsDiscoveryExperimentalSuggestionsEnabled() => featureFlags.DiscoveryExperimentalSuggestionsEnabled;
}