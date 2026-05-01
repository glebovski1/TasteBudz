using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Moderation;

/// <summary>
/// Privileged read model for admin/moderator search and report-review identity resolution.
/// </summary>
public sealed class ModerationSearchService(TasteBudzDbContext dbContext)
{
    private const int SnippetLength = 180;

    public async Task<ModerationSearchResponseDto> SearchAsync(
        CurrentUser currentUser,
        ModerationSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var searchText = query.Q?.Trim();
        var normalizedSearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText;

        var users = await LoadUserSummariesAsync(cancellationToken);
        var results = new List<ModerationSearchResultDto>();

        if (MatchesType(query, ModerationSearchResultKind.User))
        {
            results.AddRange(SearchUsers(users, normalizedSearchText));
        }

        if (MatchesType(query, ModerationSearchResultKind.Message))
        {
            results.AddRange(await SearchMessagesAsync(users, normalizedSearchText, cancellationToken));
        }

        if (MatchesType(query, ModerationSearchResultKind.Report))
        {
            results.AddRange(await SearchReportsAsync(users, normalizedSearchText, cancellationToken));
        }

        if (MatchesType(query, ModerationSearchResultKind.Event))
        {
            results.AddRange(await SearchEventsAsync(users, normalizedSearchText, cancellationToken));
        }

        if (MatchesType(query, ModerationSearchResultKind.Group))
        {
            results.AddRange(await SearchGroupsAsync(users, normalizedSearchText, cancellationToken));
        }

        if (MatchesType(query, ModerationSearchResultKind.Feedback))
        {
            results.AddRange(await SearchFeedbackAsync(users, normalizedSearchText, cancellationToken));
        }

        if (MatchesType(query, ModerationSearchResultKind.Restaurant))
        {
            results.AddRange(await SearchRestaurantsAsync(normalizedSearchText, cancellationToken));
        }

        if (currentUser.IsInRole(UserRole.Admin) && MatchesType(query, ModerationSearchResultKind.Audit))
        {
            results.AddRange(await SearchAuditAsync(users, normalizedSearchText, cancellationToken));
        }

        var ordered = results
            .OrderByDescending(item => item.CreatedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new ModerationSearchResponseDto(normalizedSearchText, query.Type, items, ordered.Length);
    }

    public async Task<ModerationReportReviewDto> GetReportReviewAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.ModerationReports
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken)
            ?? throw ApiException.NotFound("The requested report could not be found.");
        var users = await LoadUserSummariesAsync(cancellationToken);
        var relatedUser = report.RelatedUserId.HasValue ? GetUser(users, report.RelatedUserId.Value) : null;
        var relatedMessage = await ResolveMessageResultAsync(users, report.RelatedMessageId ?? (report.TargetType == ReportTargetType.Message ? report.TargetId : null), cancellationToken);
        var subjectUser = ResolveReportSubject(users, report, relatedMessage);

        return new ModerationReportReviewDto(
            ToReportDto(report),
            GetUser(users, report.ReporterUserId),
            subjectUser,
            relatedUser,
            relatedMessage);
    }

    public async Task<ModerationUserDetailDto> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var users = await LoadUserSummariesAsync(cancellationToken);
        var user = GetUser(users, userId)
            ?? throw ApiException.NotFound("The requested user could not be found.");
        var restrictions = await dbContext.UserRestrictions
            .AsNoTracking()
            .Where(item => item.SubjectUserId == userId)
            .OrderByDescending(item => item.StartsAtUtc)
            .ThenBy(item => item.Scope)
            .Select(item => new RestrictionDto(
                item.Id,
                item.SubjectUserId,
                item.Scope,
                item.Reason,
                item.StartsAtUtc,
                item.ExpiresAtUtc,
                item.Status,
                item.RevokedAtUtc))
            .ToArrayAsync(cancellationToken);
        var reportCount = await dbContext.ModerationReports
            .AsNoTracking()
            .CountAsync(report =>
                report.ReporterUserId == userId ||
                report.RelatedUserId == userId ||
                (report.TargetType == ReportTargetType.User && report.TargetId == userId),
                cancellationToken);
        var messageStats = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.SenderUserId == userId)
            .GroupBy(message => message.SenderUserId)
            .Select(group => new
            {
                Count = group.Count(),
                LastMessageAtUtc = group.Max(message => message.CreatedAtUtc),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new ModerationUserDetailDto(
            user,
            restrictions,
            reportCount,
            messageStats?.Count ?? 0,
            messageStats?.LastMessageAtUtc);
    }

    private static bool MatchesType(ModerationSearchQuery query, ModerationSearchResultKind kind) =>
        !query.Type.HasValue || query.Type.Value == kind;

    private static IEnumerable<ModerationSearchResultDto> SearchUsers(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        string? searchText)
    {
        var matchingUsers = users.Values.AsEnumerable();
        if (HasSearchText(searchText))
        {
            matchingUsers = matchingUsers.Where(user =>
                Contains(user.Username, searchText) ||
                Contains(user.DisplayName, searchText) ||
                Contains(user.Email, searchText));
        }

        foreach (var user in matchingUsers
            .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .Take(200))
        {
            yield return new ModerationSearchResultDto(
                ModerationSearchResultKind.User,
                user.UserId,
                FormatUserLabel(user),
                $"{user.Status} account - {string.Join(", ", user.Roles)}",
                user.Email,
                null,
                user,
                null,
                "User",
                user.UserId);
        }
    }

    private async Task<IReadOnlyCollection<ModerationSearchResultDto>> SearchMessagesAsync(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        string? searchText,
        CancellationToken cancellationToken)
    {
        var query =
            from message in dbContext.ChatMessages.AsNoTracking()
            join thread in dbContext.ChatThreads.AsNoTracking() on message.ThreadId equals thread.Id
            select new
            {
                Message = message,
                Thread = thread,
            };

        if (HasSearchText(searchText))
        {
            var pattern = ToLikePattern(searchText);
            query = query.Where(row => EF.Functions.Like(row.Message.Body, pattern));
        }

        var rows = await query
            .OrderByDescending(row => row.Message.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => BuildMessageResult(users, row.Message, row.Thread))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ModerationSearchResultDto>> SearchReportsAsync(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        string? searchText,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ModerationReports.AsNoTracking();
        if (HasSearchText(searchText))
        {
            var pattern = ToLikePattern(searchText);
            query = query.Where(report =>
                EF.Functions.Like(report.Category, pattern) ||
                EF.Functions.Like(report.Reason, pattern) ||
                (report.Explanation != null && EF.Functions.Like(report.Explanation, pattern)) ||
                (report.ResolutionDecision != null && EF.Functions.Like(report.ResolutionDecision, pattern)) ||
                (report.ResolutionNotes != null && EF.Functions.Like(report.ResolutionNotes, pattern)));
        }

        var reports = await query
            .OrderByDescending(report => report.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return reports
            .Select(report =>
            {
                var subject = ResolveReportSubject(users, report, relatedMessage: null);
                return new ModerationSearchResultDto(
                    ModerationSearchResultKind.Report,
                    report.Id,
                    $"{report.Category}: {report.Reason}",
                    $"{report.Status} report",
                    Snippet(report.Explanation ?? report.ResolutionNotes ?? report.Reason),
                    report.CreatedAtUtc,
                    GetUser(users, report.ReporterUserId),
                    subject,
                    nameof(ModerationReport),
                    report.Id);
            })
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ModerationSearchResultDto>> SearchEventsAsync(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        string? searchText,
        CancellationToken cancellationToken)
    {
        var query =
            from item in dbContext.Events.AsNoTracking()
            join restaurant in dbContext.Restaurants.AsNoTracking() on item.SelectedRestaurantId equals restaurant.Id into restaurants
            from restaurant in restaurants.DefaultIfEmpty()
            select new
            {
                Event = item,
                RestaurantName = restaurant == null ? null : restaurant.Name,
            };

        if (HasSearchText(searchText))
        {
            var pattern = ToLikePattern(searchText);
            query = query.Where(row =>
                (row.Event.Title != null && EF.Functions.Like(row.Event.Title, pattern)) ||
                (row.Event.CuisineTarget != null && EF.Functions.Like(row.Event.CuisineTarget, pattern)) ||
                (row.RestaurantName != null && EF.Functions.Like(row.RestaurantName, pattern)));
        }

        var rows = await query
            .OrderByDescending(row => row.Event.EventStartAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ModerationSearchResultDto(
                ModerationSearchResultKind.Event,
                row.Event.Id,
                row.Event.Title ?? "Dining event",
                $"{row.Event.Status} {row.Event.EventType} event",
                row.RestaurantName ?? row.Event.CuisineTarget,
                row.Event.EventStartAtUtc,
                GetUser(users, row.Event.HostUserId),
                null,
                nameof(Event),
                row.Event.Id))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ModerationSearchResultDto>> SearchGroupsAsync(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        string? searchText,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Groups.AsNoTracking();
        if (HasSearchText(searchText))
        {
            var pattern = ToLikePattern(searchText);
            query = query.Where(group =>
                EF.Functions.Like(group.Name, pattern) ||
                (group.Description != null && EF.Functions.Like(group.Description, pattern)));
        }

        var groups = await query
            .OrderByDescending(group => group.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return groups
            .Select(group => new ModerationSearchResultDto(
                ModerationSearchResultKind.Group,
                group.Id,
                group.Name,
                $"{group.Visibility} group - {group.LifecycleState}",
                Snippet(group.Description),
                group.CreatedAtUtc,
                GetUser(users, group.OwnerUserId),
                null,
                nameof(Group),
                group.Id))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ModerationSearchResultDto>> SearchFeedbackAsync(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        string? searchText,
        CancellationToken cancellationToken)
    {
        var query =
            from feedback in dbContext.EventFeedbacks.AsNoTracking()
            join item in dbContext.Events.AsNoTracking() on feedback.EventId equals item.Id
            select new
            {
                Feedback = feedback,
                EventTitle = item.Title,
            };

        if (HasSearchText(searchText))
        {
            var pattern = ToLikePattern(searchText);
            query = query.Where(row => EF.Functions.Like(row.Feedback.Text, pattern));
        }

        var rows = await query
            .OrderByDescending(row => row.Feedback.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ModerationSearchResultDto(
                ModerationSearchResultKind.Feedback,
                row.Feedback.Id,
                $"Feedback: {row.Feedback.Rating}/5",
                row.EventTitle ?? "Dining event",
                Snippet(row.Feedback.Text),
                row.Feedback.CreatedAtUtc,
                GetUser(users, row.Feedback.AuthorUserId),
                null,
                "EventFeedback",
                row.Feedback.EventId))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ModerationSearchResultDto>> SearchRestaurantsAsync(
        string? searchText,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Restaurants.AsNoTracking();
        if (HasSearchText(searchText))
        {
            var pattern = ToLikePattern(searchText);
            query = query.Where(restaurant =>
                EF.Functions.Like(restaurant.Name, pattern) ||
                EF.Functions.Like(restaurant.City, pattern) ||
                EF.Functions.Like(restaurant.State, pattern) ||
                EF.Functions.Like(restaurant.ZipCode, pattern) ||
                (restaurant.StreetAddress != null && EF.Functions.Like(restaurant.StreetAddress, pattern)));
        }

        var restaurants = await query
            .OrderBy(restaurant => restaurant.Name)
            .Take(200)
            .ToListAsync(cancellationToken);

        return restaurants
            .Select(restaurant => new ModerationSearchResultDto(
                ModerationSearchResultKind.Restaurant,
                restaurant.Id,
                restaurant.Name,
                $"{restaurant.City}, {restaurant.State} {restaurant.ZipCode}",
                restaurant.IsArchived ? "Archived restaurant" : "Active restaurant",
                null,
                null,
                null,
                nameof(Restaurant),
                restaurant.Id))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ModerationSearchResultDto>> SearchAuditAsync(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        string? searchText,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogEntries.AsNoTracking();
        if (HasSearchText(searchText))
        {
            var pattern = ToLikePattern(searchText);
            query = query.Where(entry =>
                EF.Functions.Like(entry.ActionType, pattern) ||
                EF.Functions.Like(entry.TargetEntityType, pattern) ||
                EF.Functions.Like(entry.Details, pattern));
        }

        var entries = await query
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entries
            .Select(entry => new ModerationSearchResultDto(
                ModerationSearchResultKind.Audit,
                entry.Id,
                entry.ActionType,
                entry.TargetEntityType,
                Snippet(entry.Details),
                entry.CreatedAtUtc,
                GetUser(users, entry.ActorUserId),
                null,
                nameof(AuditLogEntry),
                entry.TargetEntityId))
            .ToArray();
    }

    private async Task<ModerationSearchResultDto?> ResolveMessageResultAsync(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        Guid? messageId,
        CancellationToken cancellationToken)
    {
        if (!messageId.HasValue)
        {
            return null;
        }

        var row = await (
            from message in dbContext.ChatMessages.AsNoTracking()
            join thread in dbContext.ChatThreads.AsNoTracking() on message.ThreadId equals thread.Id
            where message.Id == messageId.Value
            select new
            {
                Message = message,
                Thread = thread,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : BuildMessageResult(users, row.Message, row.Thread);
    }

    private static ModerationSearchResultDto BuildMessageResult(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        ChatMessageEntity message,
        ChatThreadEntity thread)
    {
        var sender = GetUser(users, message.SenderUserId);
        return new ModerationSearchResultDto(
            ModerationSearchResultKind.Message,
            message.Id,
            sender is null ? "Message from unknown user" : $"Message from {FormatUserLabel(sender)}",
            $"{thread.ScopeType} chat",
            Snippet(message.Body),
            message.CreatedAtUtc,
            sender,
            null,
            thread.ScopeType.ToString(),
            thread.ScopeId);
    }

    private static ModerationUserSummaryDto? ResolveReportSubject(
        IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users,
        ModerationReportEntity report,
        ModerationSearchResultDto? relatedMessage)
    {
        if (report.TargetType == ReportTargetType.User)
        {
            return GetUser(users, report.TargetId);
        }

        if (relatedMessage?.PrimaryUser is not null)
        {
            return relatedMessage.PrimaryUser;
        }

        if (report.RelatedUserId.HasValue)
        {
            return GetUser(users, report.RelatedUserId.Value);
        }

        return null;
    }

    private async Task<IReadOnlyDictionary<Guid, ModerationUserSummaryDto>> LoadUserSummariesAsync(CancellationToken cancellationToken)
    {
        var accounts = await dbContext.UserAccounts
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var accountIds = accounts.Select(account => account.Id).ToArray();
        var profiles = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => accountIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);
        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(role => accountIds.Contains(role.UserId))
            .ToListAsync(cancellationToken);
        var rolesByUserId = roles
            .GroupBy(role => role.UserId)
            .ToDictionary(group => group.Key, group => group.Select(role => role.Role).OrderBy(role => role).ToArray() as IReadOnlyCollection<UserRole>);

        return accounts.ToDictionary(
            account => account.Id,
            account =>
            {
                profiles.TryGetValue(account.Id, out var profile);
                rolesByUserId.TryGetValue(account.Id, out var userRoles);
                return new ModerationUserSummaryDto(
                    account.Id,
                    account.Username,
                    string.IsNullOrWhiteSpace(profile?.DisplayName) ? account.Username : profile.DisplayName,
                    account.Email,
                    account.Status,
                    userRoles ?? Array.Empty<UserRole>());
            });
    }

    private static ModerationUserSummaryDto? GetUser(IReadOnlyDictionary<Guid, ModerationUserSummaryDto> users, Guid userId) =>
        users.TryGetValue(userId, out var user) ? user : null;

    private static string FormatUserLabel(ModerationUserSummaryDto user) =>
        $"{user.DisplayName} (@{user.Username})";

    private static bool HasSearchText(string? searchText) =>
        !string.IsNullOrWhiteSpace(searchText);

    private static bool Contains(string? value, string? searchText) =>
        !string.IsNullOrWhiteSpace(searchText) &&
        value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;

    private static string Snippet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= SnippetLength
            ? normalized
            : $"{normalized[..SnippetLength]}...";
    }

    private static string ToLikePattern(string searchText) =>
        $"%{searchText.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]")}%";

    private static ModerationReportDto ToReportDto(ModerationReportEntity report) =>
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
}
