// Production clock implementation that simply forwards to DateTimeOffset.UtcNow.
namespace TasteBudz.Backend.Infrastructure.Time;

/// <summary>
/// Default runtime clock used outside of tests.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}