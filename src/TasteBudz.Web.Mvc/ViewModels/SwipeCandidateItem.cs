using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class SwipeCandidateItem
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public SocialGoal? SocialGoal { get; init; }
    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> DietaryFlags { get; init; } = Array.Empty<string>();
    public string? GoalLabel => SocialGoal switch
    {
        TasteBudz.Backend.Domain.SocialGoal.Friends => "Friends",
        TasteBudz.Backend.Domain.SocialGoal.Dating => "Dating",
        TasteBudz.Backend.Domain.SocialGoal.Networking => "Networking",
        _ => null,
    };

    public string GoalsText => SocialGoal switch
    {
        TasteBudz.Backend.Domain.SocialGoal.Friends => "Looking for new foodie friends",
        TasteBudz.Backend.Domain.SocialGoal.Dating => "Open to dinner dates",
        TasteBudz.Backend.Domain.SocialGoal.Networking => "Interested in local networking over food",
        _ => "Open to new TasteBudz connections",
    };

    public IReadOnlyList<string> PublicFoodTags => BuildPublicFoodTags(CuisineTags, DietaryFlags);

    public IReadOnlyList<string> PreviewFoodTags => PublicFoodTags.Take(5).ToArray();

    public int HiddenFoodTagCount => Math.Max(0, PublicFoodTags.Count - PreviewFoodTags.Count);

    public string PersonalityText => string.IsNullOrWhiteSpace(Bio)
        ? "No personality note yet."
        : Truncate(Bio, 120);

    public static SwipeCandidateItem FromDto(DiscoveryProfilePreviewDto dto) =>
        new()
        {
            UserId     = dto.UserId,
            DisplayName = dto.DisplayName,
            Username   = dto.Username,
            Bio        = dto.Bio,
            SocialGoal = dto.SocialGoal,
            CuisineTags = dto.CuisineTags,
            DietaryFlags = dto.DietaryFlags,
        };

    private static IReadOnlyList<string> BuildPublicFoodTags(
        IEnumerable<string>? cuisineTags,
        IEnumerable<string>? dietaryFlags)
    {
        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRange(cuisineTags, seen, tags);
        AddRange(dietaryFlags, seen, tags);

        return tags;
    }

    private static void AddRange(IEnumerable<string>? values, HashSet<string> seen, List<string> tags)
    {
        if (values is null)
        {
            return;
        }

        foreach (var value in values)
        {
            var trimmed = value?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && seen.Add(trimmed))
            {
                tags.Add(trimmed);
            }
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..(maxLength - 1)] + "...";
    }
}
