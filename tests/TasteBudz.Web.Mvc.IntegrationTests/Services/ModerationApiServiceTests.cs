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
        var userSummary = new ModerationUserSummaryDto(
            subjectUserId,
            "subject",
            "Subject Person",
            "subject@example.com",
            AccountStatus.Active,
            new[] { UserRole.User });

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
            HttpMethod.Post,
            "/api/v1/moderation/bans",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new UserBanDto(
                    subjectUserId,
                    new[]
                    {
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.DiscoveryVisibility, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.ChatSend, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.EventJoin, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.EventCreate, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                    },
                    null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/search?q=needle&type=Message&page=3&pageSize=5",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationSearchResponseDto(
                    "needle",
                    ModerationSearchResultKind.Message,
                    new[]
                    {
                        new ModerationSearchResultDto(
                            ModerationSearchResultKind.Message,
                            Guid.NewGuid(),
                            "Message from Subject Person (@subject)",
                            "Support chat",
                            "needle message",
                            DateTimeOffset.UtcNow,
                            userSummary,
                            null,
                            "Support",
                            subjectUserId),
                    },
                    1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/moderation/reports/{reportId}/review",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationReportReviewDto(
                    new ModerationReportDto(reportId, actorUserId, ReportTargetType.User, subjectUserId, "Harassment", "Offensive messages", null, null, subjectUserId, null, DateTimeOffset.UtcNow, ModerationReportStatus.Pending, null, null, null, null),
                    userSummary,
                    userSummary,
                    userSummary,
                    null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/moderation/users/{subjectUserId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationUserDetailDto(userSummary, Array.Empty<RestrictionDto>(), 2, 3, DateTimeOffset.UtcNow)));
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
        var ban = await service.CreateUserBanAsync(new CreateUserBanRequest
        {
            SubjectUserId = subjectUserId,
            Reason = "Full safety ban",
            ReportId = reportId,
        });
        var search = await service.SearchAsync(new ModerationSearchQuery
        {
            Q = "needle",
            Type = ModerationSearchResultKind.Message,
            Page = 3,
            PageSize = 5,
        });
        var reportReview = await service.GetReportReviewAsync(reportId);
        var userDetail = await service.GetUserDetailAsync(subjectUserId);
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
        Assert.Equal(subjectUserId, ban.SubjectUserId);
        Assert.Equal(4, ban.Restrictions.Count);
        Assert.Single(search.Items);
        Assert.Equal("subject", reportReview.SubjectUser!.Username);
        Assert.Equal(subjectUserId, userDetail.User.UserId);
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
        var banRequestBody = context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/moderation/bans").Body;
        Assert.Contains($"\"subjectUserId\":\"{subjectUserId}", banRequestBody);
        Assert.Contains($"\"reportId\":\"{reportId}", banRequestBody);
        Assert.Contains("\"reason\":\"Full safety ban\"", banRequestBody);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
