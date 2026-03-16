using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class ModerationApiServiceTests
{
    [Fact]
    public async Task ModerationEndpoints_SendExpectedRoutesQueriesAndPayloads()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new ModerationApiService(client));
        var reportId = Guid.NewGuid();
        var restrictionId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/reports",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationReportDto(reportId, actorUserId, ReportTargetType.User, targetId, "Harassment", "Offensive messages", null, null, subjectUserId, null, DateTimeOffset.UtcNow, ModerationReportStatus.Pending, null, null, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/reports?status=Pending&page=2&pageSize=10",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<ModerationReportDto>(
                    new[]
                    {
                        new ModerationReportDto(reportId, actorUserId, ReportTargetType.User, targetId, "Harassment", "Offensive messages", null, null, subjectUserId, null, DateTimeOffset.UtcNow, ModerationReportStatus.Pending, null, null, null, null),
                    },
                    1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/moderation/reports/{reportId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationReportDto(reportId, actorUserId, ReportTargetType.User, targetId, "Harassment", "Offensive messages", null, null, subjectUserId, null, DateTimeOffset.UtcNow, ModerationReportStatus.Pending, null, null, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/moderation/reports/{reportId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationReportDto(reportId, actorUserId, ReportTargetType.User, targetId, "Harassment", "Offensive messages", null, null, subjectUserId, null, DateTimeOffset.UtcNow, ModerationReportStatus.Resolved, actorUserId, DateTimeOffset.UtcNow, "Warned", "User warned")));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/moderation/restrictions",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestrictionDto(restrictionId, subjectUserId, RestrictionScope.ChatSend, "Spam", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7), RestrictionStatus.Active, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/moderation/restrictions/{restrictionId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestrictionDto(restrictionId, subjectUserId, RestrictionScope.ChatSend, "Spam", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14), RestrictionStatus.Active, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/audit-logs?actorUserId={actorUserId}&targetEntityType=Event&targetEntityId={targetId}&page=1&pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<AuditLogEntryDto>(
                    new[]
                    {
                        new AuditLogEntryDto(Guid.NewGuid(), "EventUpdated", actorUserId, "Event", targetId, DateTimeOffset.UtcNow, "Updated title"),
                    },
                    1)));

        var createdReport = await service.CreateReportAsync(new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.User,
            TargetId = targetId,
            Category = "Harassment",
            Reason = "Offensive messages",
            RelatedUserId = subjectUserId,
        });
        var reports = await service.ListReportsAsync(new BrowseModerationReportsQuery
        {
            Status = ModerationReportStatus.Pending,
            Page = 2,
            PageSize = 10,
        });
        var report = await service.GetReportAsync(reportId);
        var resolvedReport = await service.ResolveReportAsync(reportId, new ResolveModerationReportRequest
        {
            Decision = "Warned",
            Notes = "User warned",
        });
        var restriction = await service.CreateRestrictionAsync(new CreateRestrictionRequest
        {
            SubjectUserId = subjectUserId,
            Scope = RestrictionScope.ChatSend,
            Reason = "Spam",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7),
        });
        var updatedRestriction = await service.UpdateRestrictionAsync(restrictionId, new UpdateRestrictionRequest
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(14),
        });
        var auditLogs = await service.ListAuditLogsAsync(new AuditLogQuery
        {
            ActorUserId = actorUserId,
            TargetEntityType = "Event",
            TargetEntityId = targetId,
        });

        Assert.Equal(reportId, createdReport.ReportId);
        Assert.Single(reports.Items);
        Assert.Equal(reportId, report.ReportId);
        Assert.Equal(ModerationReportStatus.Resolved, resolvedReport.Status);
        Assert.Equal(restrictionId, restriction.RestrictionId);
        Assert.Equal(restrictionId, updatedRestriction.RestrictionId);
        Assert.Single(auditLogs.Items);
        Assert.Contains(
            "\"category\":\"Harassment\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/reports").Body);
        Assert.Contains(
            "\"decision\":\"Warned\"",
            context.BackendHandler.Requests.Single(request =>
                request.PathAndQuery == $"/api/v1/moderation/reports/{reportId}" &&
                request.Method == HttpMethod.Patch).Body);
        Assert.Contains(
            "\"scope\":\"ChatSend\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/moderation/restrictions").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
