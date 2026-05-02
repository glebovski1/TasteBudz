using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Moderation;

public enum ModerationSearchResultKind
{
    User,
    Message,
    Report,
    Event,
    Group,
    Feedback,
    Restaurant,
    Audit,
}

public sealed class ModerationSearchQuery
{
    [MaxLength(120)]
    public string? Q { get; init; }

    public ModerationSearchResultKind? Type { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record ModerationUserSummaryDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string Email,
    AccountStatus Status,
    IReadOnlyCollection<UserRole> Roles);

public sealed record ModerationSearchResultDto(
    ModerationSearchResultKind Kind,
    Guid Id,
    string Title,
    string? Subtitle,
    string? Snippet,
    DateTimeOffset? CreatedAtUtc,
    ModerationUserSummaryDto? PrimaryUser,
    ModerationUserSummaryDto? SecondaryUser,
    string? ContextType,
    Guid? ContextId);

public sealed record ModerationSearchResponseDto(
    string? Query,
    ModerationSearchResultKind? Type,
    IReadOnlyCollection<ModerationSearchResultDto> Items,
    int TotalCount);

public sealed record ModerationReportReviewDto(
    ModerationReportDto Report,
    ModerationUserSummaryDto? Reporter,
    ModerationUserSummaryDto? SubjectUser,
    ModerationUserSummaryDto? RelatedUser,
    ModerationSearchResultDto? RelatedMessage);

public sealed record ModerationUserDetailDto(
    ModerationUserSummaryDto User,
    IReadOnlyCollection<RestrictionDto> Restrictions,
    int ReportCount,
    int MessageCount,
    DateTimeOffset? LastMessageAtUtc);
