using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed record AdminIndexViewModel
{
    public IReadOnlyCollection<ModerationReportDto> PendingReports { get; init; } = [];
    public bool RestaurantOperationsAvailable { get; init; }
    public IReadOnlyCollection<RestaurantAssignmentPanelItem> RestaurantAssignments { get; init; } = [];
    public PasswordResetTokenDto? GeneratedPasswordResetToken { get; init; }
}

public sealed class AdminReportsViewModel
{
    public IReadOnlyCollection<ModerationReportDto> Reports { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalCount { get; init; }
    public ModerationReportStatus? FilterStatus { get; init; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / 20.0);
}

public sealed class AdminReportDetailViewModel
{
    public ModerationReportDto Report { get; init; } = null!;
}

public sealed class RestaurantAssignmentPanelItem
{
    public RestaurantDto Restaurant { get; init; } = null!;
    public IReadOnlyCollection<RestaurantAdminAssignmentDto> Assignments { get; init; } = [];
}

public sealed class AdminSupportThreadsViewModel
{
    public IReadOnlyCollection<SupportThreadDto> Threads { get; init; } = [];
}
