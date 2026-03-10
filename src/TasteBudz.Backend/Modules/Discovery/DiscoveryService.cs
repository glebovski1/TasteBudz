// Discovery search, swipe, and Budz workflows.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Discovery;

/// <summary>
/// Owns user discovery filters, swipe decisions, and mutual Budz creation.
/// </summary>
public sealed class DiscoveryService(
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IDiscoveryRepository discoveryRepository,
    RestrictionService restrictionService,
    INotificationService notificationService,
    IClock clock)
{
    public async Task<ListResponse<DiscoveryProfilePreviewDto>> SearchAsync(Guid currentUserId, SearchPeopleQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await GetDiscoverableUsersAsync(currentUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var value = query.Q.Trim();
            candidates = candidates
                .Where(candidate =>
                    candidate.Username.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    candidate.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var items = candidates
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new ListResponse<DiscoveryProfilePreviewDto>(items, candidates.Length);
    }

    public async Task<ListResponse<DiscoveryProfilePreviewDto>> GetSwipeCandidatesAsync(Guid currentUserId, SwipeCandidatesQuery query, CancellationToken cancellationToken = default)
    {
        var budConnections = await discoveryRepository.ListBudConnectionsAsync(cancellationToken);
        var connectedUserIds = budConnections
            .Where(connection => connection.State == BudConnectionState.Connected && (connection.UserOneId == currentUserId || connection.UserTwoId == currentUserId))
            .Select(connection => connection.UserOneId == currentUserId ? connection.UserTwoId : connection.UserOneId)
            .ToHashSet();
        var candidates = (await GetDiscoverableUsersAsync(currentUserId, cancellationToken))
            .Where(candidate => !connectedUserIds.Contains(candidate.UserId))
            .ToArray();
        var items = candidates
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new ListResponse<DiscoveryProfilePreviewDto>(items, candidates.Length);
    }

    public async Task<SwipeDecisionResultDto> RecordSwipeAsync(CurrentUser currentUser, RecordSwipeDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var subjectUserId = request.SubjectUserId ?? throw ApiException.BadRequest("subjectUserId is required.");
        var decision = request.Decision ?? throw ApiException.BadRequest("decision is required.");

        if (subjectUserId == currentUser.UserId)
        {
            throw ApiException.BadRequest("You cannot swipe on yourself.");
        }

        await EnsureDiscoverableSubjectAsync(currentUser.UserId, subjectUserId, cancellationToken);

        var now = clock.UtcNow;
        var swipe = new SwipeDecision(currentUser.UserId, subjectUserId, decision, now);
        await discoveryRepository.SaveSwipeDecisionAsync(swipe, cancellationToken);

        var reciprocal = await discoveryRepository.GetSwipeDecisionAsync(subjectUserId, currentUser.UserId, cancellationToken);
        var existingConnection = await discoveryRepository.GetBudConnectionAsync(currentUser.UserId, subjectUserId, cancellationToken);
        var isBudMatch = decision == SwipeDecisionType.Like &&
                         reciprocal?.Decision == SwipeDecisionType.Like;
        Guid? budConnectionId = existingConnection?.Id;

        if (isBudMatch && existingConnection?.State != BudConnectionState.Connected)
        {
            var connection = new BudConnection(
                existingConnection?.Id ?? Guid.NewGuid(),
                NormalizePair(currentUser.UserId, subjectUserId).Lower,
                NormalizePair(currentUser.UserId, subjectUserId).Higher,
                BudConnectionState.Connected,
                existingConnection?.CreatedAtUtc ?? now,
                null);
            await discoveryRepository.SaveBudConnectionAsync(connection, cancellationToken);
            await NotifyBudMatchAsync(connection, cancellationToken);
            budConnectionId = connection.Id;
        }

        return new SwipeDecisionResultDto(subjectUserId, decision, isBudMatch, budConnectionId);
    }

    public async Task<IReadOnlyCollection<BudConnectionDto>> ListMyBudzAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);
        var profiles = (await profileRepository.ListProfilesAsync(cancellationToken)).ToDictionary(profile => profile.UserId);
        var connections = await discoveryRepository.ListBudConnectionsAsync(cancellationToken);

        return connections
            .Where(connection => connection.State == BudConnectionState.Connected && (connection.UserOneId == currentUserId || connection.UserTwoId == currentUserId))
            .Select(connection => connection.UserOneId == currentUserId ? new { OtherUserId = connection.UserTwoId, connection.CreatedAtUtc } : new { OtherUserId = connection.UserOneId, connection.CreatedAtUtc })
            .Where(item => accounts.ContainsKey(item.OtherUserId))
            .OrderBy(item => accounts[item.OtherUserId].Username, StringComparer.OrdinalIgnoreCase)
            .Select(item => new BudConnectionDto(
                item.OtherUserId,
                accounts[item.OtherUserId].Username,
                profiles.GetValueOrDefault(item.OtherUserId)?.DisplayName ?? accounts[item.OtherUserId].Username,
                item.CreatedAtUtc))
            .ToArray();
    }

    private async Task<DiscoveryProfilePreviewDto[]> GetDiscoverableUsersAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var accounts = await authRepository.ListActiveAccountsAsync(cancellationToken);
        var profiles = (await profileRepository.ListProfilesAsync(cancellationToken)).ToDictionary(profile => profile.UserId);
        var results = new List<DiscoveryProfilePreviewDto>(accounts.Count);

        foreach (var account in accounts)
        {
            if (account.Id == currentUserId)
            {
                continue;
            }

            if (!profiles.TryGetValue(account.Id, out var profile))
            {
                continue;
            }

            if (!await CanDiscoverAsync(currentUserId, account.Id, cancellationToken))
            {
                continue;
            }

            results.Add(ToPreview(account, profile));
        }

        return results
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Username, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task EnsureDiscoverableSubjectAsync(Guid currentUserId, Guid subjectUserId, CancellationToken cancellationToken)
    {
        var account = await authRepository.GetByIdAsync(subjectUserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");
        var profile = await profileRepository.GetProfileAsync(subjectUserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");

        if (!await CanDiscoverAsync(currentUserId, subjectUserId, cancellationToken))
        {
            throw ApiException.NotFound("The requested user could not be found.");
        }

        _ = ToPreview(account, profile);
    }

    private async Task<bool> CanDiscoverAsync(Guid currentUserId, Guid subjectUserId, CancellationToken cancellationToken)
    {
        var subjectPrivacy = await profileRepository.GetPrivacySettingsAsync(subjectUserId, cancellationToken);

        if (subjectPrivacy?.DiscoveryEnabled == false)
        {
            return false;
        }

        if (await profileRepository.GetBlockAsync(currentUserId, subjectUserId, cancellationToken) is not null ||
            await profileRepository.GetBlockAsync(subjectUserId, currentUserId, cancellationToken) is not null)
        {
            return false;
        }

        if (await restrictionService.IsRestrictedAsync(subjectUserId, RestrictionScope.DiscoveryVisibility, cancellationToken))
        {
            return false;
        }

        return true;
    }

    private async Task NotifyBudMatchAsync(BudConnection connection, CancellationToken cancellationToken)
    {
        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);

        if (!accounts.ContainsKey(connection.UserOneId) || !accounts.ContainsKey(connection.UserTwoId))
        {
            return;
        }

        var now = clock.UtcNow;
        await notificationService.CreateAsync(
            new Notification(Guid.NewGuid(), connection.UserOneId, NotificationType.BudMatched, "BudConnection", connection.Id, $"You matched with {accounts[connection.UserTwoId].Username}.", now, null),
            cancellationToken);
        await notificationService.CreateAsync(
            new Notification(Guid.NewGuid(), connection.UserTwoId, NotificationType.BudMatched, "BudConnection", connection.Id, $"You matched with {accounts[connection.UserOneId].Username}.", now, null),
            cancellationToken);
    }

    private static DiscoveryProfilePreviewDto ToPreview(UserAccount account, UserProfile profile) =>
        new(
            account.Id,
            account.Username,
            profile.DisplayName,
            profile.Bio,
            profile.SocialGoal);

    private static (Guid Lower, Guid Higher) NormalizePair(Guid first, Guid second) =>
        first.CompareTo(second) <= 0 ? (first, second) : (second, first);
}
