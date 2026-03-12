namespace TasteBudz.Backend.Modules.Discovery;

public sealed record BudConnectionDto(
    Guid UserId,
    string Username,
    string DisplayName,
    DateTimeOffset ConnectedAtUtc);
