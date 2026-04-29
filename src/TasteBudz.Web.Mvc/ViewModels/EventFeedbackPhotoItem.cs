using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class EventFeedbackPhotoItem
{
    public Guid MediaAssetId { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long ContentLength { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }

    public static EventFeedbackPhotoItem FromDto(EventFeedbackPhotoDto dto) => new()
    {
        MediaAssetId = dto.MediaAssetId,
        OriginalFileName = dto.OriginalFileName,
        ContentType = dto.ContentType,
        ContentLength = dto.ContentLength,
        CreatedAtUtc = dto.CreatedAtUtc,
    };
}
