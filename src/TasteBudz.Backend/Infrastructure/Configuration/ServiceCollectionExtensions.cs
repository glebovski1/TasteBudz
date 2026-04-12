// Collects the app's DI registration so Program.cs stays focused on host wiring.
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Payments;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Infrastructure.Configuration;

/// <summary>
/// Registers the backend's shared infrastructure, persistence layer, and module services.
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
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        services.AddDbContext<TasteBudzDbContext>((serviceProvider, options) =>
        {
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var persistenceOptions = configuration
                .GetSection(PersistenceOptions.SectionName)
                .Get<PersistenceOptions>() ?? new PersistenceOptions();
            var provider = PersistenceProviderNames.Normalize(persistenceOptions.Provider);
            var connectionString = configuration.GetConnectionString("TasteBudz")
                ?? throw new InvalidOperationException("ConnectionStrings:TasteBudz must be configured.");

            if (string.Equals(provider, PersistenceProviderNames.SqlServer, StringComparison.Ordinal))
            {
                options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());
                return;
            }

            options.UseSqlite(SqliteConnectionStringHelper.Normalize(connectionString, environment.ContentRootPath));
        });

        services.AddSingleton<IClock, TasteBudz.Backend.Infrastructure.Time.SystemClock>();
        services.AddSingleton<IKeyedLockProvider, InMemoryKeyedLockProvider>();

        // Auth/session infrastructure is shared by multiple modules.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenGenerator, SecureTokenGenerator>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
        services.AddSingleton<IPersistenceExceptionClassifier, RelationalPersistenceExceptionClassifier>();
        services.AddScoped<IPersistenceTransactionRunner, SqliteTransactionRunner>();

        services.AddScoped<IAuthRepository, SqliteAuthRepository>();
        services.AddScoped<IProfileRepository, SqliteProfileRepository>();
        services.AddScoped<IRestaurantRepository, SqliteRestaurantRepository>();
        services.AddScoped<IRestaurantOperationsRepository, SqliteRestaurantOperationsRepository>();
        services.AddScoped<IEventRepository, SqliteEventRepository>();
        services.AddScoped<IGroupRepository, SqliteGroupRepository>();
        services.AddScoped<IDiscoveryRepository, SqliteDiscoveryRepository>();
        services.AddScoped<IMessagingRepository, SqliteMessagingRepository>();
        services.AddScoped<ICheckoutSessionRepository, SqliteCheckoutSessionRepository>();
        services.AddScoped<IMediaRepository, SqliteMediaRepository>();
        services.AddScoped<IModerationRepository, SqliteModerationRepository>();
        services.AddScoped<INotificationRepository, SqliteNotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

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
        services.AddScoped<UserGroupQueryService>();
        services.AddScoped<ModerationService>();
        services.AddScoped<RestrictionService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<DiscoveryService>();
        services.AddScoped<NotificationCenterService>();
        services.AddScoped<MessagingService>();
        services.AddScoped<CheckoutSessionService>();
        services.AddScoped<MediaService>();
        services.AddScoped<RestaurantSearchService>();
        services.AddScoped<RestaurantRecommendationService>();
        services.AddScoped<RestaurantAdminAssignmentService>();
        services.AddScoped<ManagedRestaurantService>();
        services.AddScoped<RestaurantSlotService>();
        services.AddScoped<DiscountEligibilityService>();
        services.AddScoped<EventSlotReservationService>();
        services.AddScoped<OverpassRestaurantImporter>();
        services.AddHttpClient("Overpass", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "TasteBudz/1.0 (restaurant import; contact@tastebudz.local)");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<EventLifecycleService>();
        services.AddScoped<EventBrowseService>();
        services.AddScoped<EventInviteService>();
        services.AddScoped<EventParticipationService>();
        services.AddScoped<EventService>();
        services.AddScoped<UserEventQueryService>();
        services.AddSignalR();

        // The app uses a custom bearer handler backed by the session repository instead of JWT validation.
        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationDefaults.Scheme, _ => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RestaurantAdmin", policy => policy.RequireRole(nameof(UserRole.RestaurantAdmin)));
        });

        return services;
    }
}
