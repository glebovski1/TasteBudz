// Collects the app's DI registration so Program.cs stays focused on host wiring.
using Microsoft.AspNetCore.Authentication;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Infrastructure.Configuration;

/// <summary>
/// Registers the backend's shared infrastructure, in-memory repositories, and module services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Wires the MVP foundation used by all currently implemented modules.
    /// </summary>
    public static IServiceCollection AddTasteBudzFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<FeatureFlagOptions>(configuration.GetSection("FeatureFlags"));

        // These singletons are process-wide for the current in-memory MVP implementation.
        services.AddSingleton<IClock, TasteBudz.Backend.Infrastructure.Time.SystemClock>();
        services.AddSingleton<IKeyedLockProvider, InMemoryKeyedLockProvider>();
        services.AddSingleton<InMemoryTasteBudzStore>();

        // Auth/session infrastructure is shared by multiple modules.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenGenerator, SecureTokenGenerator>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        // Repository interfaces preserve the persistence boundary even though runtime storage is in memory.
        services.AddSingleton<IAuthRepository, InMemoryAuthRepository>();
        services.AddSingleton<IProfileRepository, InMemoryProfileRepository>();
        services.AddSingleton<IRestaurantRepository, InMemoryRestaurantRepository>();
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();
        services.AddSingleton<IGroupRepository, InMemoryGroupRepository>();
        services.AddSingleton<IDiscoveryRepository, InMemoryDiscoveryRepository>();
        services.AddSingleton<IMessagingRepository, InMemoryMessagingRepository>();
        services.AddSingleton<IModerationRepository, InMemoryModerationRepository>();
        services.AddSingleton<INotificationService, InMemoryNotificationService>();

        // Business rules live in scoped services so each request gets a clean workflow instance.
        services.AddScoped<AuthService>();
        services.AddScoped<OnboardingService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<PreferenceService>();
        services.AddScoped<AvailabilityService>();
        services.AddScoped<PrivacyService>();
        services.AddScoped<BlockingService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<GroupService>();
        services.AddScoped<ModerationService>();
        services.AddScoped<RestrictionService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<DiscoveryService>();
        services.AddScoped<NotificationCenterService>();
        services.AddScoped<MessagingService>();
        services.AddScoped<RestaurantSearchService>();
        services.AddScoped<RestaurantRecommendationService>();
        services.AddScoped<EventLifecycleService>();
        services.AddScoped<EventBrowseService>();
        services.AddScoped<EventInviteService>();
        services.AddScoped<EventParticipationService>();
        services.AddScoped<EventService>();
        services.AddSignalR();

        // The app uses a custom bearer handler backed by the session repository instead of JWT validation.
        services
            .AddAuthentication(SessionAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationDefaults.Scheme, _ => { });

        services.AddAuthorization();

        return services;
    }
}
