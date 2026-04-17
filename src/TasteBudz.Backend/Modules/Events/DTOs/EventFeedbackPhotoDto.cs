namespace TasteBudz.Backend.Modules.Events;

public sealed record EventFeedbackPhotoDto(
    Guid MediaAssetId,
    string OriginalFileName,
    string ContentType,
    long ContentLength,
    DateTimeOffset CreatedAtUtc);
