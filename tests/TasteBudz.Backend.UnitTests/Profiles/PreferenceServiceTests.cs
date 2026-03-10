// Unit tests for preference normalization and default-read behavior.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;

/// <summary>
/// Verifies that preference persistence stays deterministic for downstream browse/recommendation logic.
/// </summary>
public sealed class PreferenceServiceTests
{
    [Fact]
    public async Task GetAsync_WhenNoPreferencesExist_ReturnsEmptyDefaults()
    {
        var service = CreateService();

        var preferences = await service.GetAsync(Guid.NewGuid());

        Assert.Empty(preferences.CuisineTags);
        Assert.Null(preferences.SpiceTolerance);
        Assert.Empty(preferences.DietaryFlags);
        Assert.Empty(preferences.Allergies);
    }

    [Fact]
    public async Task ReplaceAsync_NormalizesDistinctValuesBeforePersisting()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var updated = await service.ReplaceAsync(userId, new ReplacePreferencesRequest
        {
            CuisineTags = new[] { " sushi ", "Thai", "SUSHI", "", "thai" },
            SpiceTolerance = SpiceTolerance.Medium,
            DietaryFlags = new[] { " Vegetarian ", "vegetarian", "Pescatarian" },
            Allergies = new[] { "Peanuts", " peanuts ", "Shellfish" },
        });

        Assert.Equal(new[] { "sushi", "Thai" }, updated.CuisineTags);
        Assert.Equal(SpiceTolerance.Medium, updated.SpiceTolerance);
        Assert.Equal(new[] { "Pescatarian", "Vegetarian" }, updated.DietaryFlags);
        Assert.Equal(new[] { "Peanuts", "Shellfish" }, updated.Allergies);
    }

    private static PreferenceService CreateService()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        return new PreferenceService(
            new InMemoryProfileRepository(store),
            new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero)));
    }
}
