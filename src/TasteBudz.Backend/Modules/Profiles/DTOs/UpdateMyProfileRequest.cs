using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed class UpdateMyProfileRequest
{
    [MinLength(3)]
    [MaxLength(32)]
    public string? Username { get; init; }

    [MinLength(1)]
    [MaxLength(64)]
    public string? DisplayName { get; init; }

    [MaxLength(500)]
    public string? Bio { get; init; }

    [RegularExpression("^[0-9]{5}$")]
    public string? HomeAreaZipCode { get; init; }

    public SocialGoal? SocialGoal { get; init; }
}
