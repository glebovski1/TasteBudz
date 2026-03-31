using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class AdminIndexViewModel
{
    public IReadOnlyCollection<ModerationReportDto> PendingReports { get; init; } = [];
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