using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over end-user reporting plus moderator/admin moderation endpoints.
/// </summary>
public sealed class ModerationApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public ModerationApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<ModerationReportDto> CreateReportAsync(
        CreateModerationReportRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateModerationReportRequest, ModerationReportDto>(
            "/api/v1/reports",
            request,
            cancellationToken: cancellationToken);

    public Task<ListResponse<ModerationReportDto>> ListReportsAsync(
        BrowseModerationReportsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<ModerationReportDto>>(
            BuildListReportsPath(query ?? new BrowseModerationReportsQuery()),
            cancellationToken);

    public Task<ModerationReportDto> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ModerationReportDto>($"/api/v1/moderation/reports/{reportId}", cancellationToken);

    public Task<ModerationReportDto> ResolveReportAsync(
        Guid reportId,
        ResolveModerationReportRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<ResolveModerationReportRequest, ModerationReportDto>(
            $"/api/v1/moderation/reports/{reportId}",
            request,
            cancellationToken);

    public Task<RestrictionDto> CreateRestrictionAsync(
        CreateRestrictionRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateRestrictionRequest, RestrictionDto>(
            "/api/v1/moderation/restrictions",
            request,
            cancellationToken: cancellationToken);

    public Task<RestrictionDto> UpdateRestrictionAsync(
        Guid restrictionId,
        UpdateRestrictionRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateRestrictionRequest, RestrictionDto>(
            $"/api/v1/moderation/restrictions/{restrictionId}",
            request,
            cancellationToken);

    public Task<ListResponse<AuditLogEntryDto>> ListAuditLogsAsync(
        AuditLogQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<AuditLogEntryDto>>(
            BuildAuditLogPath(query ?? new AuditLogQuery()),
            cancellationToken);

    private static string BuildListReportsPath(BrowseModerationReportsQuery query)
    {
        var builder = new QueryBuilder();

        if (query.Status.HasValue)
        {
            builder.Add("status", query.Status.Value.ToString());
        }

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/moderation/reports{builder.ToQueryString()}";
    }

    private static string BuildAuditLogPath(AuditLogQuery query)
    {
        var builder = new QueryBuilder();

        if (query.ActorUserId.HasValue)
        {
            builder.Add("actorUserId", query.ActorUserId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(query.TargetEntityType))
        {
            builder.Add("targetEntityType", query.TargetEntityType);
        }

        if (query.TargetEntityId.HasValue)
        {
            builder.Add("targetEntityId", query.TargetEntityId.Value.ToString());
        }

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/audit-logs{builder.ToQueryString()}";
    }
}
