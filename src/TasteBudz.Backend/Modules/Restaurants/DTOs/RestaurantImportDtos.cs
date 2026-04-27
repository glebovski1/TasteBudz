using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

public class RestaurantImportPreviewQuery
{
    public string? Preset { get; init; } = "cincinnati";

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; init; } = "45202";

    [Range(1, 50)]
    public double? RadiusMiles { get; init; } = 25;

    public double? South { get; init; }

    public double? West { get; init; }

    public double? North { get; init; }

    public double? East { get; init; }
}

public sealed class CommitRestaurantImportRequest : RestaurantImportPreviewQuery
{
    public IReadOnlyCollection<string> SelectedExternalPlaceIds { get; init; } = [];
}

public sealed record RestaurantImportPreviewDto(
    RestaurantImportGeographyDto Geography,
    IReadOnlyCollection<RestaurantImportCandidateDto> Candidates,
    int CandidateCount,
    int ImportableCount,
    int DuplicateCount);

public sealed record RestaurantImportGeographyDto(
    string Label,
    double South,
    double West,
    double North,
    double East,
    double? CenterLatitude,
    double? CenterLongitude,
    double? RadiusMiles);

public sealed record RestaurantImportCandidateDto(
    string ExternalPlaceId,
    string Name,
    string? StreetAddress,
    string City,
    string State,
    string ZipCode,
    string CuisineText,
    IReadOnlyCollection<string> CuisineTags,
    double Latitude,
    double Longitude,
    bool IsDuplicate,
    string? DuplicateReason,
    Guid? MatchingRestaurantId,
    string? MatchingRestaurantName);
