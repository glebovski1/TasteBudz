using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Payments;

public sealed record CheckoutSessionDto(
    Guid CheckoutSessionId,
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
