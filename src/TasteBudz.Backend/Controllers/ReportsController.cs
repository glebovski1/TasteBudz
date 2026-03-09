// HTTP endpoint for end-user report submission.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reports")]
/// <summary>
/// Lets authenticated users submit moderation reports.
/// </summary>
public sealed class ReportsController(
    ModerationService moderationService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost]
    public Task<ModerationReportDto> Create([FromBody] CreateModerationReportRequest request, CancellationToken cancellationToken) =>
        moderationService.SubmitReportAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
}
