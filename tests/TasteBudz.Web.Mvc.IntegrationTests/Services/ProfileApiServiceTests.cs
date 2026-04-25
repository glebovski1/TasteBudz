using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class ProfileApiServiceTests
{
    [Fact]
    public async Task CurrentUserAndProfileEndpoints_SendExpectedRoutesAndPayloads()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new ProfileApiService(client));
        var inviteTime = new DateTimeOffset(2026, 3, 20, 18, 0, 0, TimeSpan.Zero);

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile()));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/preferences/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreatePreferences()));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/privacy-settings/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreatePrivacy()));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/dashboard",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateDashboard()));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/events",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new DashboardEventSummaryDto(Guid.NewGuid(), "Friday Sushi Night", EventType.Open, EventStatus.Confirmed, inviteTime, "Sushi"),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/groups",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new DashboardGroupSummaryDto(Guid.NewGuid(), "Cincy Foodies", "Dinner club", GroupVisibility.Public, 3),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/me/event-invites",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventInviteDto(Guid.NewGuid(), "Closed Dinner", EventType.Closed, inviteTime, inviteTime.AddDays(-1)),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            "/api/v1/profiles/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreateProfile(displayName: "Alex Updated")));
        context.BackendHandler.Enqueue(
            HttpMethod.Put,
            "/api/v1/preferences/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, MvcTestHelpers.CreatePreferences(
                cuisines: new[] { "Thai" },
                dietaryFlags: new[] { "Vegetarian" },
                allergies: new[] { "Peanuts" })));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            "/api/v1/privacy-settings/me",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new PrivacySettingsDto(false)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/account/deletion",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));

        var onboardingStatus = await service.GetOnboardingStatusAsync();
        var profile = await service.GetMyProfileAsync();
        var preferences = await service.GetMyPreferencesAsync();
        var privacy = await service.GetMyPrivacySettingsAsync();
        var dashboard = await service.GetDashboardAsync();
        var events = await service.ListMyEventsAsync();
        var groups = await service.ListMyGroupsAsync();
        var invites = await service.ListMyEventInvitesAsync();
        await service.UpdateMyProfileAsync(new UpdateMyProfileRequest
        {
            DisplayName = "Alex Updated",
            HomeAreaZipCode = "45221",
            SocialGoal = SocialGoal.Networking,
        });
        await service.ReplaceMyPreferencesAsync(new ReplacePreferencesRequest
        {
            CuisineTags = new[] { "Thai" },
            DietaryFlags = new[] { "Vegetarian" },
            Allergies = new[] { "Peanuts" },
        });
        await service.UpdateMyPrivacySettingsAsync(new UpdatePrivacySettingsRequest
        {
            DiscoveryEnabled = false,
        });
        await service.RequestAccountDeletionAsync();

        Assert.True(onboardingStatus.IsComplete);
        Assert.Equal("alex", profile.Username);
        Assert.Equal(2, preferences.CuisineTags.Count);
        Assert.True(privacy.DiscoveryEnabled);
        Assert.Single(dashboard.MyEvents);
        Assert.Single(events);
        Assert.Single(groups);
        Assert.Single(invites);

        var updateProfileRequest = context.BackendHandler.Requests.Single(request =>
            request.Method == HttpMethod.Patch && request.PathAndQuery == "/api/v1/profiles/me");
        var updatePreferencesRequest = context.BackendHandler.Requests.Single(request =>
            request.Method == HttpMethod.Put && request.PathAndQuery == "/api/v1/preferences/me");
        var updatePrivacyRequest = context.BackendHandler.Requests.Single(request =>
            request.Method == HttpMethod.Patch && request.PathAndQuery == "/api/v1/privacy-settings/me");

        Assert.Contains("\"displayName\":\"Alex Updated\"", updateProfileRequest.Body);
        Assert.Contains("\"socialGoal\":\"Networking\"", updateProfileRequest.Body);
        Assert.Contains("\"cuisineTags\":[\"Thai\"]", updatePreferencesRequest.Body);
        Assert.Contains("\"discoveryEnabled\":false", updatePrivacyRequest.Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task AvailabilityAndBlocks_SendExpectedRoutesPayloadsAndDeletes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new ProfileApiService(client));
        var recurringId = Guid.NewGuid();
        var oneOffId = Guid.NewGuid();
        var blockedUserId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/recurring",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RecurringAvailabilityWindowDto(recurringId, DayOfWeek.Friday, new TimeOnly(18, 0), new TimeOnly(21, 0), "Dinner"),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/availability/recurring",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RecurringAvailabilityWindowDto(recurringId, DayOfWeek.Friday, new TimeOnly(18, 0), new TimeOnly(21, 0), "Dinner")));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/availability/recurring/{recurringId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RecurringAvailabilityWindowDto(recurringId, DayOfWeek.Friday, new TimeOnly(19, 0), new TimeOnly(21, 0), "Dinner")));
        context.BackendHandler.Enqueue(
            HttpMethod.Delete,
            $"/api/v1/availability/recurring/{recurringId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/availability/one-off",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new OneOffAvailabilityWindowDto(oneOffId, new DateTimeOffset(2026, 3, 21, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 21, 20, 0, 0, TimeSpan.Zero), "Saturday"),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/availability/one-off",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OneOffAvailabilityWindowDto(oneOffId, new DateTimeOffset(2026, 3, 21, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 21, 20, 0, 0, TimeSpan.Zero), "Saturday")));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/availability/one-off/{oneOffId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OneOffAvailabilityWindowDto(oneOffId, new DateTimeOffset(2026, 3, 21, 19, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 21, 21, 0, 0, TimeSpan.Zero), "Saturday")));
        context.BackendHandler.Enqueue(
            HttpMethod.Delete,
            $"/api/v1/availability/one-off/{oneOffId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/blocks",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new BlockedUserDto(blockedUserId, "blocked-user", "Blocked User", DateTimeOffset.UtcNow),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/blocks",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new BlockedUserDto(blockedUserId, "blocked-user", "Blocked User", DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Delete,
            $"/api/v1/blocks/{blockedUserId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));

        var recurring = await service.ListRecurringAvailabilityAsync();
        await service.CreateRecurringAvailabilityAsync(new UpsertRecurringAvailabilityWindowRequest
        {
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(21, 0),
            Label = "Dinner",
        });
        await service.UpdateRecurringAvailabilityAsync(recurringId, new UpsertRecurringAvailabilityWindowRequest
        {
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(19, 0),
            EndTime = new TimeOnly(21, 0),
            Label = "Dinner",
        });
        await service.DeleteRecurringAvailabilityAsync(recurringId);

        var oneOff = await service.ListOneOffAvailabilityAsync();
        await service.CreateOneOffAvailabilityAsync(new UpsertOneOffAvailabilityWindowRequest
        {
            StartsAtUtc = new DateTimeOffset(2026, 3, 21, 18, 0, 0, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(2026, 3, 21, 20, 0, 0, TimeSpan.Zero),
            Label = "Saturday",
        });
        await service.UpdateOneOffAvailabilityAsync(oneOffId, new UpsertOneOffAvailabilityWindowRequest
        {
            StartsAtUtc = new DateTimeOffset(2026, 3, 21, 19, 0, 0, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(2026, 3, 21, 21, 0, 0, TimeSpan.Zero),
            Label = "Saturday",
        });
        await service.DeleteOneOffAvailabilityAsync(oneOffId);

        var blocks = await service.ListBlocksAsync();
        await service.CreateBlockAsync(new CreateBlockRequest { BlockedUserId = blockedUserId });
        await service.RemoveBlockAsync(blockedUserId);

        Assert.Single(recurring);
        Assert.Single(oneOff);
        Assert.Single(blocks);
        Assert.Contains(
            "\"dayOfWeek\":\"Friday\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/availability/recurring" && request.Method == HttpMethod.Post).Body);
        Assert.Contains(
            "\"blockedUserId\":\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/blocks" && request.Method == HttpMethod.Post).Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
