// HTTP endpoint for onboarding completeness checks.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/onboarding")]
/// <summary>
/// Reports the authenticated user's onboarding status.
/// </summary>
public sealed class OnboardingController(OnboardingService onboardingService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("status")]
    public Task<OnboardingStatusDto> GetStatus(CancellationToken cancellationToken) =>
        onboardingService.GetStatusAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);
}