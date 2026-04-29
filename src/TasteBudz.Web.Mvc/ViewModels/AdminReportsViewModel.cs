using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class AdminReportsViewModel
{
    public IReadOnlyCollection<ModerationReportDto> Reports { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalCount { get; init; }
    public ModerationReportStatus? FilterStatus { get; init; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / 20.0);
}
