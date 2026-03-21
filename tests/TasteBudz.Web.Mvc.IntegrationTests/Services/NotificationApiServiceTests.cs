using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class NotificationApiServiceTests
{
    [Fact]
    public async Task ListAndUpdate_SendExpectedRoutesAndPayloads()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new NotificationApiService(client));
        var notificationId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/notifications",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new NotificationDto(notificationId, NotificationType.EventUpdated, "Event", Guid.NewGuid(), "Event updated", DateTimeOffset.UtcNow, null),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/notifications/{notificationId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new NotificationDto(notificationId, NotificationType.EventUpdated, "Event", Guid.NewGuid(), "Event updated", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));

        var notifications = await service.ListAsync();
        var updated = await service.UpdateAsync(notificationId, new UpdateNotificationRequest
        {
            Read = true,
        });

        Assert.Single(notifications);
        Assert.NotNull(updated.ReadAtUtc);
        Assert.Contains(
            "\"read\":true",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/notifications/{notificationId}").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
