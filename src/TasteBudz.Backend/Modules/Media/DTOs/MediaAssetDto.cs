namespace TasteBudz.Backend.Modules.Media;

public sealed record MediaAssetDto(
    Guid MediaAssetId,
    string OriginalFileName,
    string ContentType,
    long ContentLength,
    DateTimeOffset CreatedAtUtc);
