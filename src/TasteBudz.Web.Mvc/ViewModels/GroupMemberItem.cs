using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class GroupMemberItem
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public SocialGoal? SocialGoal { get; init; }
    public string? HomeAreaZipCode { get; init; }
    public Guid? AvatarMediaAssetId { get; init; }
    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> DietaryFlags { get; init; } = Array.Empty<string>();
    public bool IsOwner { get; init; }
    public DateTimeOffset JoinedAtUtc { get; init; }

    public string Initial => GroupCardFormatting.GetInitial(DisplayName);

    public string? AvatarUrl => GroupCardFormatting.ToMediaUrl(AvatarMediaAssetId);

    public string? GoalLabel => GroupCardFormatting.GetSocialGoalLabel(SocialGoal);

    public string? ZipLabel => GroupCardFormatting.GetZipLabel(HomeAreaZipCode);

    public string RoleLabel => IsOwner ? "Owner" : "Member";

    public string JoinedChipLabel => $"Joined {JoinedAtUtc.ToLocalTime().ToString("MMM d", CultureInfo.InvariantCulture)}";

    public string JoinedLabel => $"Joined {JoinedAtUtc.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";

    public IReadOnlyList<string> PublicFoodTags => GroupCardFormatting.BuildPublicFoodTags(CuisineTags, DietaryFlags);

    public IReadOnlyList<string> PreviewFoodTags => PublicFoodTags.Take(6).ToArray();

    public int HiddenFoodTagCount => Math.Max(0, PublicFoodTags.Count - PreviewFoodTags.Count);

    public string PersonalityText => GroupCardFormatting.GetPersonalityText(Bio);

    public string GoalsText => GroupCardFormatting.GetSocialGoalDescription(SocialGoal);

    public static GroupMemberItem FromDto(GroupMemberDto dto, Guid ownerUserId) => new()
    {
        UserId = dto.UserId,
        DisplayName = dto.DisplayName,
        Username = dto.Username,
        Bio = dto.Bio,
        SocialGoal = dto.SocialGoal,
        HomeAreaZipCode = dto.HomeAreaZipCode,
        AvatarMediaAssetId = dto.AvatarMediaAssetId,
        CuisineTags = dto.CuisineTags,
        DietaryFlags = dto.DietaryFlags,
        IsOwner = dto.UserId == ownerUserId,
        JoinedAtUtc = dto.JoinedAtUtc,
    };
}
