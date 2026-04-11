using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;

public static partial class MvcTestHelpers
{
    private static readonly Regex RequestVerificationTokenRegex =
        RequestVerificationTokenPattern();

    public static HttpClient CreateClient(TasteBudzMvcFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    public static async Task<string> GetRequestVerificationTokenAsync(
        HttpClient client,
        string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = RequestVerificationTokenRegex.Match(html);

        return match.Success
            ? WebUtility.HtmlDecode(match.Groups["token"].Value)
            : throw new InvalidOperationException($"No antiforgery token was found in the HTML for {path}.");
    }

    public static SessionDto CreateSession(
        string username = "alex",
        string email = "alex@example.com",
        string accessToken = "access-token",
        string refreshToken = "refresh-token",
        IReadOnlyCollection<UserRole>? roles = null) =>
        new(
            accessToken,
            refreshToken,
            DateTimeOffset.UtcNow.AddHours(8),
            new CurrentUserSummaryDto(Guid.NewGuid(), username, email, roles ?? new[] { UserRole.User }));

    public static ProfileDto CreateProfile(
        string username = "alex",
        string displayName = "Alex Carter",
        string email = "alex@example.com",
        string zipCode = "45220",
        SocialGoal? socialGoal = SocialGoal.Friends) =>
        new(Guid.NewGuid(), username, email, displayName, "Always down for noodles.", zipCode, socialGoal);

    public static PreferenceDto CreatePreferences(
        IReadOnlyCollection<string>? cuisines = null,
        SpiceTolerance? spiceTolerance = SpiceTolerance.Medium,
        IReadOnlyCollection<string>? dietaryFlags = null,
        IReadOnlyCollection<string>? allergies = null) =>
        new(
            cuisines ?? new[] { "Sushi", "Thai" },
            spiceTolerance,
            dietaryFlags ?? new[] { "Vegetarian" },
            allergies ?? new[] { "Peanuts" });

    public static PrivacySettingsDto CreatePrivacy(bool discoveryEnabled = true) =>
        new(discoveryEnabled);

    public static DashboardDto CreateDashboard() =>
        new(
            CreateProfile(),
            new[]
            {
                new DashboardEventSummaryDto(Guid.NewGuid(), "Friday Sushi Night", EventStatus.Confirmed, new DateTimeOffset(2026, 3, 20, 19, 0, 0, TimeSpan.Zero)),
            },
            new[]
            {
                new DashboardGroupSummaryDto(Guid.NewGuid(), "Cincy Foodies", GroupVisibility.Public),
            },
            new[]
            {
                new DashboardBudSummaryDto(Guid.NewGuid(), "sam", "Sam Carter"),
            });

    public static async Task<SessionDto> LoginThroughUiAsync(
        HttpClient client,
        TasteBudzMvcFactory factory,
        bool isOnboardingComplete,
        IReadOnlyCollection<UserRole>? roles = null)
    {
        var token = await GetRequestVerificationTokenAsync(client, "/Account/Login");
        var session = CreateSession(roles: roles);
        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/login",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, session));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(
                    isOnboardingComplete,
                    isOnboardingComplete ? Array.Empty<string>() : new[] { "cuisineTags" })));

        using var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["UsernameOrEmail"] = "alex@example.com",
                ["Password"] = "Pa$$w0rd123",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return session;
    }

    public static string ExtractRefreshToken(string requestBody)
    {
        var match = Regex.Match(requestBody, "\"refreshToken\":\"(?<value>[^\"]+)\"");
        return match.Success
            ? match.Groups["value"].Value
            : throw new InvalidOperationException("Refresh token payload was not found in the request body.");
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\".*?value=\"(?<token>[^\"]+)\"", RegexOptions.Singleline)]
    private static partial Regex RequestVerificationTokenPattern();
}
