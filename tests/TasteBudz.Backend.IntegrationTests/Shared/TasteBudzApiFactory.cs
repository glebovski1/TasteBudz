// Shared WebApplicationFactory configuration for backend integration tests.
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.IntegrationTests.Shared;

/// <summary>
/// Boots the real API host in an integration-testing environment and exposes a reset hook for shared in-memory state.
/// </summary>
public sealed class TasteBudzApiFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> configurationOverrides;

    public TasteBudzApiFactory()
        : this(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase))
    {
    }

    private TasteBudzApiFactory(IReadOnlyDictionary<string, string?> configurationOverrides)
    {
        this.configurationOverrides = configurationOverrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            if (configurationOverrides.Count > 0)
            {
                configurationBuilder.AddInMemoryCollection(configurationOverrides);
            }
        });
    }

    public void ResetState()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<InMemoryTasteBudzStore>().Reset();
    }

    public TasteBudzApiFactory WithConfigurationOverrides(IReadOnlyDictionary<string, string?> overrides)
    {
        var merged = new Dictionary<string, string?>(configurationOverrides, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in overrides)
        {
            merged[pair.Key] = pair.Value;
        }

        return new TasteBudzApiFactory(merged);
    }
}
