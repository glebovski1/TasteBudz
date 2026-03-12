using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Moderation;

public sealed class AuditLogQuery
{
    public Guid? ActorUserId { get; init; }

    public string? TargetEntityType { get; init; }

    public Guid? TargetEntityId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
