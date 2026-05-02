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
    public async Task LoginAndResetPasswordPages_RenderPasswordResetRequestEntryPoint()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        using var loginResponse = await client.GetAsync("/Account/Login");
        using var resetResponse = await client.GetAsync("/Account/ResetPassword?token=reset-token");
        var loginHtml = await loginResponse.Content.ReadAsStringAsync();
        var resetHtml = await resetResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Contains("Need a password reset?", loginHtml);
        Assert.Contains("/Account/RequestPasswordReset", loginHtml);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Contains("Ask the Admin Team", resetHtml);
        Assert.Contains("/Account/RequestPasswordReset", resetHtml);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task RequestPasswordReset_PostsAnonymousPayloadAndRedirectsToLogin()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Account/RequestPasswordReset");

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/password-reset-requests",
            (_, _) => new HttpResponseMessage(HttpStatusCode.Accepted));

        using var response = await client.PostAsync(
            "/Account/RequestPasswordReset",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Username"] = "alex",
                ["Message"] = "I cannot sign in and need help.",
            }));

        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.ToString());
        Assert.Null(request.AuthorizationParameter);
        Assert.Contains("\"username\":\"alex\"", request.Body);
        Assert.Contains("\"message\":\"I cannot sign in and need help.\"", request.Body);
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
    public async Task Availability_CreateRecurringPostsToBackendAndRedirects()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/recurring",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RecurringAvailabilityWindowDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/one-off",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<OneOffAvailabilityWindowDto>()));
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Profile/Availability");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/availability/recurring",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RecurringAvailabilityWindowDto(Guid.NewGuid(), DayOfWeek.Friday, new TimeOnly(18, 0), new TimeOnly(20, 30), "Weeknight dinner")));

        using var response = await client.PostAsync(
            "/Profile/CreateRecurringAvailability",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["DayOfWeek"] = DayOfWeek.Friday.ToString(),
                ["StartTime"] = "18:00",
                ["EndTime"] = "20:30",
                ["Label"] = "Weeknight dinner",
            }));

        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Profile/Availability", response.Headers.Location?.ToString());
        Assert.Contains("\"dayOfWeek\":\"Friday\"", request.Body);
        Assert.Contains("\"startTime\":\"18:00:00\"", request.Body);
        Assert.Contains("\"endTime\":\"20:30:00\"", request.Body);
        Assert.Contains("\"label\":\"Weeknight dinner\"", request.Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task Availability_UpdateOneOffPostsToBackendAndRedirects()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var windowId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/recurring",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RecurringAvailabilityWindowDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/one-off",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new OneOffAvailabilityWindowDto(windowId, new DateTimeOffset(2026, 5, 2, 15, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 2, 17, 0, 0, TimeSpan.Zero), "Saturday brunch"),
                }));
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Profile/Availability");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/availability/one-off/{windowId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OneOffAvailabilityWindowDto(windowId, new DateTimeOffset(2026, 5, 3, 16, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 3, 18, 0, 0, TimeSpan.Zero), "Sunday dinner")));

        using var response = await client.PostAsync(
            "/Profile/UpdateOneOffAvailability",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["WindowId"] = windowId.ToString(),
                ["StartsAt"] = "2026-05-03T16:00",
                ["EndsAt"] = "2026-05-03T18:00",
                ["Label"] = "Sunday dinner",
            }));

        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Profile/Availability", response.Headers.Location?.ToString());
        Assert.Contains("\"startsAtUtc\":\"2026-05-03T16:00:00", request.Body);
        Assert.Contains("\"endsAtUtc\":\"2026-05-03T18:00:00", request.Body);
        Assert.Contains("\"label\":\"Sunday dinner\"", request.Body);
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
        Assert.Contains("Upcoming Events", html);
        Assert.Contains("Future events you are hosting or joined to, sorted by date.", html);
        Assert.Contains("My Events", html);
        Assert.Contains("data-profile-section=\"upcoming-events\"", html);
        Assert.Contains("Group events", html);
        Assert.Contains("Standalone events", html);
        Assert.DoesNotContain("Ordinary event", html);
        Assert.Contains("Active/Open", html);
        Assert.Contains("Full", html);
        Assert.Contains("Completed", html);
        Assert.Contains("Cancelled", html);
        Assert.Contains("Friday Sushi Night", html);
        Assert.Contains("Cincy Foodies", html);
        Assert.Contains("Sam Carter", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task DashboardView_UpcomingEventsOnlyShowsFutureJoinedEvents()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var now = DateTimeOffset.UtcNow;
        var dashboard = new DashboardDto(
            MvcTestHelpers.CreateProfile(),
            new[]
            {
                new DashboardEventSummaryDto(Guid.NewGuid(), "Joined future dinner", EventType.Open, EventStatus.Open, now.AddDays(2), "Sushi", IsJoined: true),
                new DashboardEventSummaryDto(Guid.NewGuid(), "Future invite only", EventType.Closed, EventStatus.Open, now.AddDays(3), "Ramen", IsInvited: true),
                new DashboardEventSummaryDto(Guid.NewGuid(), "Group linked not joined", EventType.Open, EventStatus.Open, now.AddDays(4), "Tacos", Guid.NewGuid(), IsGroupLinked: true),
                new DashboardEventSummaryDto(Guid.NewGuid(), "Past joined dinner", EventType.Open, EventStatus.Open, now.AddDays(-1), "Pizza", IsJoined: true),
                new DashboardEventSummaryDto(Guid.NewGuid(), "Completed joined dinner", EventType.Open, EventStatus.Completed, now.AddDays(5), "Thai", IsJoined: true),
            },
            Array.Empty<DashboardGroupSummaryDto>(),
            Array.Empty<DashboardBudSummaryDto>());

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
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, dashboard));

        using var response = await client.GetAsync("/Profile/View");
        var html = await response.Content.ReadAsStringAsync();
        var upcomingSection = ExtractSection(html, "<div id=\"upcoming-events\"", "<div id=\"friends\"");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Joined future dinner", upcomingSection);
        Assert.DoesNotContain("Future invite only", upcomingSection);
        Assert.DoesNotContain("Group linked not joined", upcomingSection);
        Assert.DoesNotContain("Past joined dinner", upcomingSection);
        Assert.DoesNotContain("Completed joined dinner", upcomingSection);
        Assert.Contains("Future invite only", html);
        Assert.Contains("Group linked not joined", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task DashboardView_RendersAvatarThroughMvcMediaProxy()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var mediaAssetId = Guid.NewGuid();
        var dashboard = new DashboardDto(
            MvcTestHelpers.CreateProfile(),
            Array.Empty<DashboardEventSummaryDto>(),
            Array.Empty<DashboardGroupSummaryDto>(),
            new[]
            {
                new DashboardBudSummaryDto(Guid.NewGuid(), "sam", "Sam Carter", null, SocialGoal.Friends, "45220", mediaAssetId, Array.Empty<string>(), Array.Empty<string>(), DateTimeOffset.UtcNow),
            });

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
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, dashboard));

        using var response = await client.GetAsync("/Profile/View");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"/media/{mediaAssetId}", html);
        Assert.DoesNotContain($"/api/v1/media/{mediaAssetId}", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task MediaProxy_UsesBackendBearerTokenAndStreamsBytes()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var mediaAssetId = Guid.NewGuid();
        var bytes = new byte[] { 1, 2, 3 };

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/media/{mediaAssetId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") },
                },
            });

        using var response = await client.GetAsync($"/media/{mediaAssetId}");
        var actualBytes = await response.Content.ReadAsByteArrayAsync();
        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(bytes, actualBytes);
        Assert.Equal("Bearer", request.AuthorizationScheme);
        Assert.Equal("access-token", request.AuthorizationParameter);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task DashboardStyles_HideFilteredMyEventCards()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        using var response = await client.GetAsync("/css/site.css");
        var css = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("[data-my-event-card][hidden]", css);
        Assert.Contains("display: none !important;", css);
        factory.BackendHandler.AssertDrained();
    }

    private static string ExtractSection(string html, string startMarker, string endMarker)
    {
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected start marker '{startMarker}' in HTML.");

        var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, $"Expected end marker '{endMarker}' after '{startMarker}' in HTML.");

        return html[start..end];
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
