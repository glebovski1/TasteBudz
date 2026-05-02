using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class AdminReportDetailViewModel
{
    public ModerationReportReviewDto Review { get; init; } = null!;

    public ModerationReportDto Report => Review.Report;

    public ModerationUserSummaryDto? Reporter => Review.Reporter;

    public ModerationUserSummaryDto? SubjectUser => Review.SubjectUser;

    public ModerationUserSummaryDto? RelatedUser => Review.RelatedUser;

    public ModerationSearchResultDto? RelatedMessage => Review.RelatedMessage;
}
