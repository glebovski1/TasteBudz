using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class DashboardViewModel
{
    public required string Username { get; init; }

    public required string DisplayName { get; init; }

    public required string Email { get; init; }

    public string? Bio { get; init; }

    public required string HomeAreaZipCode { get; init; }

    public SocialGoal? SocialGoal { get; init; }

    public IReadOnlyCollection<DashboardEventCardViewModel> ActiveEvents { get; init; } = Array.Empty<DashboardEventCardViewModel>();

    public IReadOnlyCollection<DashboardGroupCardViewModel> ActiveGroups { get; init; } = Array.Empty<DashboardGroupCardViewModel>();

    public IReadOnlyCollection<DashboardBudCardViewModel> Budz { get; init; } = Array.Empty<DashboardBudCardViewModel>();

    public static DashboardViewModel FromDto(DashboardDto dto) =>
        new()
        {
            Username = dto.Profile.Username,
            DisplayName = dto.Profile.DisplayName,
            Email = dto.Profile.Email,
            Bio = dto.Profile.Bio,
            HomeAreaZipCode = dto.Profile.HomeAreaZipCode,
            SocialGoal = dto.Profile.SocialGoal,
            ActiveEvents = dto.ActiveEvents
                .Select(item => new DashboardEventCardViewModel(item.EventId, item.Title ?? "Untitled Event", item.Status, item.EventStartAtUtc))
                .ToArray(),
            ActiveGroups = dto.ActiveGroups
                .Select(item => new DashboardGroupCardViewModel(item.GroupId, item.Name, item.Visibility))
                .ToArray(),
            Budz = dto.Budz
                .Select(item => new DashboardBudCardViewModel(item.UserId, item.Username, item.DisplayName))
                .ToArray(),
        };
}

public sealed record DashboardEventCardViewModel(
    Guid EventId,
    string Title,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc);

public sealed record DashboardGroupCardViewModel(
    Guid GroupId,
    string Name,
    GroupVisibility Visibility);

public sealed record DashboardBudCardViewModel(
    Guid UserId,
    string Username,
    string DisplayName);
