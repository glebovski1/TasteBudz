using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Discovery;

public sealed class SearchPeopleQuery
{
    public string? Q { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
