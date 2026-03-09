// Unit tests for privacy settings patch behavior.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;

/// <summary>
/// Verifies that privacy updates preserve existing values when fields are omitted.
/// </summary>
public sealed class PrivacyServiceTests
{
    [Fact]
    public async Task UpdateAsync_WhenDiscoveryEnabledIsOmitted_PreservesCurrentValue()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var profileRepository = new InMemoryProfileRepository(store);
        var clock = new TestClock(new DateTimeOffset(2026, 3, 9, 18, 0, 0, TimeSpan.Zero));
        var service = new PrivacyService(profileRepository, clock);
        var userId = Guid.NewGuid();

        await profileRepository.SavePrivacySettingsAsync(new PrivacySettings(userId, false, clock.UtcNow.AddMinutes(-10)));

        var updated = await service.UpdateAsync(userId, new UpdatePrivacySettingsRequest());

        Assert.False(updated.DiscoveryEnabled);
    }
}
