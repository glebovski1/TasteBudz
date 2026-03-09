// Integration tests for the auth endpoints and the custom bearer authentication pipeline.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Profiles;

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
        var onboarding = await response.Content.ReadFromJsonAsync<OnboardingStatusDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(onboarding!.IsComplete);
        Assert.Contains("socialGoal", onboarding.MissingRequiredFields);
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

    [Fact]
    public async Task LoginRefreshAndLogout_RotateSessionAndRevokeProtectedAccess()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        await ApiTestHelpers.RegisterAsync(client, username: "alex", email: "alex@example.com");

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "alex@example.com",
            Password = "Pa$$w0rd123",
        });
        var loginSession = await loginResponse.Content.ReadFromJsonAsync<SessionDto>(ApiTestHelpers.JsonOptions);

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshSessionRequest
        {
            RefreshToken = loginSession!.RefreshToken,
        });
        var refreshedSession = await refreshResponse.Content.ReadFromJsonAsync<SessionDto>(ApiTestHelpers.JsonOptions);

        ApiTestHelpers.SetBearer(client, refreshedSession!.AccessToken);
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
        var protectedResponse = await client.GetAsync("/api/v1/onboarding/status");

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotEqual(loginSession.RefreshToken, refreshedSession.RefreshToken);
        Assert.NotEqual(loginSession.AccessToken, refreshedSession.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task AccountDeletion_RevokesCurrentSessionAndPreventsFutureLogin()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "delete-me", email: "delete-me@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var deletionResponse = await client.PostAsync("/api/v1/account/deletion", null);
        var protectedResponse = await client.GetAsync("/api/v1/onboarding/status");
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "delete-me",
            Password = "Pa$$w0rd123",
        });

        Assert.Equal(HttpStatusCode.NoContent, deletionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }
}
