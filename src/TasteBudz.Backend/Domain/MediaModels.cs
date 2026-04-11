// Database-backed media records shared across profile and moderation flows.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Stored image asset owned by one user and linked to one bounded context.
/// </summary>
public sealed record MediaAsset(
    Guid Id,
    Guid OwnerUserId,
    Guid? ProfileUserId,
    Guid? GroupId,
    Guid? EventId,
    Guid? ReportId,
    string OriginalFileName,
    string ContentType,
    long ContentLength,
    byte[] Content,
    DateTimeOffset CreatedAtUtc);
