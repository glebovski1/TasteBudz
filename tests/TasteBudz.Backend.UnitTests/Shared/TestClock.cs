// Mutable test clock used to make time-sensitive services deterministic.
using TasteBudz.Backend.Infrastructure.Time;

namespace TasteBudz.Backend.UnitTests.Shared;

/// <summary>
/// Test double for <see cref="IClock"/> that lets tests pin or advance UTC time manually.
/// </summary>
public sealed class TestClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}