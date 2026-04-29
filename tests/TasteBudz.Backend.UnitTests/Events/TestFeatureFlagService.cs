// Unit tests for event browse filtering and visibility behavior.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Events;


internal sealed class TestFeatureFlagService(bool restaurantOperationsEnabled, bool discountsEnabled) : IFeatureFlagService
{
    public bool IsMessagingDirectChatEnabled() => false;

    public bool IsMessagingGroupChatEnabled() => true;

    public bool IsNotificationsPushEnabled() => false;

    public bool IsRestaurantsOperationsEnabled() => restaurantOperationsEnabled;

    public bool IsRestaurantsSlotsEnabled() => restaurantOperationsEnabled;

    public bool IsRestaurantsDiscountsEnabled() => discountsEnabled;

    public bool IsPaymentsCheckoutEnabled() => false;

    public bool IsDiscoveryExperimentalSuggestionsEnabled() => false;
}
