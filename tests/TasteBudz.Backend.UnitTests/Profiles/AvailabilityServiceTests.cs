// Unit tests for availability update edge cases.
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;

/// <summary>
/// Verifies that availability updates do not silently create new windows for missing ids.
/// </summary>
public sealed class AvailabilityServiceTests
{
    /// <summary>
    /// Confirms that a recurring PATCH behaves like an edit and not like an accidental create.
    /// </summary>
    [Fact]
    public async Task UpsertRecurringAsync_WithUnknownWindowId_ReturnsNotFound()
    {
        // No seed data is created here on purpose; the missing id is the behavior under test.
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpsertRecurringAsync(Guid.NewGuid(), Guid.NewGuid(), new UpsertRecurringAvailabilityWindowRequest
            {
                DayOfWeek = DayOfWeek.Friday,
                StartTime = new TimeOnly(18, 0),
                EndTime = new TimeOnly(21, 0),
                Label = "Friday dinner",
            }));

        Assert.Equal(404, exception.StatusCode);
    }

    /// <summary>
    /// Confirms that a one-off PATCH behaves like an edit and not like an accidental create.
    /// </summary>
    [Fact]
    public async Task UpsertOneOffAsync_WithUnknownWindowId_ReturnsNotFound()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpsertOneOffAsync(Guid.NewGuid(), Guid.NewGuid(), new UpsertOneOffAvailabilityWindowRequest
            {
                StartsAtUtc = new DateTimeOffset(2026, 3, 14, 18, 0, 0, TimeSpan.Zero),
                EndsAtUtc = new DateTimeOffset(2026, 3, 14, 21, 0, 0, TimeSpan.Zero),
                Label = "Saturday dinner",
            }));

        Assert.Equal(404, exception.StatusCode);
    }

    /// <summary>
    /// Builds the smallest deterministic service graph needed for availability-rule tests.
    /// </summary>
    private static AvailabilityService CreateService()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        return new AvailabilityService(new InMemoryProfileRepository(store), new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero)));
    }
}
