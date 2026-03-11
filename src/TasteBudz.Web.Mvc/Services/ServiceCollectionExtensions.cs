using Microsoft.Extensions.Options;
using TasteBudz.Web.Mvc.Options;
using TasteBudz.Web.Mvc.Services.Backend;
using TasteBudz.Web.Mvc.Services.Session;

namespace TasteBudz.Web.Mvc.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTasteBudzMvcFrontend(this IServiceCollection services)
    {
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddScoped<BackendApiRequestExecutor>();
        services.AddScoped<AuthApiClient>();
        services.AddScoped<OnboardingApiClient>();
        services.AddScoped<ProfileApiClient>();
        services.AddScoped<PreferenceApiClient>();
        services.AddScoped<PrivacyApiClient>();
        services.AddScoped<DashboardApiClient>();

        services.AddHttpClient("BackendApi", ConfigureHttpClient);
        services.AddHttpClient("BackendApiAuth", ConfigureHttpClient);

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    }
}
