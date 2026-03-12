using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Messaging;

public sealed class ChatHistoryQuery
{
    public string? Cursor { get; init; }

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
