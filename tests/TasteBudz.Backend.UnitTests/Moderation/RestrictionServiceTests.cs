// Unit tests for restriction lifecycle rules.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Moderation;

/// <summary>
/// Verifies restriction expiry and revocation behavior.
/// </summary>
public sealed class RestrictionServiceTests
{
    [Fact]
    public async Task CreateUserBanAsync_WithMatchingReport_CreatesAllBanScopesAndResolvesReport()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var moderator = await RegisterAsync(services.AuthService, "mod", "mod@example.com");
        var reporter = await RegisterAsync(services.AuthService, "reporter", "reporter@example.com");
        var subject = await RegisterAsync(services.AuthService, "subject", "subject@example.com");
        var reportId = Guid.NewGuid();
        var expiresAtUtc = clock.UtcNow.AddDays(7);

        await services.ModerationRepository.SaveReportAsync(new ModerationReport(
            reportId,
            reporter.CurrentUser.UserId,
            ReportTargetType.User,
            subject.CurrentUser.UserId,
            "Safety",
            "Repeated unwanted contact",
            null,
            null,
            subject.CurrentUser.UserId,
            null,
            clock.UtcNow.AddMinutes(-5),
            ModerationReportStatus.Pending,
            null,
            null,
            null,
            null));

        var ban = await services.RestrictionService.CreateUserBanAsync(
            new CurrentUser(moderator.CurrentUser.UserId, moderator.CurrentUser.Username, new[] { UserRole.Moderator }),
            new CreateUserBanRequest
            {
                SubjectUserId = subject.CurrentUser.UserId,
                Reason = "Safety ban",
                ExpiresAtUtc = expiresAtUtc,
                ReportId = reportId,
            });

        var storedReport = await services.ModerationRepository.GetReportAsync(reportId);
        var actions = await services.ModerationRepository.ListActionsAsync();
        var auditLogEntries = await services.ModerationRepository.ListAuditLogEntriesAsync();

        Assert.Equal(subject.CurrentUser.UserId, ban.SubjectUserId);
        Assert.Equal(
            new[]
            {
                RestrictionScope.DiscoveryVisibility,
                RestrictionScope.ChatSend,
                RestrictionScope.EventJoin,
                RestrictionScope.EventCreate,
            }.OrderBy(scope => scope),
            ban.Restrictions.Select(restriction => restriction.Scope).OrderBy(scope => scope));
        Assert.All(ban.Restrictions, restriction =>
        {
            Assert.Equal(subject.CurrentUser.UserId, restriction.SubjectUserId);
            Assert.Equal("Safety ban", restriction.Reason);
            Assert.Equal(expiresAtUtc, restriction.ExpiresAtUtc);
            Assert.Equal(RestrictionStatus.Active, restriction.Status);
        });
        Assert.NotNull(ban.ResolvedReport);
        Assert.Equal(ModerationReportStatus.Resolved, ban.ResolvedReport.Status);
        Assert.Equal("UserSoftBanned", ban.ResolvedReport.ResolutionDecision);
        Assert.Equal(ModerationReportStatus.Resolved, storedReport!.Status);
        Assert.Equal(moderator.CurrentUser.UserId, storedReport.ResolvedByUserId);
        Assert.Equal("UserSoftBanned", storedReport.ResolutionDecision);
        Assert.Equal(4, actions.Count(action => action.ActionType == ModerationActionType.RestrictionCreated && action.ReportId == reportId));
        Assert.Single(actions, action => action.ActionType == ModerationActionType.ReportResolved && action.ReportId == reportId);
        Assert.Equal(4, auditLogEntries.Count(entry => entry.ActionType == "RestrictionCreated"));
        Assert.Single(auditLogEntries, entry => entry.ActionType == "ReportResolved");
    }

    [Fact]
    public async Task IsRestrictedAsync_ExpiredRestrictionReturnsFalseAndMarksExpired()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var moderator = await RegisterAsync(services.AuthService, "mod", "mod@example.com");
        var subject = await RegisterAsync(services.AuthService, "user", "user@example.com");

        var restriction = await services.RestrictionService.CreateAsync(
            new CurrentUser(moderator.CurrentUser.UserId, moderator.CurrentUser.Username, new[] { UserRole.Moderator }),
            new CreateRestrictionRequest
            {
                SubjectUserId = subject.CurrentUser.UserId,
                Scope = RestrictionScope.ChatSend,
                Reason = "Cooldown",
                ExpiresAtUtc = clock.UtcNow.AddMinutes(5),
            });

        clock.Advance(TimeSpan.FromMinutes(10));

        var restricted = await services.RestrictionService.IsRestrictedAsync(subject.CurrentUser.UserId, RestrictionScope.ChatSend);
        var stored = await services.ModerationRepository.GetRestrictionAsync(restriction.RestrictionId);

        Assert.False(restricted);
        Assert.Equal(RestrictionStatus.Expired, stored!.Status);
    }

    [Fact]
    public async Task UpdateAsync_RevokeMarksRestrictionRevoked()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var moderator = await RegisterAsync(services.AuthService, "mod", "mod@example.com");
        var subject = await RegisterAsync(services.AuthService, "user", "user@example.com");

        var restriction = await services.RestrictionService.CreateAsync(
            new CurrentUser(moderator.CurrentUser.UserId, moderator.CurrentUser.Username, new[] { UserRole.Moderator }),
            new CreateRestrictionRequest
            {
                SubjectUserId = subject.CurrentUser.UserId,
                Scope = RestrictionScope.EventJoin,
                Reason = "Safety pause",
            });

        var updated = await services.RestrictionService.UpdateAsync(
            new CurrentUser(moderator.CurrentUser.UserId, moderator.CurrentUser.Username, new[] { UserRole.Moderator }),
            restriction.RestrictionId,
            new UpdateRestrictionRequest
            {
                Revoke = true,
                Reason = "Appeal granted",
            });

        Assert.Equal(RestrictionStatus.Revoked, updated.Status);
        Assert.NotNull(updated.RevokedAtUtc);
    }

    private static async Task<SessionDto> RegisterAsync(AuthService authService, string username, string email) =>
        await authService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

    private static TestServices CreateServices(TestClock clock)
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var auditLogService = new AuditLogService(moderationRepository);
        var restrictionService = new RestrictionService(moderationRepository, authRepository, auditLogService, clock);

        return new TestServices(authService, restrictionService, moderationRepository);
    }

    private sealed record TestServices(
        AuthService AuthService,
        RestrictionService RestrictionService,
        IModerationRepository ModerationRepository);
}
