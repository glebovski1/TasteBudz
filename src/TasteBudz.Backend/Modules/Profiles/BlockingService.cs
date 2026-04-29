// Business rules for user blocking and unblock workflows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Manages the authenticated user's block list.
/// </summary>
public sealed class BlockingService(
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IDiscoveryRepository discoveryRepository,
    IEventRepository eventRepository,
    IGroupRepository groupRepository,
    IClock clock,
    IPersistenceTransactionRunner? transactionRunner = null)
{
    private readonly IPersistenceTransactionRunner persistenceTransactionRunner = transactionRunner ?? NoOpPersistenceTransactionRunner.Instance;

    public async Task<IReadOnlyCollection<BlockedUserDto>> ListAsync(Guid blockerUserId, CancellationToken cancellationToken = default)
    {
        var blocks = await profileRepository.ListBlocksAsync(blockerUserId, cancellationToken);
        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);
        var profiles = (await profileRepository.ListProfilesAsync(cancellationToken)).ToDictionary(profile => profile.UserId);

        return blocks
            .Where(block => accounts.ContainsKey(block.BlockedUserId))
            .Select(block =>
            {
                var account = accounts[block.BlockedUserId];
                profiles.TryGetValue(block.BlockedUserId, out var profile);
                return new BlockedUserDto(block.BlockedUserId, account.Username, profile?.DisplayName ?? account.Username, block.CreatedAtUtc);
            })
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<BlockedUserDto> CreateAsync(Guid blockerUserId, CreateBlockRequest request, CancellationToken cancellationToken = default)
    {
        var blockedUserId = request.BlockedUserId ?? throw ApiException.BadRequest("blockedUserId is required.");

        if (blockedUserId == blockerUserId)
        {
            throw ApiException.BadRequest("You cannot block yourself.");
        }

        var blockedAccount = await authRepository.GetByIdAsync(blockedUserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");

        var existing = await profileRepository.GetBlockAsync(blockerUserId, blockedUserId, cancellationToken);

        var now = clock.UtcNow;

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                if (existing is null)
                {
                    await profileRepository.SaveBlockAsync(new UserBlock(blockerUserId, blockedUserId, now), cancellationToken);
                }

                await RemoveBudConnectionAsync(blockerUserId, blockedUserId, now, cancellationToken);
                await SeparateSharedEventsAsync(blockerUserId, blockedUserId, now, cancellationToken);
                await SeparateSharedGroupsAsync(blockerUserId, blockedUserId, now, cancellationToken);
            },
            cancellationToken);

        var blockedProfile = await profileRepository.GetProfileAsync(blockedUserId, cancellationToken);
        return new BlockedUserDto(blockedUserId, blockedAccount.Username, blockedProfile?.DisplayName ?? blockedAccount.Username, existing?.CreatedAtUtc ?? now);
    }

    public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default) =>
        profileRepository.DeleteBlockAsync(blockerUserId, blockedUserId, cancellationToken);

    private async Task RemoveBudConnectionAsync(Guid blockerUserId, Guid blockedUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = await discoveryRepository.GetBudConnectionAsync(blockerUserId, blockedUserId, cancellationToken);

        if (connection?.State == BudConnectionState.Connected)
        {
            await discoveryRepository.SaveBudConnectionAsync(connection with
            {
                State = BudConnectionState.Removed,
                EndedAtUtc = now,
            }, cancellationToken);
        }
    }

    private async Task SeparateSharedEventsAsync(Guid blockerUserId, Guid blockedUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var blockerParticipations = await eventRepository.ListParticipantsForUserAsync(blockerUserId, cancellationToken);

        foreach (var blockerParticipation in blockerParticipations.Where(participant => participant.State == EventParticipantState.Joined))
        {
            var eventRecord = await eventRepository.GetAsync(blockerParticipation.EventId, cancellationToken);

            if (eventRecord is null || eventRecord.Status == EventStatus.Completed)
            {
                continue;
            }

            var blockedParticipation = await eventRepository.GetParticipantAsync(blockerParticipation.EventId, blockedUserId, cancellationToken);

            if (blockedParticipation?.State != EventParticipantState.Joined)
            {
                continue;
            }

            if (eventRecord.HostUserId == blockerUserId)
            {
                await eventRepository.SaveParticipantAsync(blockedParticipation with
                {
                    State = EventParticipantState.Removed,
                    RespondedAtUtc = now,
                    RemovedAtUtc = now,
                }, cancellationToken);
                continue;
            }

            await eventRepository.SaveParticipantAsync(blockerParticipation with
            {
                State = EventParticipantState.Left,
                RespondedAtUtc = now,
                LeftAtUtc = now,
            }, cancellationToken);
        }
    }

    private async Task SeparateSharedGroupsAsync(Guid blockerUserId, Guid blockedUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var blockerMemberships = await groupRepository.ListMembershipsForUserAsync(blockerUserId, cancellationToken);

        foreach (var blockerMembership in blockerMemberships.Where(member => member.State == GroupMemberState.Active))
        {
            var group = await groupRepository.GetAsync(blockerMembership.GroupId, cancellationToken);

            if (group is null || group.LifecycleState != GroupLifecycleState.Active)
            {
                continue;
            }

            var blockedMembership = await groupRepository.GetMemberAsync(blockerMembership.GroupId, blockedUserId, cancellationToken);

            if (blockedMembership?.State != GroupMemberState.Active)
            {
                continue;
            }

            if (group.OwnerUserId == blockerUserId)
            {
                await groupRepository.SaveMemberAsync(blockedMembership with
                {
                    State = GroupMemberState.Removed,
                    UpdatedAtUtc = now,
                }, cancellationToken);
                continue;
            }

            await groupRepository.SaveMemberAsync(blockerMembership with
            {
                State = GroupMemberState.Left,
                UpdatedAtUtc = now,
            }, cancellationToken);
        }
    }
}
