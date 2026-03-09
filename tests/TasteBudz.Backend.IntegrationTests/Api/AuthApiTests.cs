// Integration tests for the auth endpoints and the custom bearer authentication pipeline.
using System.Net;
using TasteBudz.Backend.IntegrationTests.Shared;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Verifies that a registered session can immediately access protected endpoints.
/// </summary>
public sealed class AuthApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task Register_ThenAccessProtectedEndpoint_Succeeds()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client);
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var response = await client.GetAsync("/api/v1/onboarding/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_DoesNotAcceptQueryStringAccessToken()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client);
        var response = await client.GetAsync($"/api/v1/onboarding/status?access_token={session.AccessToken}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
