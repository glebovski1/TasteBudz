using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class ProfileEditViewModel
{
    public static IReadOnlyList<string> AvailableCuisineTags { get; } = new[]
    {
        "American",
        "Brazilian",
        "Caribbean",
        "Chinese",
        "French",
        "German",
        "Greek",
        "Indian",
        "Italian",
        "Jamaican",
        "Japanese",
        "Korean",
        "Mediterranean",
        "Mexican",
        "Seafood",
        "Spanish",
        "Tex-Mex",
        "Thai",
        "Vietnamese",
    };

    public static IReadOnlyList<string> AvailableDietaryFlags { get; } = new[]
    {
        "Vegetarian",
        "Vegan",
        "Gluten Free",
        "Dairy Free",
        "Halal",
        "Kosher",
    };

    public static IReadOnlyList<SelectListItem> SocialGoalOptions { get; } =
        Enum.GetValues<SocialGoal>()
            .Select(goal => new SelectListItem(goal.ToString(), goal.ToString()))
            .ToArray();

    public static IReadOnlyList<SelectListItem> SpiceToleranceOptions { get; } =
        Enum.GetValues<SpiceTolerance>()
            .Select(level => new SelectListItem(level.ToString(), level.ToString()))
            .ToArray();

    [Required]
    [MinLength(3)]
    [MaxLength(32)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(64)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    [DataType(DataType.MultilineText)]
    public string? Bio { get; set; }

    [Required]
    [RegularExpression("^[0-9]{5}$", ErrorMessage = "ZIP code must be a 5-digit value.")]
    [Display(Name = "Home ZIP Code")]
    public string HomeAreaZipCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Social Goal")]
    public SocialGoal? SocialGoal { get; set; }

    [Required]
    [Display(Name = "Spice Tolerance")]
    public SpiceTolerance? SpiceTolerance { get; set; }

    [Display(Name = "Favorite Cuisines")]
    public List<string> SelectedCuisineTags { get; set; } = [];

    [Display(Name = "Dietary Flags")]
    public List<string> SelectedDietaryFlags { get; set; } = [];

    [Display(Name = "Allergies")]
    public string? AllergiesText { get; set; }

    [Display(Name = "Allow people discovery")]
    public bool DiscoveryEnabled { get; set; } = true;

    public bool IsOnboardingFlow { get; set; }

    public static ProfileEditViewModel FromDto(
        ProfileDto profile,
        PreferenceDto preferences,
        PrivacySettingsDto privacySettings,
        OnboardingStatusDto onboardingStatus) =>
        new()
        {
            Username = profile.Username,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            HomeAreaZipCode = profile.HomeAreaZipCode,
            SocialGoal = profile.SocialGoal,
            SpiceTolerance = preferences.SpiceTolerance,
            SelectedCuisineTags = preferences.CuisineTags.ToList(),
            SelectedDietaryFlags = preferences.DietaryFlags.ToList(),
            AllergiesText = string.Join(", ", preferences.Allergies),
            DiscoveryEnabled = privacySettings.DiscoveryEnabled,
            IsOnboardingFlow = !onboardingStatus.IsComplete,
        };

    public UpdateMyProfileRequest ToProfileRequest() =>
        new()
        {
            Username = Username.Trim(),
            DisplayName = DisplayName.Trim(),
            Bio = string.IsNullOrWhiteSpace(Bio) ? null : Bio.Trim(),
            HomeAreaZipCode = HomeAreaZipCode.Trim(),
            SocialGoal = SocialGoal,
        };

    public ReplacePreferencesRequest ToPreferenceRequest() =>
        new()
        {
            CuisineTags = SelectedCuisineTags,
            SpiceTolerance = SpiceTolerance,
            DietaryFlags = SelectedDietaryFlags,
            Allergies = SplitList(AllergiesText),
        };

    public UpdatePrivacySettingsRequest ToPrivacyRequest() =>
        new()
        {
            DiscoveryEnabled = DiscoveryEnabled,
        };

    public void NormalizeSelections()
    {
        SelectedCuisineTags = NormalizeList(SelectedCuisineTags);
        SelectedDietaryFlags = NormalizeList(SelectedDietaryFlags);
    }

    private static List<string> NormalizeList(IEnumerable<string>? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? [];

    private static IReadOnlyCollection<string> SplitList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value
                .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
}
