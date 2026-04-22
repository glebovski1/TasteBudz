using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class AdminMvcTests
{
    [Fact]
    public async Task AdminIndex_RendersOpenPasswordResetRequests()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var requestId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        EnqueueAdminIndexShell(factory, new[]
        {
            new PasswordResetRequestDto(
                requestId,
                "alex",
                "I lost access to my old email.",
                Guid.NewGuid(),
                "alex",
                new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                null,
                null),
        });

        using var response = await client.GetAsync("/Admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Password Reset", html);
        Assert.Contains("alex", html);
        Assert.Contains("I lost access to my old email.", html);
        Assert.Contains("Generate Reset Link", html);
        Assert.Contains("Dismiss", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task GeneratePasswordResetToken_FromRequest_PostsRequestIdAndRendersResetLink()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        EnqueueAdminIndexShell(factory, new[]
        {
            new PasswordResetRequestDto(
                requestId,
                "alex",
                "Please reset my password.",
                userId,
                "alex",
                new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                null,
                null),
        });
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Admin");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/admin/users/password-reset-tokens",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new PasswordResetTokenDto(
                    userId,
                    "alex",
                    "reset-token",
                    "/Account/ResetPassword?token=reset-token",
                    new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero))));
        EnqueueAdminIndexShell(factory, Array.Empty<PasswordResetRequestDto>());

        using var response = await client.PostAsync(
            "/Admin/GeneratePasswordResetToken",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["usernameOrEmail"] = "alex",
                ["passwordResetRequestId"] = requestId.ToString(),
            }));
        var html = await response.Content.ReadAsStringAsync();
        var request = factory.BackendHandler.Requests.Single(recorded =>
            recorded.Method == HttpMethod.Post &&
            recorded.PathAndQuery == "/api/v1/admin/users/password-reset-tokens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/Account/ResetPassword?token=reset-token", html);
        Assert.Contains("\"usernameOrEmail\":\"alex\"", request.Body);
        Assert.Contains($"\"passwordResetRequestId\":\"{requestId}", request.Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task DismissPasswordResetRequest_PostsClosureAndRedirectsToAdminIndex()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var requestId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        EnqueueAdminIndexShell(factory, new[]
        {
            new PasswordResetRequestDto(
                requestId,
                "alex",
                "Please close this request.",
                Guid.NewGuid(),
                "alex",
                new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                null,
                null),
        });
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Admin");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/admin/users/password-reset-requests/{requestId}/closure",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new PasswordResetRequestDto(
                    requestId,
                    "alex",
                    "Please close this request.",
                    Guid.NewGuid(),
                    "alex",
                    new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 13, 0, 0, TimeSpan.Zero),
                    Guid.NewGuid())));

        using var response = await client.PostAsync(
            "/Admin/DismissPasswordResetRequest",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["requestId"] = requestId.ToString(),
            }));

        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Admin", response.Headers.Location?.ToString());
        Assert.Equal($"/api/v1/admin/users/password-reset-requests/{requestId}/closure", request.PathAndQuery);
        Assert.Null(request.Body);
        factory.BackendHandler.AssertDrained();
    }

    private static void EnqueueAdminIndexShell(
        TasteBudzMvcFactory factory,
        IReadOnlyCollection<PasswordResetRequestDto> openRequests)
    {
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/reports?status=Pending&page=1&pageSize=50",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<ModerationReportDto>(Array.Empty<ModerationReportDto>(), 0)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=1&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(Array.Empty<RestaurantDto>(), 0)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/users/password-reset-requests",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, openRequests));
    }
}
