// Unit tests for scoped messaging access and restriction rules.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Messaging;


internal sealed class AlwaysOnFeatureFlagService : IFeatureFlagService
{
    public bool IsMessagingDirectChatEnabled() => true;

    public bool IsMessagingGroupChatEnabled() => true;

    public bool IsNotificationsPushEnabled() => false;

    public bool IsRestaurantsOperationsEnabled() => false;

    public bool IsRestaurantsSlotsEnabled() => false;

    public bool IsRestaurantsDiscountsEnabled() => false;

    public bool IsPaymentsCheckoutEnabled() => false;

    public bool IsDiscoveryExperimentalSuggestionsEnabled() => false;
}
