using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class GroupAnnouncementItem
{
    public Guid AnnouncementId { get; init; }
    public Guid GroupId { get; init; }
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string AuthorUsername { get; init; } = string.Empty;
    public GroupAnnouncementType AnnouncementType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Guid? RelatedEventId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public bool IsEventAnnouncement => AnnouncementType == GroupAnnouncementType.EventCreated;
    public string TypeLabel => IsEventAnnouncement ? "Event update" : "Owner post";
    public string CreatedLabel => CreatedAtUtc.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);

    public static GroupAnnouncementItem FromDto(GroupAnnouncementDto dto) => new()
    {
        AnnouncementId = dto.AnnouncementId,
        GroupId = dto.GroupId,
        AuthorDisplayName = dto.AuthorDisplayName,
        AuthorUsername = dto.AuthorUsername,
        AnnouncementType = dto.AnnouncementType,
        Title = dto.Title,
        Body = dto.Body,
        RelatedEventId = dto.RelatedEventId,
        CreatedAtUtc = dto.CreatedAtUtc,
    };
}
