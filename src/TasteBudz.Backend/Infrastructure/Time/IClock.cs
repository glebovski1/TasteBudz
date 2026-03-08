// Small abstraction over UTC time so services stay deterministic in tests.
namespace TasteBudz.Backend.Infrastructure.Time;

/// <summary>
/// Supplies the current UTC timestamp to business workflows.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}