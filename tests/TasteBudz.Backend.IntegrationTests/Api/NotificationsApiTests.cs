// Integration tests for the notification-center API.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Notifications;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Verifies that in-app notifications are readable and markable through HTTP.
/// </summary>
public sealed class NotificationsApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task NotificationsEndpoints_ListAndMarkRead()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Private Invite Group",
            Visibility = GroupVisibility.Private,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);

        await ownerClient.PostAsJsonAsync($"/api/v1/groups/{group!.GroupId}/invites", new InviteUserToGroupRequest
        {
            Username = "guest",
        });

        var listResponse = await guestClient.GetAsync("/api/v1/notifications");
        var notifications = await listResponse.Content.ReadFromJsonAsync<NotificationDto[]>(ApiTestHelpers.JsonOptions);
        var notification = Assert.Single(notifications!);

        var updateResponse = await guestClient.PatchAsJsonAsync($"/api/v1/notifications/{notification.NotificationId}", new UpdateNotificationRequest
        {
            Read = true,
        });
        var updated = await updateResponse.Content.ReadFromJsonAsync<NotificationDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(NotificationType.GroupInviteReceived, notification.NotificationType);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated!.ReadAtUtc);
    }

    [Fact]
    public async Task NotificationsEndpoint_ExposesEventWorkflowNotifications()
    {
        factory.ResetState();
        using var hostClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var hostSession = await ApiTestHelpers.RegisterAsync(hostClient, username: "host", email: "host@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(hostClient, hostSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createEventResponse = await hostClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Notification event",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });
        var eventDetail = await createEventResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/events/{eventDetail!.EventId}/participants", null);

        var response = await hostClient.GetAsync("/api/v1/notifications");
        var notifications = await response.Content.ReadFromJsonAsync<NotificationDto[]>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(notifications!, notification => notification.NotificationType == NotificationType.EventJoined);
    }
}
