// Request and response contracts for people discovery and Budz.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Discovery;

/// <summary>
/// Limited public profile preview returned by discovery APIs.
/// </summary>
public sealed record DiscoveryProfilePreviewDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Bio,
    SocialGoal? SocialGoal);

/// <summary>
/// Query parameters for discovery search.
/// </summary>
public sealed class SearchPeopleQuery
{
    public string? Q { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Query parameters for deterministic swipe-candidate retrieval.
/// </summary>
public sealed class SwipeCandidatesQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Request body for recording one directional swipe decision.
/// </summary>
public sealed class RecordSwipeDecisionRequest
{
    [Required]
    public Guid? SubjectUserId { get; init; }

    [Required]
    public SwipeDecisionType? Decision { get; init; }
}

/// <summary>
/// Result payload returned by the swipe endpoint.
/// </summary>
public sealed record SwipeDecisionResultDto(
    Guid SubjectUserId,
    SwipeDecisionType Decision,
    bool IsBudMatch,
    Guid? BudConnectionId);

/// <summary>
/// Bud connection payload returned by the Budz list endpoint.
/// </summary>
public sealed record BudConnectionDto(
    Guid UserId,
    string Username,
    string DisplayName,
    DateTimeOffset ConnectedAtUtc);
