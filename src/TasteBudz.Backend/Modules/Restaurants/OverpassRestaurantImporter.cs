using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

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

    // Expanded Greater Cincinnati / Northern Kentucky bounding box.
    private const double BBoxSouth = 38.90;
    private const double BBoxNorth = 39.40;
    private const double BBoxWest = -84.90;
    private const double BBoxEast = -84.15;

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
        var cuisines = await dbContext.Cuisines.AsNoTracking()
            .ToDictionaryAsync(cuisine => cuisine.Name, cuisine => cuisine.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existingOpenStreetMapIds = await dbContext.Restaurants.AsNoTracking()
            .Where(restaurant => restaurant.ExternalPlaceId != null && restaurant.ExternalPlaceId.StartsWith(OpenStreetMapPlaceIdPrefix))
            .Select(restaurant => restaurant.ExternalPlaceId!)
            .ToHashSetAsync(cancellationToken);

        var allCuisineNames = CuisineTagMap.Values
            .Append(FallbackCuisine)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var cuisineName in allCuisineNames)
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

        var client = httpClientFactory.CreateClient("Overpass");
        logger.LogInformation(
            "Querying Overpass for restaurants in Greater Cincinnati ({South},{West}) to ({North},{East}).",
            BBoxSouth,
            BBoxWest,
            BBoxNorth,
            BBoxEast);

        var elements = await QueryOverpassAsync(client, cancellationToken);
        logger.LogInformation("Found {Count} OpenStreetMap restaurant elements.", elements.Count);

        var inserted = 0;

        foreach (var element in elements)
        {
            if (!element.Latitude.HasValue || !element.Longitude.HasValue)
            {
                continue;
            }

            var providerQualifiedId = $"{OpenStreetMapPlaceIdPrefix}{element.Type}:{element.Id}";
            var legacyNodeId = $"{OpenStreetMapPlaceIdPrefix}{element.Id}";
            if (existingOpenStreetMapIds.Contains(providerQualifiedId) ||
                existingOpenStreetMapIds.Contains(legacyNodeId))
            {
                continue;
            }

            var name = element.Tags.GetValueOrDefault("name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var restaurantId = Guid.NewGuid();
            var cuisineTagRaw = element.Tags.GetValueOrDefault("cuisine") ?? string.Empty;
            var matchedCuisines = ResolveCuisines(cuisineTagRaw, cuisines);
            if (matchedCuisines.Count == 0 && cuisines.TryGetValue(FallbackCuisine, out var fallbackCuisineId))
            {
                matchedCuisines = [fallbackCuisineId];
            }

            dbContext.Restaurants.Add(new RestaurantEntity
            {
                Id = restaurantId,
                Name = name.Trim(),
                City = element.Tags.GetValueOrDefault("addr:city") ?? DeriveCity(element.Latitude.Value),
                State = element.Tags.GetValueOrDefault("addr:state") ?? DeriveState(element.Latitude.Value),
                ZipCode = element.Tags.GetValueOrDefault("addr:postcode") ?? DeriveZip(element.Latitude.Value, element.Longitude.Value),
                Latitude = element.Latitude.Value,
                Longitude = element.Longitude.Value,
                PriceTier = PriceTier.Two,
                ExternalPlaceId = providerQualifiedId,
            });

            foreach (var cuisineId in matchedCuisines)
            {
                dbContext.RestaurantCuisines.Add(new RestaurantCuisineEntity
                {
                    RestaurantId = restaurantId,
                    CuisineId = cuisineId,
                });
            }

            existingOpenStreetMapIds.Add(providerQualifiedId);
            inserted++;

            if (inserted % 200 == 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Saved {InsertedCount} imported restaurants so far.", inserted);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Overpass restaurant import inserted {InsertedCount} restaurants.", inserted);
        return inserted;
    }

    private async Task<List<OsmElement>> QueryOverpassAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var query = $"""
            [out:json][timeout:90];
            (
              node["amenity"="restaurant"]({BBoxSouth},{BBoxWest},{BBoxNorth},{BBoxEast});
              way["amenity"="restaurant"]({BBoxSouth},{BBoxWest},{BBoxNorth},{BBoxEast});
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

    private static IReadOnlyCollection<Guid> ResolveCuisines(string rawTag, Dictionary<string, Guid> cuisineIndex)
    {
        var matched = new HashSet<Guid>();

        foreach (var part in rawTag.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (CuisineTagMap.TryGetValue(part, out var mapped) &&
                cuisineIndex.TryGetValue(mapped, out var id))
            {
                matched.Add(id);
            }
        }

        return matched;
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

    private sealed record OsmElement(
        long Id,
        string Type,
        double? Latitude,
        double? Longitude,
        Dictionary<string, string> Tags);
}
