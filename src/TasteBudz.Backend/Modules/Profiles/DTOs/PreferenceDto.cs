using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed record PreferenceDto(
    IReadOnlyCollection<string> CuisineTags,
    SpiceTolerance? SpiceTolerance,
    IReadOnlyCollection<string> DietaryFlags,
    IReadOnlyCollection<string> Allergies);
