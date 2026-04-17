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
    [HttpPost("password-reset-tokens")]
    public Task<PasswordResetTokenDto> CreatePasswordResetToken(
        [FromBody] CreatePasswordResetTokenRequest request,
        CancellationToken cancellationToken) =>
        authService.CreatePasswordResetTokenAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
}
