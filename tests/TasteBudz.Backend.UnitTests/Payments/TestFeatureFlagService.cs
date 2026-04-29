using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Payments;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Payments;


internal sealed class TestFeatureFlagService(bool checkoutEnabled, bool discountsEnabled) : IFeatureFlagService
{
    public bool IsMessagingDirectChatEnabled() => false;

    public bool IsMessagingGroupChatEnabled() => true;

    public bool IsNotificationsPushEnabled() => false;

    public bool IsRestaurantsOperationsEnabled() => true;

    public bool IsRestaurantsSlotsEnabled() => true;

    public bool IsRestaurantsDiscountsEnabled() => discountsEnabled;

    public bool IsPaymentsCheckoutEnabled() => checkoutEnabled;

    public bool IsDiscoveryExperimentalSuggestionsEnabled() => false;
}
