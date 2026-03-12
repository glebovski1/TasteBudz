using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

public sealed record RestrictionDto(
    Guid RestrictionId,
    Guid SubjectUserId,
    RestrictionScope Scope,
    string Reason,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    RestrictionStatus Status,
    DateTimeOffset? RevokedAtUtc);
