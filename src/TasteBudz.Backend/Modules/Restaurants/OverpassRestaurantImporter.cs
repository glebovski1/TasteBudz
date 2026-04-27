using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Imports OpenStreetMap restaurants through Overpass into the local catalog.
/// </summary>
public sealed class OverpassRestaurantImporter(
    TasteBudzDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<OverpassRestaurantImporter> logger)
{
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";
    private const string OpenStreetMapPlaceIdPrefix = "osm:";
    private const string FallbackCuisine = "Other";

    private const double CincinnatiLatitude = 39.1031;
    private const double CincinnatiLongitude = -84.5120;
    private const double DefaultRadiusMiles = 25;
    private const double MaxRadiusMiles = 50;

    private static readonly Dictionary<string, string> CuisineTagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["american"] = "American",
        ["burger"] = "American",
        ["burgers"] = "American",
        ["sandwich"] = "American",
        ["sandwiches"] = "American",
        ["barbecue"] = "American",
        ["bbq"] = "American",
        ["steak_house"] = "American",
        ["steak"] = "American",
        ["chicken"] = "American",
        ["wings"] = "American",
        ["soul_food"] = "American",
        ["southern"] = "American",
        ["comfort_food"] = "American",
        ["diner"] = "American",
        ["breakfast"] = "American",
        ["brunch"] = "American",
        ["chili"] = "American",
        ["regional"] = "American",
        ["hot_dog"] = "American",
        ["donut"] = "American",
        ["donuts"] = "American",
        ["bagel"] = "American",
        ["bagels"] = "American",
        ["ice_cream"] = "American",
        ["frozen_yogurt"] = "American",
        ["cookie"] = "American",
        ["cookies"] = "American",
        ["italian"] = "Italian",
        ["pizza"] = "Italian",
        ["pasta"] = "Italian",
        ["mexican"] = "Mexican",
        ["tacos"] = "Mexican",
        ["taco"] = "Mexican",
        ["tex-mex"] = "Tex-Mex",
        ["tex_mex"] = "Tex-Mex",
        ["chinese"] = "Chinese",
        ["dim_sum"] = "Chinese",
        ["cantonese"] = "Chinese",
        ["szechuan"] = "Chinese",
        ["sichuan"] = "Chinese",
        ["hunan"] = "Chinese",
        ["noodles"] = "Chinese",
        ["dumpling"] = "Chinese",
        ["dumplings"] = "Chinese",
        ["mongolian"] = "Chinese",
        ["japanese"] = "Japanese",
        ["sushi"] = "Japanese",
        ["ramen"] = "Japanese",
        ["udon"] = "Japanese",
        ["tempura"] = "Japanese",
        ["teppanyaki"] = "Japanese",
        ["teriyaki"] = "Japanese",
        ["hibachi"] = "Japanese",
        ["indian"] = "Indian",
        ["curry"] = "Indian",
        ["pakistani"] = "Indian",
        ["thai"] = "Thai",
        ["vietnamese"] = "Vietnamese",
        ["pho"] = "Vietnamese",
        ["korean"] = "Korean",
        ["korean_bbq"] = "Korean",
        ["mediterranean"] = "Mediterranean",
        ["turkish"] = "Mediterranean",
        ["lebanese"] = "Mediterranean",
        ["middle_eastern"] = "Mediterranean",
        ["falafel"] = "Mediterranean",
        ["kebab"] = "Mediterranean",
        ["shawarma"] = "Mediterranean",
        ["tapas"] = "Mediterranean",
        ["moroccan"] = "Mediterranean",
        ["egyptian"] = "Mediterranean",
        ["greek"] = "Greek",
        ["french"] = "French",
        ["crepe"] = "French",
        ["crepes"] = "French",
        ["belgian"] = "French",
        ["seafood"] = "Seafood",
        ["fish_and_chips"] = "Seafood",
        ["fish"] = "Seafood",
        ["latin_american"] = "Latin American",
        ["caribbean"] = "Caribbean",
        ["cuban"] = "Caribbean",
        ["haitian"] = "Caribbean",
        ["jamaican"] = "Caribbean",
        ["brazilian"] = "Brazilian",
        ["peruvian"] = "Latin American",
        ["colombian"] = "Latin American",
        ["african"] = "African",
        ["ethiopian"] = "African",
        ["west_african"] = "African",
        ["asian"] = "Asian",
        ["asian_fusion"] = "Asian",
        ["fusion"] = "Asian",
        ["cambodian"] = "Asian",
        ["filipino"] = "Asian",
        ["taiwanese"] = "Asian",
        ["malaysian"] = "Asian",
        ["indonesian"] = "Asian",
        ["singaporean"] = "Asian",
        ["german"] = "German",
        ["spanish"] = "Spanish",
        ["portuguese"] = "Spanish",
        ["vegetarian"] = "Vegetarian",
    };

    public async Task<int> ImportAsync(CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAsync(new RestaurantImportPreviewQuery(), cancellationToken);
        var result = await CommitAsync(
            new CommitRestaurantImportRequest
            {
                SelectedExternalPlaceIds = preview.Candidates
                    .Where(candidate => !candidate.IsDuplicate)
                    .Select(candidate => candidate.ExternalPlaceId)
                    .ToArray(),
            },
            cancellationToken);

        return result.Inserted;
    }

    public async Task<RestaurantImportPreviewDto> PreviewAsync(
        RestaurantImportPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var geography = await ResolveGeographyAsync(query, cancellationToken);
        var client = httpClientFactory.CreateClient("Overpass");

        logger.LogInformation(
            "Querying Overpass for restaurants in {Label} ({South},{West}) to ({North},{East}).",
            geography.Label,
            geography.South,
            geography.West,
            geography.North,
            geography.East);

        var elements = await QueryOverpassAsync(client, geography, cancellationToken);
        logger.LogInformation("Found {Count} OpenStreetMap restaurant elements.", elements.Count);

        var existingRestaurants = await LoadExistingRestaurantsAsync(cancellationToken);
        var candidates = elements
            .Select(element => TryBuildCandidate(element, geography))
            .Where(candidate => candidate is not null)
            .Cast<RestaurantImportCandidateDto>()
            .GroupBy(candidate => candidate.ExternalPlaceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(candidate => ApplyDuplicateStatus(candidate, existingRestaurants))
            .OrderBy(candidate => candidate.IsDuplicate)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new RestaurantImportPreviewDto(
            geography.ToDto(),
            candidates,
            candidates.Length,
            candidates.Count(candidate => !candidate.IsDuplicate),
            candidates.Count(candidate => candidate.IsDuplicate));
    }

    public async Task<RestaurantImportCommitResult> CommitAsync(
        CommitRestaurantImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var selectedExternalIds = request.SelectedExternalPlaceIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedExternalIds.Count == 0)
        {
            return new RestaurantImportCommitResult(0, 0);
        }

        var preview = await PreviewAsync(request, cancellationToken);
        var selectedCandidates = preview.Candidates
            .Where(candidate => selectedExternalIds.Contains(candidate.ExternalPlaceId))
            .Where(candidate => !candidate.IsDuplicate)
            .ToArray();
        var cuisines = await EnsureCuisineIndexAsync(cancellationToken);
        var existingRestaurants = await LoadExistingRestaurantsAsync(cancellationToken);
        var importedSnapshots = new List<ExistingRestaurantSnapshot>();
        var inserted = 0;

        foreach (var candidate in selectedCandidates)
        {
            var duplicate = FindDuplicate(candidate, existingRestaurants.Concat(importedSnapshots));
            if (duplicate is not null)
            {
                continue;
            }

            var restaurantId = Guid.NewGuid();
            dbContext.Restaurants.Add(new RestaurantEntity
            {
                Id = restaurantId,
                Name = candidate.Name,
                StreetAddress = candidate.StreetAddress,
                City = candidate.City,
                State = candidate.State,
                ZipCode = candidate.ZipCode,
                Latitude = candidate.Latitude,
                Longitude = candidate.Longitude,
                PriceTier = PriceTier.Two,
                ExternalPlaceId = candidate.ExternalPlaceId,
                IsArchived = false,
            });

            foreach (var cuisineTag in candidate.CuisineTags)
            {
                dbContext.RestaurantCuisines.Add(new RestaurantCuisineEntity
                {
                    RestaurantId = restaurantId,
                    CuisineId = cuisines[cuisineTag],
                });
            }

            importedSnapshots.Add(ExistingRestaurantSnapshot.FromCandidate(restaurantId, candidate));
            inserted++;

            if (inserted % 200 == 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Saved {InsertedCount} imported restaurants so far.", inserted);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Overpass restaurant import inserted {InsertedCount} restaurants.", inserted);

        return new RestaurantImportCommitResult(inserted, selectedExternalIds.Count - inserted);
    }

    private async Task<ImportGeography> ResolveGeographyAsync(
        RestaurantImportPreviewQuery query,
        CancellationToken cancellationToken)
    {
        if (query.South.HasValue || query.West.HasValue || query.North.HasValue || query.East.HasValue)
        {
            if (!query.South.HasValue || !query.West.HasValue || !query.North.HasValue || !query.East.HasValue)
            {
                throw ApiException.BadRequest("Manual import bounds require south, west, north, and east.");
            }

            return ImportGeography.ManualBounds(query.South.Value, query.West.Value, query.North.Value, query.East.Value);
        }

        var radiusMiles = Math.Clamp(query.RadiusMiles ?? DefaultRadiusMiles, 1, MaxRadiusMiles);
        var center = await ResolveCenterAsync(query.ZipCode, cancellationToken)
            ?? (CincinnatiLatitude, CincinnatiLongitude);

        return ImportGeography.FromCenter(
            $"Cincinnati area within {radiusMiles:0.#} miles of {(string.IsNullOrWhiteSpace(query.ZipCode) ? "downtown" : query.ZipCode.Trim())}",
            center.Latitude,
            center.Longitude,
            radiusMiles);
    }

    private async Task<(double Latitude, double Longitude)?> ResolveCenterAsync(string? zipCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
        {
            return null;
        }

        var value = zipCode.Trim();
        var entity = await dbContext.ZipCoordinates
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ZipCode == value, cancellationToken);

        return entity is null ? null : (entity.Latitude, entity.Longitude);
    }

    private async Task<List<OsmElement>> QueryOverpassAsync(
        HttpClient client,
        ImportGeography geography,
        CancellationToken cancellationToken)
    {
        var query = $"""
            [out:json][timeout:90];
            (
              node["amenity"="restaurant"]({geography.South},{geography.West},{geography.North},{geography.East});
              way["amenity"="restaurant"]({geography.South},{geography.West},{geography.North},{geography.East});
            );
            out center;
            """;

        try
        {
            using var content = new FormUrlEncodedContent([new("data", query)]);
            using var response = await client.PostAsync(OverpassUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);

            return document.RootElement
                .GetProperty("elements")
                .EnumerateArray()
                .Select(ReadOsmElement)
                .Where(element => element.Latitude.HasValue && element.Longitude.HasValue)
                .Where(element => geography.Contains(element.Latitude!.Value, element.Longitude!.Value))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Overpass restaurant import query failed.");
            return [];
        }
    }

    private static OsmElement ReadOsmElement(JsonElement element)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (element.TryGetProperty("tags", out var tagsElement))
        {
            foreach (var tag in tagsElement.EnumerateObject())
            {
                tags[tag.Name] = tag.Value.GetString() ?? string.Empty;
            }
        }

        var type = element.GetProperty("type").GetString() ?? "node";
        double? latitude = null;
        double? longitude = null;

        if (element.TryGetProperty("lat", out var latitudeElement))
        {
            latitude = latitudeElement.GetDouble();
        }
        else if (element.TryGetProperty("center", out var centerElement))
        {
            latitude = centerElement.GetProperty("lat").GetDouble();
            longitude = centerElement.GetProperty("lon").GetDouble();
        }

        if (element.TryGetProperty("lon", out var longitudeElement))
        {
            longitude = longitudeElement.GetDouble();
        }

        return new OsmElement(
            element.GetProperty("id").GetInt64(),
            type,
            latitude,
            longitude,
            tags);
    }

    private RestaurantImportCandidateDto? TryBuildCandidate(OsmElement element, ImportGeography geography)
    {
        if (!element.Latitude.HasValue || !element.Longitude.HasValue)
        {
            return null;
        }

        var name = element.Tags.GetValueOrDefault("name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var cuisineTagRaw = element.Tags.GetValueOrDefault("cuisine") ?? string.Empty;
        var matchedCuisines = ResolveCuisineNames(cuisineTagRaw);
        var streetAddress = BuildStreetAddress(element.Tags);
        var city = element.Tags.GetValueOrDefault("addr:city") ?? DeriveCity(element.Latitude.Value);
        var state = element.Tags.GetValueOrDefault("addr:state") ?? DeriveState(element.Latitude.Value);
        var zipCode = element.Tags.GetValueOrDefault("addr:postcode") ?? DeriveZip(element.Latitude.Value, element.Longitude.Value);

        return new RestaurantImportCandidateDto(
            $"{OpenStreetMapPlaceIdPrefix}{element.Type}:{element.Id}",
            name,
            string.IsNullOrWhiteSpace(streetAddress) ? null : streetAddress,
            string.IsNullOrWhiteSpace(city) ? "Cincinnati" : city.Trim(),
            string.IsNullOrWhiteSpace(state) ? "OH" : state.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(zipCode) ? DeriveZip(element.Latitude.Value, element.Longitude.Value) : zipCode.Trim(),
            string.IsNullOrWhiteSpace(cuisineTagRaw) ? FallbackCuisine : cuisineTagRaw,
            matchedCuisines,
            element.Latitude.Value,
            element.Longitude.Value,
            IsDuplicate: false,
            DuplicateReason: null,
            MatchingRestaurantId: null,
            MatchingRestaurantName: null);
    }

    private static RestaurantImportCandidateDto ApplyDuplicateStatus(
        RestaurantImportCandidateDto candidate,
        IReadOnlyCollection<ExistingRestaurantSnapshot> existingRestaurants)
    {
        var duplicate = FindDuplicate(candidate, existingRestaurants);

        return duplicate is null
            ? candidate
            : candidate with
            {
                IsDuplicate = true,
                DuplicateReason = duplicate.Reason,
                MatchingRestaurantId = duplicate.RestaurantId,
                MatchingRestaurantName = duplicate.RestaurantName,
            };
    }

    private async Task<IReadOnlyCollection<ExistingRestaurantSnapshot>> LoadExistingRestaurantsAsync(CancellationToken cancellationToken) =>
        await dbContext.Restaurants
            .AsNoTracking()
            .Select(restaurant => new ExistingRestaurantSnapshot(
                restaurant.Id,
                restaurant.Name,
                restaurant.StreetAddress,
                restaurant.City,
                restaurant.State,
                restaurant.ZipCode,
                restaurant.Latitude,
                restaurant.Longitude,
                restaurant.ExternalPlaceId))
            .ToArrayAsync(cancellationToken);

    private async Task<Dictionary<string, Guid>> EnsureCuisineIndexAsync(CancellationToken cancellationToken)
    {
        var cuisines = await dbContext.Cuisines
            .ToDictionaryAsync(cuisine => cuisine.Name, cuisine => cuisine.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var cuisineName in CuisineTagMap.Values.Append(FallbackCuisine).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (cuisines.ContainsKey(cuisineName))
            {
                continue;
            }

            var cuisine = new CuisineEntity { Id = Guid.NewGuid(), Name = cuisineName };
            dbContext.Cuisines.Add(cuisine);
            cuisines[cuisineName] = cuisine.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return cuisines;
    }

    private static DuplicateMatch? FindDuplicate(RestaurantImportCandidateDto candidate, IEnumerable<ExistingRestaurantSnapshot> existingRestaurants)
    {
        var candidateExternalIds = GetExternalPlaceIdVariants(candidate.ExternalPlaceId);
        var normalizedName = NormalizeForMatch(candidate.Name);
        var simplifiedName = SimplifyName(candidate.Name);
        var normalizedAddress = NormalizeForMatch(candidate.StreetAddress);
        var normalizedCity = NormalizeForMatch(candidate.City);
        var normalizedState = NormalizeForMatch(candidate.State);
        var normalizedZip = NormalizeForMatch(candidate.ZipCode);

        foreach (var restaurant in existingRestaurants)
        {
            if (!string.IsNullOrWhiteSpace(restaurant.ExternalPlaceId) &&
                candidateExternalIds.Contains(restaurant.ExternalPlaceId))
            {
                return new DuplicateMatch(restaurant.Id, restaurant.Name, "Same OpenStreetMap id.");
            }

            var sameAddress =
                !string.IsNullOrEmpty(normalizedAddress) &&
                normalizedName == NormalizeForMatch(restaurant.Name) &&
                normalizedAddress == NormalizeForMatch(restaurant.StreetAddress) &&
                normalizedCity == NormalizeForMatch(restaurant.City) &&
                normalizedState == NormalizeForMatch(restaurant.State) &&
                normalizedZip == NormalizeForMatch(restaurant.ZipCode);

            if (sameAddress)
            {
                return new DuplicateMatch(restaurant.Id, restaurant.Name, "Same name and address.");
            }

            if (restaurant.Latitude.HasValue && restaurant.Longitude.HasValue)
            {
                var distance = CalculateDistanceMiles(candidate.Latitude, candidate.Longitude, restaurant.Latitude.Value, restaurant.Longitude.Value);
                if (normalizedName == NormalizeForMatch(restaurant.Name) && distance <= 0.1)
                {
                    return new DuplicateMatch(restaurant.Id, restaurant.Name, "Same name within 0.1 miles.");
                }

                if (simplifiedName == SimplifyName(restaurant.Name) &&
                    normalizedZip == NormalizeForMatch(restaurant.ZipCode) &&
                    distance <= 0.25)
                {
                    return new DuplicateMatch(restaurant.Id, restaurant.Name, "Similar name in the same ZIP within 0.25 miles.");
                }
            }
        }

        return null;
    }

    private static HashSet<string> GetExternalPlaceIdVariants(string externalPlaceId)
    {
        var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { externalPlaceId };
        var parts = externalPlaceId.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 3)
        {
            variants.Add($"{OpenStreetMapPlaceIdPrefix}{parts[2]}");
        }

        return variants;
    }

    private static IReadOnlyCollection<string> ResolveCuisineNames(string rawTag)
    {
        var matched = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in rawTag.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (CuisineTagMap.TryGetValue(part, out var mapped))
            {
                matched.Add(mapped);
            }
        }

        if (matched.Count == 0)
        {
            matched.Add(FallbackCuisine);
        }

        return matched.ToArray();
    }

    private static string? BuildStreetAddress(Dictionary<string, string> tags)
    {
        var houseNumber = tags.GetValueOrDefault("addr:housenumber");
        var street = tags.GetValueOrDefault("addr:street");

        if (string.IsNullOrWhiteSpace(street))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(houseNumber)
            ? street.Trim()
            : $"{houseNumber.Trim()} {street.Trim()}";
    }

    private static string DeriveCity(double latitude) => latitude < 39.09 ? "Covington" : "Cincinnati";

    private static string DeriveState(double latitude) => latitude < 39.09 ? "KY" : "OH";

    private static string DeriveZip(double latitude, double longitude) => (latitude, longitude) switch
    {
        _ when latitude < 39.09 => "41011",
        _ when longitude < -84.55 => "45220",
        _ when latitude < 39.12 => "45202",
        _ when latitude < 39.13 => "45219",
        _ when longitude > -84.45 => "45208",
        _ => "45206",
    };

    private static string NormalizeForMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }

    private static string SimplifyName(string value)
    {
        var normalized = NormalizeForMatch(value);
        var wordsToRemove = new[] { "THE", "RESTAURANT", "BAR", "GRILL", "CAFE", "KITCHEN", "BISTRO", "CO" };

        foreach (var word in wordsToRemove)
        {
            normalized = normalized.Replace(word, string.Empty, StringComparison.Ordinal);
        }

        return normalized;
    }

    private static double CalculateDistanceMiles(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMiles = 3958.8;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var startLat = DegreesToRadians(lat1);
        var endLat = DegreesToRadians(lat2);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(startLat) * Math.Cos(endLat) * Math.Pow(Math.Sin(dLon / 2), 2);
        var c = 2 * Math.Asin(Math.Sqrt(a));

        return earthRadiusMiles * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private sealed record OsmElement(
        long Id,
        string Type,
        double? Latitude,
        double? Longitude,
        Dictionary<string, string> Tags);

    private sealed record ImportGeography(
        string Label,
        double South,
        double West,
        double North,
        double East,
        double? CenterLatitude,
        double? CenterLongitude,
        double? RadiusMiles)
    {
        public static ImportGeography ManualBounds(double south, double west, double north, double east)
        {
            if (south >= north || west >= east)
            {
                throw ApiException.BadRequest("Manual import bounds are invalid.");
            }

            return new("Manual bounds", south, west, north, east, null, null, null);
        }

        public static ImportGeography FromCenter(string label, double latitude, double longitude, double radiusMiles)
        {
            var latDelta = radiusMiles / 69.0;
            var lonDelta = radiusMiles / (69.0 * Math.Cos(DegreesToRadians(latitude)));

            return new(
                label,
                latitude - latDelta,
                longitude - lonDelta,
                latitude + latDelta,
                longitude + lonDelta,
                latitude,
                longitude,
                radiusMiles);
        }

        public bool Contains(double latitude, double longitude)
        {
            if (latitude < South || latitude > North || longitude < West || longitude > East)
            {
                return false;
            }

            if (!CenterLatitude.HasValue || !CenterLongitude.HasValue || !RadiusMiles.HasValue)
            {
                return true;
            }

            return CalculateDistanceMiles(CenterLatitude.Value, CenterLongitude.Value, latitude, longitude) <= RadiusMiles.Value;
        }

        public RestaurantImportGeographyDto ToDto() =>
            new(Label, South, West, North, East, CenterLatitude, CenterLongitude, RadiusMiles);
    }

    private sealed record ExistingRestaurantSnapshot(
        Guid Id,
        string Name,
        string? StreetAddress,
        string City,
        string State,
        string ZipCode,
        double? Latitude,
        double? Longitude,
        string? ExternalPlaceId)
    {
        public static ExistingRestaurantSnapshot FromCandidate(Guid restaurantId, RestaurantImportCandidateDto candidate) =>
            new(
                restaurantId,
                candidate.Name,
                candidate.StreetAddress,
                candidate.City,
                candidate.State,
                candidate.ZipCode,
                candidate.Latitude,
                candidate.Longitude,
                candidate.ExternalPlaceId);
    }

    private sealed record DuplicateMatch(Guid RestaurantId, string RestaurantName, string Reason);
}

public sealed record RestaurantImportCommitResult(int Inserted, int Skipped);
