namespace TasteBudz.Backend.Domain;

/// <summary>
/// Simulation-only checkout session for one event participant.
/// </summary>
public sealed record CheckoutSession(
    Guid Id,
    Guid EventId,
    Guid UserId,
    CheckoutSessionStatus Status,
    string Currency,
    int SubtotalCents,
    int DiscountCents,
    int TotalCents,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc);
