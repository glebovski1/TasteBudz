// Integration tests for moderation, restrictions, and audit APIs.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Exercises end-user report submission and moderator/admin review flows through HTTP.
/// </summary>
public sealed class ModerationApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task ReportQueueEndpoints_SupportSubmitAndResolve()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var reporterClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var reporterSession = await ApiTestHelpers.RegisterAsync(reporterClient, username: "reporter", email: "reporter@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "subject", email: "subject@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(reporterClient, reporterSession.AccessToken);

        var createResponse = await reporterClient.PostAsJsonAsync("/api/v1/reports", new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.User,
            TargetId = subjectSession.CurrentUser.UserId,
            Category = "Harassment",
            Reason = "Repeated abuse",
        });
        var report = await createResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        var queueResponse = await moderatorClient.GetAsync("/api/v1/moderation/reports");
        var queue = await queueResponse.Content.ReadFromJsonAsync<ListResponse<ModerationReportDto>>(ApiTestHelpers.JsonOptions);

        var resolveResponse = await moderatorClient.PatchAsJsonAsync($"/api/v1/moderation/reports/{report!.ReportId}", new ResolveModerationReportRequest
        {
            Decision = "NoAction",
            Notes = "Reviewed and documented",
        });
        var resolved = await resolveResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.Contains(queue!.Items, item => item.ReportId == report.ReportId);
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        Assert.Equal(ModerationReportStatus.Resolved, resolved!.Status);
    }

    [Fact]
    public async Task RestrictionAndAuditEndpoints_RespectRoleBoundaries()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        using var userClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var adminSession = await ApiTestHelpers.RegisterAsync(adminClient, username: "admin", email: "admin@example.com");
        var userSession = await ApiTestHelpers.RegisterAsync(userClient, username: "user", email: "user@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, adminSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Admin });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(adminClient, adminSession.AccessToken);
        ApiTestHelpers.SetBearer(userClient, userSession.AccessToken);

        var createRestrictionResponse = await moderatorClient.PostAsJsonAsync("/api/v1/moderation/restrictions", new CreateRestrictionRequest
        {
            SubjectUserId = userSession.CurrentUser.UserId,
            Scope = RestrictionScope.EventJoin,
            Reason = "Safety pause",
        });
        var restriction = await createRestrictionResponse.Content.ReadFromJsonAsync<RestrictionDto>(ApiTestHelpers.JsonOptions);

        var userAuditResponse = await userClient.GetAsync("/api/v1/audit-logs");
        var adminAuditResponse = await adminClient.GetAsync("/api/v1/audit-logs");
        var audit = await adminAuditResponse.Content.ReadFromJsonAsync<ListResponse<AuditLogEntryDto>>(ApiTestHelpers.JsonOptions);

        var revokeResponse = await moderatorClient.PatchAsJsonAsync($"/api/v1/moderation/restrictions/{restriction!.RestrictionId}", new UpdateRestrictionRequest
        {
            Revoke = true,
            Reason = "Appeal granted",
        });
        revokeResponse.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, createRestrictionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userAuditResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminAuditResponse.StatusCode);
        Assert.Contains(audit!.Items, item => item.ActionType == "RestrictionCreated");
    }
}
