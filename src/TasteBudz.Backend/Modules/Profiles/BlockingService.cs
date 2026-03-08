// Business rules for user blocking and unblock workflows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Manages the authenticated user's block list.
/// </summary>
public sealed class BlockingService(
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IClock clock)
{
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

        if (existing is null)
        {
            await profileRepository.SaveBlockAsync(new UserBlock(blockerUserId, blockedUserId, clock.UtcNow), cancellationToken);
        }

        var blockedProfile = await profileRepository.GetProfileAsync(blockedUserId, cancellationToken);
        return new BlockedUserDto(blockedUserId, blockedAccount.Username, blockedProfile?.DisplayName ?? blockedAccount.Username, existing?.CreatedAtUtc ?? clock.UtcNow);
    }

    public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default) =>
        profileRepository.DeleteBlockAsync(blockerUserId, blockedUserId, cancellationToken);
}