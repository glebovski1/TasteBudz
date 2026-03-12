namespace TasteBudz.Backend.Modules.Profiles;

public sealed record OneOffAvailabilityWindowDto(
    Guid Id,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Label);
