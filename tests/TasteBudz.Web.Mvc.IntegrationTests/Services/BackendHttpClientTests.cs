using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class BackendHttpClientTests
{
    [Fact]
    public async Task GetAsync_WhenRefreshFailsAfterAnotherRequestAlreadyUpdatedSession_RetriesWithNewStoredToken()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();

        var refreshedSession = MvcTestHelpers.CreateSession(
            accessToken: "refreshed-access-token",
            refreshToken: "refreshed-refresh-token");

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Problem(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Access token expired."));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/refresh",
            (_, _) =>
            {
                // Simulate another concurrent request successfully refreshing before this request handles the failure.
                context.SignInAsync(refreshedSession).GetAwaiter().GetResult();

                return StubBackendApiHandler.Problem(
                    HttpStatusCode.Unauthorized,
                    "Unauthorized",
                    "Refresh token already rotated.");
            });
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));

        var backendHttpClient = context.CreateBackendHttpClient();
        var result = await backendHttpClient.GetAsync<OnboardingStatusDto>("/api/v1/onboarding/status");
        var storedSession = context.GetStoredSession();
        var requests = context.BackendHandler.Requests.ToArray();

        Assert.True(result.IsComplete);
        Assert.NotNull(storedSession);
        Assert.Equal("refreshed-access-token", storedSession.AccessToken);
        Assert.Equal("refreshed-refresh-token", storedSession.RefreshToken);
        Assert.Equal(2, requests.Count(request => request.Method == HttpMethod.Get));
        Assert.Equal("access-token", requests[0].AuthorizationParameter);
        Assert.Equal("refreshed-access-token", requests[2].AuthorizationParameter);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, context.LastSignInScheme);
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task PostAndDeleteAsync_SupportResponseAndNoContentVariants()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();

        var eventId = Guid.NewGuid();
        var blockedUserId = Guid.NewGuid();
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventParticipantDto(
                    Guid.NewGuid(),
                    "alex",
                    "Alex Carter",
                    EventParticipantState.Joined,
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/cancellation",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Delete,
            $"/api/v1/blocks/{blockedUserId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));

        var backendHttpClient = context.CreateBackendHttpClient();
        var participant = await backendHttpClient.PostAsync<EventParticipantDto>(
            $"/api/v1/events/{eventId}/participants");
        await backendHttpClient.PostAsync(
            $"/api/v1/events/{eventId}/cancellation",
            new CancelEventRequest { Reason = "Restaurant closed." });
        await backendHttpClient.DeleteAsync($"/api/v1/blocks/{blockedUserId}");

        Assert.Equal(EventParticipantState.Joined, participant.State);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        Assert.Contains(
            "\"reason\":\"Restaurant closed.\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/cancellation").Body);
        context.BackendHandler.AssertDrained();
    }
}
