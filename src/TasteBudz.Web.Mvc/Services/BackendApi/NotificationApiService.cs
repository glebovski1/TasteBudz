using TasteBudz.Backend.Modules.Notifications;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over the notification-center endpoints.
/// </summary>
public sealed class NotificationApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public NotificationApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<IReadOnlyCollection<NotificationDto>> ListAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<NotificationDto>>("/api/v1/notifications", cancellationToken);

    public Task<NotificationDto> UpdateAsync(
        Guid notificationId,
        UpdateNotificationRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateNotificationRequest, NotificationDto>(
            $"/api/v1/notifications/{notificationId}",
            request,
            cancellationToken);
}
