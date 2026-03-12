using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed class ReplacePreferencesRequest
{
    public IReadOnlyCollection<string> CuisineTags { get; init; } = Array.Empty<string>();

    public SpiceTolerance? SpiceTolerance { get; init; }

    public IReadOnlyCollection<string> DietaryFlags { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Allergies { get; init; } = Array.Empty<string>();
}
