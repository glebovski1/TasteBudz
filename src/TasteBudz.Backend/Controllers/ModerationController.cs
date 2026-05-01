// HTTP endpoints for moderator/admin report and restriction workflows.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Backend.Controllers;

[Authorize(Roles = "Moderator,Admin")]
[ApiController]
[Route("api/v1/moderation")]
/// <summary>
/// Exposes moderator/admin APIs for report review and scoped restrictions.
/// </summary>
public sealed class ModerationController(
    ModerationService moderationService,
    ModerationSearchService moderationSearchService,
    RestrictionService restrictionService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("search")]
    public Task<ModerationSearchResponseDto> Search([FromQuery] ModerationSearchQuery query, CancellationToken cancellationToken) =>
        moderationSearchService.SearchAsync(currentUserAccessor.GetRequiredCurrentUser(), query, cancellationToken);

    [HttpGet("reports")]
    public Task<ListResponse<ModerationReportDto>> ListReports([FromQuery] BrowseModerationReportsQuery query, CancellationToken cancellationToken) =>
        moderationService.ListReportsAsync(query, cancellationToken);

    [HttpGet("reports/{reportId:guid}")]
    public Task<ModerationReportDto> GetReport(Guid reportId, CancellationToken cancellationToken) =>
        moderationService.GetReportAsync(reportId, cancellationToken);

    [HttpGet("reports/{reportId:guid}/review")]
    public Task<ModerationReportReviewDto> GetReportReview(Guid reportId, CancellationToken cancellationToken) =>
        moderationSearchService.GetReportReviewAsync(reportId, cancellationToken);

    [HttpGet("users/{userId:guid}")]
    public Task<ModerationUserDetailDto> GetUserDetail(Guid userId, CancellationToken cancellationToken) =>
        moderationSearchService.GetUserDetailAsync(userId, cancellationToken);

    [HttpPatch("reports/{reportId:guid}")]
    public Task<ModerationReportDto> ResolveReport(Guid reportId, [FromBody] ResolveModerationReportRequest request, CancellationToken cancellationToken) =>
        moderationService.ResolveReportAsync(currentUserAccessor.GetRequiredCurrentUser(), reportId, request, cancellationToken);

    [HttpPost("restrictions")]
    public Task<RestrictionDto> CreateRestriction([FromBody] CreateRestrictionRequest request, CancellationToken cancellationToken) =>
        restrictionService.CreateAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);

    [HttpPost("bans")]
    public Task<UserBanDto> CreateBan([FromBody] CreateUserBanRequest request, CancellationToken cancellationToken) =>
        restrictionService.CreateUserBanAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);

    [HttpPatch("restrictions/{restrictionId:guid}")]
    public Task<RestrictionDto> UpdateRestriction(Guid restrictionId, [FromBody] UpdateRestrictionRequest request, CancellationToken cancellationToken) =>
        restrictionService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser(), restrictionId, request, cancellationToken);
}
