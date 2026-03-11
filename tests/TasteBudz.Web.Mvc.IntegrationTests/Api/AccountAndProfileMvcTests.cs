using System.Net;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services.Backend;
using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class AccountAndProfileMvcTests
{
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
}
