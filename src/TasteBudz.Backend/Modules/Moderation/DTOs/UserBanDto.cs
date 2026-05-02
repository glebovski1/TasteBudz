namespace TasteBudz.Backend.Modules.Moderation;

public sealed record UserBanDto(
    Guid SubjectUserId,
    IReadOnlyCollection<RestrictionDto> Restrictions,
    ModerationReportDto? ResolvedReport);
