using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Media;


internal sealed class DenyingEventFeedbackAccessService : IEventFeedbackAccessService
{
    public Task<bool> CanViewFeedbackAsync(CurrentUser currentUser, Guid eventId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<EventFeedbackPhoto?> GetFeedbackPhotoByMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default) =>
        Task.FromResult<EventFeedbackPhoto?>(null);
}
