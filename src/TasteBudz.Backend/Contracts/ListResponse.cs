// Generic page-number-based list envelope used by the current browse endpoints.
namespace TasteBudz.Backend.Contracts;

/// <summary>
/// Wraps a page of items together with the full filtered result count.
/// </summary>
public sealed record ListResponse<T>(IReadOnlyCollection<T> Items, int TotalCount);