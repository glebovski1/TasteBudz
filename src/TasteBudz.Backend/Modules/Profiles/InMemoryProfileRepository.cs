// In-memory profile repository used by the MVP runtime and automated tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Stores profile-side data in the shared in-memory store.
/// </summary>
public sealed class InMemoryProfileRepository(InMemoryTasteBudzStore store) : IProfileRepository
{
    public Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Profiles.TryGetValue(userId, out var profile);
            return Task.FromResult(profile);
        }
    }

    public Task SaveProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Profiles[profile.UserId] = profile;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<UserProfile>> ListProfilesAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<UserProfile>>(store.Profiles.Values.ToArray());
        }
    }

    public Task<UserPreferences?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Preferences.TryGetValue(userId, out var preferences);
            return Task.FromResult(preferences);
        }
    }

    public Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Preferences[preferences.UserId] = preferences;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<RecurringAvailabilityWindow>> ListRecurringAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.RecurringAvailability.Values
                .Where(window => window.UserId == userId)
                .OrderBy(window => window.DayOfWeek)
                .ThenBy(window => window.StartTime)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<RecurringAvailabilityWindow>>(items);
        }
    }

    public Task<IReadOnlyCollection<OneOffAvailabilityWindow>> ListOneOffAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.OneOffAvailability.Values
                .Where(window => window.UserId == userId)
                .OrderBy(window => window.StartsAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<OneOffAvailabilityWindow>>(items);
        }
    }

    public Task<RecurringAvailabilityWindow?> GetRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            if (!store.RecurringAvailability.TryGetValue(windowId, out var window) || window.UserId != userId)
            {
                return Task.FromResult<RecurringAvailabilityWindow?>(null);
            }

            return Task.FromResult<RecurringAvailabilityWindow?>(window);
        }
    }

    public Task<OneOffAvailabilityWindow?> GetOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            if (!store.OneOffAvailability.TryGetValue(windowId, out var window) || window.UserId != userId)
            {
                return Task.FromResult<OneOffAvailabilityWindow?>(null);
            }

            return Task.FromResult<OneOffAvailabilityWindow?>(window);
        }
    }

    public Task SaveRecurringAvailabilityAsync(RecurringAvailabilityWindow window, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.RecurringAvailability[window.Id] = window;
            return Task.CompletedTask;
        }
    }

    public Task SaveOneOffAvailabilityAsync(OneOffAvailabilityWindow window, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.OneOffAvailability[window.Id] = window;
            return Task.CompletedTask;
        }
    }

    public Task DeleteRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            if (store.RecurringAvailability.TryGetValue(windowId, out var window) && window.UserId == userId)
            {
                store.RecurringAvailability.Remove(windowId);
            }

            return Task.CompletedTask;
        }
    }

    public Task DeleteOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            if (store.OneOffAvailability.TryGetValue(windowId, out var window) && window.UserId == userId)
            {
                store.OneOffAvailability.Remove(windowId);
            }

            return Task.CompletedTask;
        }
    }

    public Task<PrivacySettings?> GetPrivacySettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.PrivacySettings.TryGetValue(userId, out var settings);
            return Task.FromResult(settings);
        }
    }

    public Task SavePrivacySettingsAsync(PrivacySettings settings, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.PrivacySettings[settings.UserId] = settings;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<UserBlock>> ListBlocksAsync(Guid blockerUserId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.Blocks.Values
                .Where(block => block.BlockerUserId == blockerUserId)
                .OrderBy(block => block.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<UserBlock>>(items);
        }
    }

    public Task<UserBlock?> GetBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Blocks.TryGetValue(ToBlockKey(blockerUserId, blockedUserId), out var block);
            return Task.FromResult(block);
        }
    }

    public Task SaveBlockAsync(UserBlock block, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Blocks[ToBlockKey(block.BlockerUserId, block.BlockedUserId)] = block;
            return Task.CompletedTask;
        }
    }

    public Task DeleteBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Blocks.Remove(ToBlockKey(blockerUserId, blockedUserId));
            return Task.CompletedTask;
        }
    }

    private static string ToBlockKey(Guid blockerUserId, Guid blockedUserId) => $"{blockerUserId:N}:{blockedUserId:N}";
}