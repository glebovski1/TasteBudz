using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class NotificationsMvcTests
{
    [Fact]
    public async Task NotificationsPage_RendersVisibleFailureMessageForInlineMarkRead()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var notificationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/notifications",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new NotificationDto(
                        notificationId,
                        NotificationType.EventInviteReceived,
                        "Event",
                        eventId,
                        "You were invited to Friday ramen.",
                        DateTimeOffset.UtcNow,
                        null),
                }));

        using var response = await client.GetAsync("/Notifications");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("notificationActionStatus", html);
        Assert.Contains("Could not mark this notification as read.", html);
        Assert.DoesNotContain("catch { /* silent */ }", html);
        factory.BackendHandler.AssertDrained();
    }
}
