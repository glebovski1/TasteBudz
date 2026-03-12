using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed record GroupSummaryDto(
    Guid GroupId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    int ActiveMembers);
