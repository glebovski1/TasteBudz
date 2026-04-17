// Integration tests for the auth endpoints and the custom bearer authentication pipeline.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Domain;
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

    [Fact]
    public async Task AdminPasswordResetToken_AllowsUserToSetNewPasswordAndRevokesOldSession()
    {
        factory.ResetState();
        using var adminClient = factory.CreateClient();
        using var userClient = factory.CreateClient();

        var adminSession = await ApiTestHelpers.RegisterAsync(adminClient, username: "admin", email: "admin@example.com");
        var userSession = await ApiTestHelpers.RegisterAsync(userClient, username: "alex", email: "alex@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, adminSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Admin });
        ApiTestHelpers.SetBearer(adminClient, adminSession.AccessToken);
        ApiTestHelpers.SetBearer(userClient, userSession.AccessToken);

        var tokenResponse = await adminClient.PostAsJsonAsync("/api/v1/admin/users/password-reset-tokens", new CreatePasswordResetTokenRequest
        {
            UsernameOrEmail = "alex@example.com",
        });
        var token = await tokenResponse.Content.ReadFromJsonAsync<PasswordResetTokenDto>(ApiTestHelpers.JsonOptions);

        var resetResponse = await userClient.PostAsJsonAsync("/api/v1/auth/password-reset", new ResetPasswordRequest
        {
            Token = token!.ResetToken,
            NewPassword = "N3wPa$$w0rd",
        });
        var oldSessionResponse = await userClient.GetAsync("/api/v1/onboarding/status");
        var loginResponse = await userClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "alex",
            Password = "N3wPa$$w0rd",
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<SessionDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        Assert.Equal(userSession.CurrentUser.UserId, token.UserId);
        Assert.False(string.IsNullOrWhiteSpace(token.ResetToken));
        Assert.StartsWith("/Account/ResetPassword?token=", token.ResetUrl, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSessionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Equal(userSession.CurrentUser.UserId, login!.CurrentUser.UserId);
    }

    [Fact]
    public async Task PasswordResetTokenCreation_WhenCallerIsNotAdmin_ReturnsForbidden()
    {
        factory.ResetState();
        using var callerClient = factory.CreateClient();
        using var userClient = factory.CreateClient();

        var callerSession = await ApiTestHelpers.RegisterAsync(callerClient, username: "caller", email: "caller@example.com");
        await ApiTestHelpers.RegisterAsync(userClient, username: "alex", email: "alex@example.com");
        ApiTestHelpers.SetBearer(callerClient, callerSession.AccessToken);

        var response = await callerClient.PostAsJsonAsync("/api/v1/admin/users/password-reset-tokens", new CreatePasswordResetTokenRequest
        {
            UsernameOrEmail = "alex",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
