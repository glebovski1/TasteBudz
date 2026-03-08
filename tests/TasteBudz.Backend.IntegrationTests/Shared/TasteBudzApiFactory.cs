// Shared WebApplicationFactory configuration for backend integration tests.
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.IntegrationTests.Shared;

/// <summary>
/// Boots the real API host in an integration-testing environment and exposes a reset hook for shared in-memory state.
/// </summary>
public sealed class TasteBudzApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    public void ResetState()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<InMemoryTasteBudzStore>().Reset();
    }
}