// Generic cursor-based paging envelope for endpoints that may move beyond simple page numbers later.
namespace TasteBudz.Backend.Contracts;

/// <summary>
/// Wraps a page of items together with the opaque cursor needed to fetch the next page.
/// </summary>
public sealed record CursorPageResponse<T>(IReadOnlyCollection<T> Items, string? NextCursor);