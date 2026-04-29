using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
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
    public int RestaurantCatalogTotalCount { get; init; }
    public IReadOnlyCollection<PasswordResetRequestDto> OpenPasswordResetRequests { get; init; } = [];
    public PasswordResetTokenDto? GeneratedPasswordResetToken { get; init; }
}
