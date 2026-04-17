using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class AccountAndProfileMvcTests
{
    [Fact]
    public void AuthCookie_UsesEightHourSlidingExpirationAndBackendSessionValidation()
    {
        using var factory = new TasteBudzMvcFactory();
        var optionsMonitor = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var options = optionsMonitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
        Assert.True(options.SlidingExpiration);
        Assert.Equal(typeof(BackendSessionCookieEvents), options.EventsType);
    }

    [Fact]
    public async Task Register_PostsToBackendAndRedirectsToProfileEdit()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Account/CreateAccount");

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/register",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateSession()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(false, new[] { "cuisineTags", "spiceTolerance" })));

        using var response = await client.PostAsync(
            "/Account/CreateAccount",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Username"] = "alex",
                ["Email"] = "alex@example.com",
                ["ZipCode"] = "45220",
                ["Password"] = "Pa$$w0rd123",
                ["ConfirmPassword"] = "Pa$$w0rd123",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Profile/Edit", response.Headers.Location?.ToString());
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task Login_WhenOnboardingComplete_RedirectsToDashboard()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Account/Login");

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/login",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateSession()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));

        using var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["UsernameOrEmail"] = "alex@example.com",
                ["Password"] = "Pa$$w0rd123",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Profile/View", response.Headers.Location?.ToString());
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ProtectedPages_RedirectAnonymousUsersToLogin()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        using var editResponse = await client.GetAsync("/Profile/Edit");
        using var viewResponse = await client.GetAsync("/Profile/View");

        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        Assert.Contains("/Account/Login", editResponse.Headers.Location?.ToString());
        Assert.Equal(HttpStatusCode.Redirect, viewResponse.StatusCode);
        Assert.Contains("/Account/Login", viewResponse.Headers.Location?.ToString());
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ProtectedPages_WhenBackendSessionHasTimedOut_RedirectToLoginWithoutCallingBackend()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Account/Login");

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/login",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateSession()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));

        using var loginResponse = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["UsernameOrEmail"] = "alex@example.com",
                ["Password"] = "Pa$$w0rd123",
            }));

        var authCookie = loginResponse.Headers
            .GetValues("Set-Cookie")
            .Single(header => header.StartsWith(".TasteBudz.Mvc.Auth=", StringComparison.Ordinal))
            .Split(';', 2)[0];

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        using var authOnlyClient = factory.Server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Profile/View");
        request.Headers.Add("Cookie", authCookie);

        using var response = await authOnlyClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
        Assert.Empty(factory.BackendHandler.Requests);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task InvalidLogin_RedisplaysFormWithBackendError()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Account/Login");

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/login",
            (_, _) => StubBackendApiHandler.Problem(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Invalid username/email or password."));

        using var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["UsernameOrEmail"] = "alex@example.com",
                ["Password"] = "bad-password",
            }));

        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid username/email or password.", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ResetPassword_PostsTokenAndNewPasswordToBackendThenRedirectsToLogin()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Account/ResetPassword?token=reset-token");

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/password-reset",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));

        using var response = await client.PostAsync(
            "/Account/ResetPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Token"] = "reset-token",
                ["NewPassword"] = "N3wPa$$w0rd",
                ["ConfirmPassword"] = "N3wPa$$w0rd",
            }));

        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.ToString());
        Assert.Null(request.AuthorizationParameter);
        Assert.Contains("\"token\":\"reset-token\"", request.Body);
        Assert.Contains("\"newPassword\":\"N3wPa$$w0rd\"", request.Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ProfileEdit_PostsProfilePreferencesAndPrivacyThenRedirects()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/preferences/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreatePreferences()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/privacy-settings/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreatePrivacy()));

        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Profile/Edit");

        factory.BackendHandler.Enqueue(
            HttpMethod.Patch,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile(displayName: "Alex Updated")));
        factory.BackendHandler.Enqueue(
            HttpMethod.Put,
            "/api/v1/preferences/me",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                MvcTestHelpers.CreatePreferences(
                    cuisines: new[] { "Italian", "Thai" },
                    dietaryFlags: new[] { "Vegetarian" },
                    allergies: new[] { "Peanuts", "Shellfish" })));
        factory.BackendHandler.Enqueue(
            HttpMethod.Patch,
            "/api/v1/privacy-settings/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new PrivacySettingsDto(false)));

        using var response = await client.PostAsync(
            "/Profile/Edit",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("Username", "alex-updated"),
                new KeyValuePair<string, string>("DisplayName", "Alex Updated"),
                new KeyValuePair<string, string>("Bio", "Updated bio"),
                new KeyValuePair<string, string>("HomeAreaZipCode", "45221"),
                new KeyValuePair<string, string>("SocialGoal", SocialGoal.Networking.ToString()),
                new KeyValuePair<string, string>("SpiceTolerance", SpiceTolerance.Hot.ToString()),
                new KeyValuePair<string, string>("SelectedCuisineTags", "Italian"),
                new KeyValuePair<string, string>("SelectedCuisineTags", "Thai"),
                new KeyValuePair<string, string>("SelectedDietaryFlags", "Vegetarian"),
                new KeyValuePair<string, string>("AllergiesText", "Peanuts, Shellfish"),
                new KeyValuePair<string, string>("DiscoveryEnabled", "false"),
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Profile/View", response.Headers.Location?.ToString());

        var profileRequest = factory.BackendHandler.Requests.Single(request =>
            request.Method == HttpMethod.Patch &&
            request.PathAndQuery == "/api/v1/profiles/me");
        var preferenceRequest = factory.BackendHandler.Requests.Single(request =>
            request.Method == HttpMethod.Put &&
            request.PathAndQuery == "/api/v1/preferences/me");
        var privacyRequest = factory.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/privacy-settings/me" && request.Method == HttpMethod.Patch);

        Assert.Contains("\"username\":\"alex-updated\"", profileRequest.Body);
        Assert.Contains("\"socialGoal\":\"Networking\"", profileRequest.Body);
        Assert.Contains("\"cuisineTags\":[\"Italian\",\"Thai\"]", preferenceRequest.Body);
        Assert.Contains("\"allergies\":[\"Peanuts\",\"Shellfish\"]", preferenceRequest.Body);
        Assert.Contains("\"discoveryEnabled\":false", privacyRequest.Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task DashboardView_RendersBackendSummaryData()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/dashboard",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateDashboard()));

        using var response = await client.GetAsync("/Profile/View");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Friday Sushi Night", html);
        Assert.Contains("Cincy Foodies", html);
        Assert.Contains("Sam Carter", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task Logout_ClearsLocalSessionAndProtectedPagesRedirectAgain()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/dashboard",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateDashboard()));

        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Profile/View");

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/logout",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));

        using var logoutResponse = await client.PostAsync(
            "/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
            }));

        using var protectedResponse = await client.GetAsync("/Profile/View");

        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Contains("/Account/Login", protectedResponse.Headers.Location?.ToString());
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ProtectedRequest_WhenBackendReturnsUnauthorized_RefreshesOnceAndRetriesWithNewToken()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Problem(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Access token expired."));
        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/refresh",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                MvcTestHelpers.CreateSession(
                    accessToken: "refreshed-access-token",
                    refreshToken: "refreshed-refresh-token")));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/dashboard",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateDashboard()));

        using var response = await client.GetAsync("/Profile/View");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var onboardingRequests = factory.BackendHandler.Requests
            .Where(request => request.Method == HttpMethod.Get && request.PathAndQuery == "/api/v1/onboarding/status")
            .ToArray();
        var refreshRequest = factory.BackendHandler.Requests.Single(request =>
            request.Method == HttpMethod.Post && request.PathAndQuery == "/api/v1/auth/refresh");
        var dashboardRequest = factory.BackendHandler.Requests.Single(request =>
            request.Method == HttpMethod.Get && request.PathAndQuery == "/api/v1/me/dashboard");

        Assert.Equal(2, onboardingRequests.Length);
        Assert.Equal("access-token", onboardingRequests[0].AuthorizationParameter);
        Assert.Equal("refresh-token", refreshRequest.Body is null ? null : MvcTestHelpers.ExtractRefreshToken(refreshRequest.Body));
        Assert.Null(refreshRequest.AuthorizationParameter);
        Assert.Equal("refreshed-access-token", onboardingRequests[1].AuthorizationParameter);
        Assert.Equal("refreshed-access-token", dashboardRequest.AuthorizationParameter);
        factory.BackendHandler.AssertDrained();
    }
}
