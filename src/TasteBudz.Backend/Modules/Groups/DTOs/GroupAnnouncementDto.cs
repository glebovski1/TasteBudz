using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed record GroupAnnouncementDto(
    Guid AnnouncementId,
    Guid GroupId,
    Guid AuthorUserId,
    string AuthorUsername,
    string AuthorDisplayName,
    GroupAnnouncementType AnnouncementType,
    string Title,
    string Body,
    Guid? RelatedEventId,
    DateTimeOffset CreatedAtUtc);
