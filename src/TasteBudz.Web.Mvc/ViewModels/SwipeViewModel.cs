using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;

namespace TasteBudz.Web.Mvc.ViewModels;

/// <summary>
/// Page model for the BudzSwipe discovery view.
/// Carries the initial batch of swipe candidates rendered server-side,
/// with the UserId exposed so the JS layer can POST swipe decisions back.
/// </summary>
public sealed class SwipeViewModel
{
    public IReadOnlyList<SwipeCandidateItem> Candidates { get; init; } = [];

    public static SwipeViewModel FromDto(IEnumerable<DiscoveryProfilePreviewDto> candidates) =>
        new() { Candidates = candidates.Select(SwipeCandidateItem.FromDto).ToList() };
}
