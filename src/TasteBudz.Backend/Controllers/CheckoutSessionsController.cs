using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Payments;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
public sealed class CheckoutSessionsController(
    CheckoutSessionService checkoutSessionService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost("api/v1/events/{eventId:guid}/checkout-sessions")]
    public Task<CheckoutSessionDto> CreateForEvent(Guid eventId, CancellationToken cancellationToken) =>
        checkoutSessionService.CreateForEventAsync(currentUserAccessor.GetRequiredCurrentUser(), eventId, cancellationToken);

    [HttpPost("api/v1/checkout-sessions/{checkoutSessionId:guid}/completion")]
    public Task<CheckoutSessionDto> Complete(Guid checkoutSessionId, CancellationToken cancellationToken) =>
        checkoutSessionService.CompleteAsync(currentUserAccessor.GetRequiredCurrentUser(), checkoutSessionId, cancellationToken);

    [HttpPost("api/v1/checkout-sessions/{checkoutSessionId:guid}/cancellation")]
    public Task<CheckoutSessionDto> Cancel(Guid checkoutSessionId, CancellationToken cancellationToken) =>
        checkoutSessionService.CancelAsync(currentUserAccessor.GetRequiredCurrentUser(), checkoutSessionId, cancellationToken);
}
