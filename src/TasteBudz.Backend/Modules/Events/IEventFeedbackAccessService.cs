using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Exposes event-feedback authorization to supporting modules such as Media.
/// </summary>
public interface IEventFeedbackAccessService
{
    Task<bool> CanViewFeedbackAsync(CurrentUser currentUser, Guid eventId, CancellationToken cancellationToken = default);

    Task<EventFeedbackPhoto?> GetFeedbackPhotoByMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
}
