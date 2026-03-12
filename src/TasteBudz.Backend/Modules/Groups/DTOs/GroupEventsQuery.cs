using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Groups;

public sealed class GroupEventsQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
