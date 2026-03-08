// Application-facing interface for checking whether deferred features are enabled.
namespace TasteBudz.Backend.Infrastructure.FeatureFlags;

/// <summary>
/// Provides named feature toggles without leaking configuration details into callers.
/// </summary>
public interface IFeatureFlagService
{
    bool IsMessagingDirectChatEnabled();

    bool IsMessagingGroupChatEnabled();

    bool IsNotificationsPushEnabled();

    bool IsRestaurantsOperationsEnabled();

    bool IsRestaurantsSlotsEnabled();

    bool IsRestaurantsDiscountsEnabled();

    bool IsDiscoveryExperimentalSuggestionsEnabled();
}