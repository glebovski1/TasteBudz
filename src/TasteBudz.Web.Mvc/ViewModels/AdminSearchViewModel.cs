using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class AdminSearchViewModel
{
    public const int PageSize = 20;

    public string? Query { get; init; }
    public ModerationSearchResultKind? Type { get; init; }
    public IReadOnlyCollection<ModerationSearchResultDto> Results { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalCount { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
