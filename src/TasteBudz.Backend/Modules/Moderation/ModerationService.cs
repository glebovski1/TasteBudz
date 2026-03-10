// Report submission and moderation queue workflows.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// Owns user report submission plus moderator/admin report review flows.
/// </summary>
public sealed class ModerationService(
    IModerationRepository moderationRepository,
    IAuthRepository authRepository,
    AuditLogService auditLogService,
    IClock clock)
{
    public async Task<ModerationReportDto> SubmitReportAsync(CurrentUser currentUser, CreateModerationReportRequest request, CancellationToken cancellationToken = default)
    {
        var targetType = request.TargetType ?? throw ApiException.BadRequest("targetType is required.");
        var targetId = request.TargetId ?? throw ApiException.BadRequest("targetId is required.");
        var category = NormalizeRequired(request.Category, "category");
        var reason = NormalizeRequired(request.Reason, "reason");
        var explanation = NormalizeOptional(request.Explanation);

        if (targetType == ReportTargetType.User)
        {
            _ = await authRepository.GetByIdAsync(targetId, cancellationToken)
                ?? throw ApiException.NotFound("The reported user could not be found.");
        }

        var now = clock.UtcNow;
        var report = new ModerationReport(
            Guid.NewGuid(),
            currentUser.UserId,
            targetType,
            targetId,
            category,
            reason,
            explanation,
            request.RelatedEventId,
            request.RelatedUserId,
            request.RelatedMessageId,
            now,
            ModerationReportStatus.Pending,
            null,
            null,
            null,
            null);

        await moderationRepository.SaveReportAsync(report, cancellationToken);
        return ToDto(report);
    }

    public async Task<ListResponse<ModerationReportDto>> ListReportsAsync(BrowseModerationReportsQuery query, CancellationToken cancellationToken = default)
    {
        var reports = await moderationRepository.ListReportsAsync(cancellationToken);
        var filtered = reports
            .Where(report => !query.Status.HasValue || report.Status == query.Status.Value)
            .Select(ToDto)
            .ToArray();
        var items = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new ListResponse<ModerationReportDto>(items, filtered.Length);
    }

    public async Task<ModerationReportDto> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await moderationRepository.GetReportAsync(reportId, cancellationToken)
            ?? throw ApiException.NotFound("The requested report could not be found.");

        return ToDto(report);
    }

    public async Task<ModerationReportDto> ResolveReportAsync(CurrentUser actor, Guid reportId, ResolveModerationReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = await moderationRepository.GetReportAsync(reportId, cancellationToken)
            ?? throw ApiException.NotFound("The requested report could not be found.");

        if (report.Status == ModerationReportStatus.Resolved)
        {
            throw ApiException.Conflict("The requested report has already been resolved.");
        }

        var now = clock.UtcNow;
        var resolved = report with
        {
            Status = ModerationReportStatus.Resolved,
            ResolvedByUserId = actor.UserId,
            ResolvedAtUtc = now,
            ResolutionDecision = NormalizeRequired(request.Decision, "decision"),
            ResolutionNotes = NormalizeOptional(request.Notes),
        };

        await moderationRepository.SaveReportAsync(resolved, cancellationToken);
        await moderationRepository.SaveActionAsync(
            new ModerationAction(Guid.NewGuid(), actor.UserId, resolved.Id, ModerationActionType.ReportResolved, resolved.ResolutionNotes ?? resolved.ResolutionDecision!, now),
            cancellationToken);
        await auditLogService.WriteAsync(
            new AuditLogEntry(Guid.NewGuid(), "ReportResolved", actor.UserId, nameof(ModerationReport), resolved.Id, now, resolved.ResolutionDecision!),
            cancellationToken);

        return ToDto(resolved);
    }

    private static ModerationReportDto ToDto(ModerationReport report) =>
        new(
            report.Id,
            report.ReporterUserId,
            report.TargetType,
            report.TargetId,
            report.Category,
            report.Reason,
            report.Explanation,
            report.RelatedEventId,
            report.RelatedUserId,
            report.RelatedMessageId,
            report.CreatedAtUtc,
            report.Status,
            report.ResolvedByUserId,
            report.ResolvedAtUtc,
            report.ResolutionDecision,
            report.ResolutionNotes);

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw ApiException.BadRequest($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
