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

    [HttpPost("{userId:guid}/deletion")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        await authService.DeleteAccountAsAdminAsync(currentUserAccessor.GetRequiredCurrentUser(), userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/permanent-deletion")]
    public async Task<IActionResult> PermanentlyDeleteUser(
        Guid userId,
        [FromBody] PermanentlyDeleteUserRequest request,
        CancellationToken cancellationToken)
    {
        await authService.PermanentlyDeleteAccountAsAdminAsync(currentUserAccessor.GetRequiredCurrentUser(), userId, request, cancellationToken);
        return NoContent();
    }
}
