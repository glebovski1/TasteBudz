// HTTP endpoints for recurring and one-off availability windows.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/availability")]
/// <summary>
/// Lets the authenticated user manage their availability preferences.
/// </summary>
public sealed class AvailabilityController(AvailabilityService availabilityService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("recurring")]
    public Task<IReadOnlyCollection<RecurringAvailabilityWindowDto>> ListRecurring(CancellationToken cancellationToken) =>
        availabilityService.ListRecurringAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpPost("recurring")]
    public Task<RecurringAvailabilityWindowDto> CreateRecurring([FromBody] UpsertRecurringAvailabilityWindowRequest request, CancellationToken cancellationToken) =>
        availabilityService.UpsertRecurringAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, null, request, cancellationToken);

    [HttpPatch("recurring/{windowId:guid}")]
    public Task<RecurringAvailabilityWindowDto> UpdateRecurring(Guid windowId, [FromBody] UpsertRecurringAvailabilityWindowRequest request, CancellationToken cancellationToken) =>
        availabilityService.UpsertRecurringAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, windowId, request, cancellationToken);

    [HttpDelete("recurring/{windowId:guid}")]
    public async Task<IActionResult> DeleteRecurring(Guid windowId, CancellationToken cancellationToken)
    {
        await availabilityService.DeleteRecurringAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, windowId, cancellationToken);
        return NoContent();
    }

    [HttpGet("one-off")]
    public Task<IReadOnlyCollection<OneOffAvailabilityWindowDto>> ListOneOff(CancellationToken cancellationToken) =>
        availabilityService.ListOneOffAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpPost("one-off")]
    public Task<OneOffAvailabilityWindowDto> CreateOneOff([FromBody] UpsertOneOffAvailabilityWindowRequest request, CancellationToken cancellationToken) =>
        availabilityService.UpsertOneOffAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, null, request, cancellationToken);

    [HttpPatch("one-off/{windowId:guid}")]
    public Task<OneOffAvailabilityWindowDto> UpdateOneOff(Guid windowId, [FromBody] UpsertOneOffAvailabilityWindowRequest request, CancellationToken cancellationToken) =>
        availabilityService.UpsertOneOffAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, windowId, request, cancellationToken);

    [HttpDelete("one-off/{windowId:guid}")]
    public async Task<IActionResult> DeleteOneOff(Guid windowId, CancellationToken cancellationToken)
    {
        await availabilityService.DeleteOneOffAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, windowId, cancellationToken);
        return NoContent();
    }
}