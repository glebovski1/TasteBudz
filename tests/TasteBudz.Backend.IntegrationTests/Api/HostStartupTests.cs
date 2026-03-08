// Basic integration smoke tests for host startup and routing.
using System.Net;
using TasteBudz.Backend.IntegrationTests.Shared;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Confirms the test host boots and still returns the framework-standard 404 for unknown routes.
/// </summary>
public sealed class HostStartupTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task MissingRoute_Returns404()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}