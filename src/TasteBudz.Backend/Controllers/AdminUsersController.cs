using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController(
    AuthService authService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("password-reset-requests")]
    public Task<IReadOnlyCollection<PasswordResetRequestDto>> ListOpenPasswordResetRequests(CancellationToken cancellationToken) =>
        authService.ListOpenPasswordResetRequestsAsync(currentUserAccessor.GetRequiredCurrentUser(), cancellationToken);

    [HttpPost("password-reset-requests/{requestId:guid}/closure")]
    public Task<PasswordResetRequestDto> ClosePasswordResetRequest(Guid requestId, CancellationToken cancellationToken) =>
        authService.ClosePasswordResetRequestAsync(currentUserAccessor.GetRequiredCurrentUser(), requestId, cancellationToken);

    [HttpPost("password-reset-tokens")]
    public Task<PasswordResetTokenDto> CreatePasswordResetToken(
        [FromBody] CreatePasswordResetTokenRequest request,
        CancellationToken cancellationToken) =>
        authService.CreatePasswordResetTokenAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
}
