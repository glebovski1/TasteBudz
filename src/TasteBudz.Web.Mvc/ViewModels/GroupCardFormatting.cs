using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


internal static class GroupCardFormatting
{
    public static string GetInitial(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "?"
            : value.Trim()[0].ToString().ToUpperInvariant();

    public static string? ToMediaUrl(Guid? mediaAssetId) =>
        mediaAssetId.HasValue
            ? $"/media/{mediaAssetId.Value}"
            : null;

    public static string? GetSocialGoalLabel(SocialGoal? socialGoal) => socialGoal switch
    {
        SocialGoal.Friends => "Friends",
        SocialGoal.Dating => "Dating",
        SocialGoal.Networking => "Networking",
        _ => null,
    };

    public static string? GetZipLabel(string? homeAreaZipCode) =>
        string.IsNullOrWhiteSpace(homeAreaZipCode)
            ? null
            : $"ZIP {homeAreaZipCode}";

    public static IReadOnlyList<string> BuildPublicFoodTags(
        IEnumerable<string>? cuisineTags,
        IEnumerable<string>? dietaryFlags)
    {
        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRange(cuisineTags, seen, tags);
        AddRange(dietaryFlags, seen, tags);

        return tags;
    }

    public static string GetPersonalityText(string? bio) =>
        string.IsNullOrWhiteSpace(bio)
            ? "No personality note yet."
            : Truncate(bio, 140);

    public static string GetSocialGoalDescription(SocialGoal? socialGoal) => socialGoal switch
    {
        SocialGoal.Friends => "Looking for new foodie friends",
        SocialGoal.Dating => "Open to dinner dates",
        SocialGoal.Networking => "Interested in local networking over food",
        _ => "Open to new TasteBudz connections",
    };

    public static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return $"{trimmed[..Math.Max(0, maxLength - 1)].TrimEnd()}...";
    }

    private static void AddRange(
        IEnumerable<string>? values,
        ISet<string> seen,
        ICollection<string> destination)
    {
        if (values is null)
        {
            return;
        }

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalized = value.Trim();
            if (seen.Add(normalized))
            {
                destination.Add(normalized);
            }
        }
    }
}
