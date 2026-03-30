using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// SQLite-backed repository for profile-side data, availability, privacy, and blocks.
/// </summary>
public sealed class SqliteProfileRepository(TasteBudzDbContext dbContext) : IProfileRepository
{
    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
        return entity is null ? null : MapProfile(entity);
    }

    public async Task SaveProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserProfiles.FirstOrDefaultAsync(item => item.UserId == profile.UserId, cancellationToken);

        if (entity is null)
        {
            dbContext.UserProfiles.Add(ToEntity(profile));
        }
        else
        {
            entity.DisplayName = profile.DisplayName;
            entity.Bio = profile.Bio;
            entity.HomeAreaZipCode = profile.HomeAreaZipCode;
            entity.SocialGoal = profile.SocialGoal;
            entity.CreatedAtUtc = profile.CreatedAtUtc;
            entity.UpdatedAtUtc = profile.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserProfile>> ListProfilesAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.UserProfiles.AsNoTracking().ToListAsync(cancellationToken))
        .Select(MapProfile)
        .ToArray();

    public async Task<UserPreferences?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserPreferences.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return await MapPreferencesAsync(entity, cancellationToken);
    }

    public async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserPreferences.FirstOrDefaultAsync(item => item.UserId == preferences.UserId, cancellationToken);

        if (entity is null)
        {
            dbContext.UserPreferences.Add(new UserPreferenceEntity
            {
                UserId = preferences.UserId,
                SpiceTolerance = preferences.SpiceTolerance,
                UpdatedAtUtc = preferences.UpdatedAtUtc,
            });
        }
        else
        {
            entity.SpiceTolerance = preferences.SpiceTolerance;
            entity.UpdatedAtUtc = preferences.UpdatedAtUtc;
        }

        dbContext.UserCuisinePreferences.RemoveRange(await dbContext.UserCuisinePreferences.Where(item => item.UserId == preferences.UserId).ToListAsync(cancellationToken));
        dbContext.UserDietaryFlags.RemoveRange(await dbContext.UserDietaryFlags.Where(item => item.UserId == preferences.UserId).ToListAsync(cancellationToken));
        dbContext.UserAllergies.RemoveRange(await dbContext.UserAllergies.Where(item => item.UserId == preferences.UserId).ToListAsync(cancellationToken));

        var cuisineIds = await EnsureCuisineIdsAsync(preferences.CuisineTags, cancellationToken);
        dbContext.UserCuisinePreferences.AddRange(cuisineIds.Select(cuisineId => new UserCuisinePreferenceEntity
        {
            UserId = preferences.UserId,
            CuisineId = cuisineId,
        }));
        dbContext.UserDietaryFlags.AddRange(preferences.DietaryFlags.Select(flag => new UserDietaryFlagEntity
        {
            UserId = preferences.UserId,
            DietaryFlag = flag,
        }));
        dbContext.UserAllergies.AddRange(preferences.Allergies.Select(allergy => new UserAllergyEntity
        {
            UserId = preferences.UserId,
            Allergy = allergy,
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RecurringAvailabilityWindow>> ListRecurringAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.RecurringAvailabilityWindows
            .AsNoTracking()
            .Where(window => window.UserId == userId)
            .ToListAsync(cancellationToken))
        .Select(MapRecurringAvailability)
        .OrderBy(window => window.DayOfWeek)
        .ThenBy(window => window.StartTime)
        .ToArray();

    public async Task<IReadOnlyCollection<OneOffAvailabilityWindow>> ListOneOffAvailabilityAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.OneOffAvailabilityWindows
            .AsNoTracking()
            .Where(window => window.UserId == userId)
            .ToListAsync(cancellationToken))
        .Select(MapOneOffAvailability)
        .OrderBy(window => window.StartsAtUtc)
        .ToArray();

    public async Task<RecurringAvailabilityWindow?> GetRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RecurringAvailabilityWindows
            .AsNoTracking()
            .FirstOrDefaultAsync(window => window.Id == windowId && window.UserId == userId, cancellationToken);
        return entity is null ? null : MapRecurringAvailability(entity);
    }

    public async Task<OneOffAvailabilityWindow?> GetOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.OneOffAvailabilityWindows
            .AsNoTracking()
            .FirstOrDefaultAsync(window => window.Id == windowId && window.UserId == userId, cancellationToken);
        return entity is null ? null : MapOneOffAvailability(entity);
    }

    public async Task SaveRecurringAvailabilityAsync(RecurringAvailabilityWindow window, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RecurringAvailabilityWindows.FirstOrDefaultAsync(item => item.Id == window.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.RecurringAvailabilityWindows.Add(ToEntity(window));
        }
        else
        {
            entity.UserId = window.UserId;
            entity.DayOfWeek = window.DayOfWeek;
            entity.StartTime = window.StartTime;
            entity.EndTime = window.EndTime;
            entity.Label = window.Label;
            entity.CreatedAtUtc = window.CreatedAtUtc;
            entity.UpdatedAtUtc = window.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveOneOffAvailabilityAsync(OneOffAvailabilityWindow window, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.OneOffAvailabilityWindows.FirstOrDefaultAsync(item => item.Id == window.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.OneOffAvailabilityWindows.Add(ToEntity(window));
        }
        else
        {
            entity.UserId = window.UserId;
            entity.StartsAtUtc = window.StartsAtUtc;
            entity.EndsAtUtc = window.EndsAtUtc;
            entity.Label = window.Label;
            entity.CreatedAtUtc = window.CreatedAtUtc;
            entity.UpdatedAtUtc = window.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRecurringAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RecurringAvailabilityWindows.FirstOrDefaultAsync(window => window.Id == windowId && window.UserId == userId, cancellationToken);

        if (entity is not null)
        {
            dbContext.RecurringAvailabilityWindows.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteOneOffAvailabilityAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.OneOffAvailabilityWindows.FirstOrDefaultAsync(window => window.Id == windowId && window.UserId == userId, cancellationToken);

        if (entity is not null)
        {
            dbContext.OneOffAvailabilityWindows.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PrivacySettings?> GetPrivacySettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PrivacySettings.AsNoTracking().FirstOrDefaultAsync(setting => setting.UserId == userId, cancellationToken);
        return entity is null ? null : new PrivacySettings(entity.UserId, entity.DiscoveryEnabled, entity.UpdatedAtUtc);
    }

    public async Task SavePrivacySettingsAsync(PrivacySettings settings, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PrivacySettings.FirstOrDefaultAsync(item => item.UserId == settings.UserId, cancellationToken);

        if (entity is null)
        {
            dbContext.PrivacySettings.Add(new PrivacySettingEntity
            {
                UserId = settings.UserId,
                DiscoveryEnabled = settings.DiscoveryEnabled,
                UpdatedAtUtc = settings.UpdatedAtUtc,
            });
        }
        else
        {
            entity.DiscoveryEnabled = settings.DiscoveryEnabled;
            entity.UpdatedAtUtc = settings.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserBlock>> ListBlocksAsync(Guid blockerUserId, CancellationToken cancellationToken = default) =>
        (await dbContext.UserBlocks
            .AsNoTracking()
            .Where(block => block.BlockerUserId == blockerUserId)
            .ToListAsync(cancellationToken))
        .Select(block => new UserBlock(block.BlockerUserId, block.BlockedUserId, block.CreatedAtUtc))
        .OrderBy(block => block.CreatedAtUtc)
        .ToArray();

    public async Task<UserBlock?> GetBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserBlocks.AsNoTracking().FirstOrDefaultAsync(block => block.BlockerUserId == blockerUserId && block.BlockedUserId == blockedUserId, cancellationToken);
        return entity is null ? null : new UserBlock(entity.BlockerUserId, entity.BlockedUserId, entity.CreatedAtUtc);
    }

    public async Task SaveBlockAsync(UserBlock block, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserBlocks.FirstOrDefaultAsync(item => item.BlockerUserId == block.BlockerUserId && item.BlockedUserId == block.BlockedUserId, cancellationToken);

        if (entity is null)
        {
            dbContext.UserBlocks.Add(new UserBlockEntity
            {
                BlockerUserId = block.BlockerUserId,
                BlockedUserId = block.BlockedUserId,
                CreatedAtUtc = block.CreatedAtUtc,
            });
        }
        else
        {
            entity.CreatedAtUtc = block.CreatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteBlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UserBlocks.FirstOrDefaultAsync(block => block.BlockerUserId == blockerUserId && block.BlockedUserId == blockedUserId, cancellationToken);

        if (entity is not null)
        {
            dbContext.UserBlocks.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<UserPreferences> MapPreferencesAsync(UserPreferenceEntity entity, CancellationToken cancellationToken)
    {
        var cuisineLinks = await dbContext.UserCuisinePreferences
            .AsNoTracking()
            .Where(item => item.UserId == entity.UserId)
            .ToListAsync(cancellationToken);
        var cuisineIds = cuisineLinks.Select(item => item.CuisineId).ToArray();
        IReadOnlyCollection<CuisineEntity> cuisines = cuisineIds.Length == 0
            ? Array.Empty<CuisineEntity>()
            : await dbContext.Cuisines.AsNoTracking().Where(item => cuisineIds.Contains(item.Id)).ToListAsync(cancellationToken);
        var dietaryFlags = await dbContext.UserDietaryFlags
            .AsNoTracking()
            .Where(item => item.UserId == entity.UserId)
            .Select(item => item.DietaryFlag)
            .ToListAsync(cancellationToken);
        var allergies = await dbContext.UserAllergies
            .AsNoTracking()
            .Where(item => item.UserId == entity.UserId)
            .Select(item => item.Allergy)
            .ToListAsync(cancellationToken);

        return new UserPreferences(
            entity.UserId,
            cuisines.Select(item => item.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
            entity.SpiceTolerance,
            dietaryFlags.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray(),
            allergies.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray(),
            entity.UpdatedAtUtc);
    }

    private async Task<Guid[]> EnsureCuisineIdsAsync(IEnumerable<string> cuisineNames, CancellationToken cancellationToken)
    {
        var normalizedNames = cuisineNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedNames.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        var existing = await dbContext.Cuisines
            .Where(cuisine => normalizedNames.Contains(cuisine.Name))
            .ToListAsync(cancellationToken);
        var result = existing.ToDictionary(item => item.Name, item => item.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var name in normalizedNames)
        {
            if (result.ContainsKey(name))
            {
                continue;
            }

            var entity = new CuisineEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
            };

            dbContext.Cuisines.Add(entity);
            result[name] = entity.Id;
        }

        return normalizedNames.Select(name => result[name]).ToArray();
    }

    private static UserProfile MapProfile(UserProfileEntity entity) =>
        new(
            entity.UserId,
            entity.DisplayName,
            entity.Bio,
            entity.HomeAreaZipCode,
            entity.SocialGoal,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static RecurringAvailabilityWindow MapRecurringAvailability(RecurringAvailabilityWindowEntity entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.DayOfWeek,
            entity.StartTime,
            entity.EndTime,
            entity.Label,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static OneOffAvailabilityWindow MapOneOffAvailability(OneOffAvailabilityWindowEntity entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.StartsAtUtc,
            entity.EndsAtUtc,
            entity.Label,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static UserProfileEntity ToEntity(UserProfile profile) =>
        new()
        {
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            HomeAreaZipCode = profile.HomeAreaZipCode,
            SocialGoal = profile.SocialGoal,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
        };

    private static RecurringAvailabilityWindowEntity ToEntity(RecurringAvailabilityWindow window) =>
        new()
        {
            Id = window.Id,
            UserId = window.UserId,
            DayOfWeek = window.DayOfWeek,
            StartTime = window.StartTime,
            EndTime = window.EndTime,
            Label = window.Label,
            CreatedAtUtc = window.CreatedAtUtc,
            UpdatedAtUtc = window.UpdatedAtUtc,
        };

    private static OneOffAvailabilityWindowEntity ToEntity(OneOffAvailabilityWindow window) =>
        new()
        {
            Id = window.Id,
            UserId = window.UserId,
            StartsAtUtc = window.StartsAtUtc,
            EndsAtUtc = window.EndsAtUtc,
            Label = window.Label,
            CreatedAtUtc = window.CreatedAtUtc,
            UpdatedAtUtc = window.UpdatedAtUtc,
        };
}
