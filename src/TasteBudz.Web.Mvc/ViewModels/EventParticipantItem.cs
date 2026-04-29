using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class EventParticipantItem
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public bool IsHost { get; init; }
    public DateTimeOffset? JoinedAtUtc { get; init; }
    public string Initial => string.IsNullOrWhiteSpace(DisplayName)
        ? "?"
        : DisplayName.Trim()[0].ToString().ToUpperInvariant();
    public string RoleLabel => IsHost ? "Host" : "Attendee";
    public string JoinedLabel => JoinedAtUtc.HasValue
        ? $"Joined {JoinedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}"
        : "Joined event";

    public static EventParticipantItem FromDto(EventParticipantDto dto, Guid hostUserId) => new()
    {
        UserId = dto.UserId,
        DisplayName = dto.DisplayName,
        Username = dto.Username,
        IsHost = dto.UserId == hostUserId,
        JoinedAtUtc = dto.JoinedAtUtc,
    };
}
