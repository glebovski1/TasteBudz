using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;



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
