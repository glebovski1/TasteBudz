namespace TasteBudz.Backend.Modules.Profiles;

internal static class BlockingPolicy
{
    internal static async Task<bool> HasBlockBetweenAsync(
        IProfileRepository profileRepository,
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken)
    {
        if (firstUserId == secondUserId)
        {
            return false;
        }

        return await profileRepository.GetBlockAsync(firstUserId, secondUserId, cancellationToken) is not null ||
            await profileRepository.GetBlockAsync(secondUserId, firstUserId, cancellationToken) is not null;
    }

    internal static async Task<bool> HasBlockWithAnyAsync(
        IProfileRepository profileRepository,
        Guid currentUserId,
        IEnumerable<Guid> otherUserIds,
        CancellationToken cancellationToken)
    {
        foreach (var otherUserId in otherUserIds.Where(userId => userId != currentUserId).Distinct())
        {
            if (await HasBlockBetweenAsync(profileRepository, currentUserId, otherUserId, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }
}
