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

    private const double BBoxSouth = 38.95;
    private const double BBoxNorth = 39.35;
    private const double BBoxWest = -84.75;
    private const double BBoxEast = -84.25;

    private static readonly Dictionary<string, string> CuisineTagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sushi"] = "Sushi",
        ["japanese"] = "Japanese",
        ["indian"] = "Indian",
        ["mexican"] = "Mexican",
        ["tacos"] = "Tacos",
        ["thai"] = "Thai",
        ["noodles"] = "Noodles",
        ["american"] = "American",
        ["mediterranean"] = "Mediterranean",
        ["vegetarian"] = "Vegetarian",
        ["vietnamese"] = "Vietnamese",
        ["pizza"] = "Pizza",
        ["italian"] = "Italian",
        ["burger"] = "American",
        ["burgers"] = "American",
        ["chinese"] = "Chinese",
        ["korean"] = "Korean",
        ["greek"] = "Greek",
        ["french"] = "French",
        ["seafood"] = "Seafood",
        ["tex-mex"] = "Tex-Mex",
        ["sandwich"] = "American",
        ["barbecue"] = "American",
        ["steak_house"] = "American",
        ["chicken"] = "American",
    };

    public async Task<int> ImportAsync(CancellationToken cancellationToken = default)
    {
        var cuisines = await dbContext.Cuisines.AsNoTracking()
            .ToDictionaryAsync(cuisine => cuisine.Name, cuisine => cuisine.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existingOpenStreetMapIds = await dbContext.Restaurants.AsNoTracking()
            .Where(restaurant => restaurant.ExternalPlaceId != null && restaurant.ExternalPlaceId.StartsWith(OpenStreetMapPlaceIdPrefix))
            .Select(restaurant => restaurant.ExternalPlaceId!)
            .ToHashSetAsync(cancellationToken);

        foreach (var cuisineName in CuisineTagMap.Values.Distinct(StringComparer.OrdinalIgnoreCase))
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
        var nodes = await QueryOverpassAsync(client, cancellationToken);
        var inserted = 0;

        foreach (var node in nodes)
        {
            var openStreetMapId = $"{OpenStreetMapPlaceIdPrefix}{node.Id}";
            if (existingOpenStreetMapIds.Contains(openStreetMapId))
            {
                continue;
            }

            var name = node.Tags.GetValueOrDefault("name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var restaurantId = Guid.NewGuid();
            var cuisineTagRaw = node.Tags.GetValueOrDefault("cuisine") ?? string.Empty;
            var matchedCuisines = ResolveCuisines(cuisineTagRaw, cuisines);

            dbContext.Restaurants.Add(new RestaurantEntity
            {
                Id = restaurantId,
                Name = name.Trim(),
                City = node.Tags.GetValueOrDefault("addr:city") ?? DeriveCity(node.Lat),
                State = node.Tags.GetValueOrDefault("addr:state") ?? DeriveState(node.Lat),
                ZipCode = node.Tags.GetValueOrDefault("addr:postcode") ?? DeriveZip(node.Lat, node.Lon),
                Latitude = node.Lat,
                Longitude = node.Lon,
                PriceTier = PriceTier.Two,
                ExternalPlaceId = openStreetMapId,
            });

            foreach (var cuisineId in matchedCuisines)
            {
                dbContext.RestaurantCuisines.Add(new RestaurantCuisineEntity
                {
                    RestaurantId = restaurantId,
                    CuisineId = cuisineId,
                });
            }

            existingOpenStreetMapIds.Add(openStreetMapId);
            inserted++;

            if (inserted % 100 == 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Overpass restaurant import inserted {InsertedCount} restaurants.", inserted);
        return inserted;
    }

    private async Task<List<OsmNode>> QueryOverpassAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var query = $"""
            [out:json][timeout:60];
            node["amenity"="restaurant"]({BBoxSouth},{BBoxWest},{BBoxNorth},{BBoxEast});
            out;
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
                .Select(element =>
                {
                    var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (element.TryGetProperty("tags", out var tagsElement))
                    {
                        foreach (var tag in tagsElement.EnumerateObject())
                        {
                            tags[tag.Name] = tag.Value.GetString() ?? string.Empty;
                        }
                    }

                    return new OsmNode(
                        element.GetProperty("id").GetInt64(),
                        element.GetProperty("lat").GetDouble(),
                        element.GetProperty("lon").GetDouble(),
                        tags);
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Overpass restaurant import query failed.");
            return [];
        }
    }

    private static IReadOnlyCollection<Guid> ResolveCuisines(
        string rawTag,
        Dictionary<string, Guid> cuisineIndex)
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

    private static string DeriveCity(double lat) => lat < 39.09 ? "Covington" : "Cincinnati";

    private static string DeriveState(double lat) => lat < 39.09 ? "KY" : "OH";

    private static string DeriveZip(double lat, double lon) => (lat, lon) switch
    {
        _ when lat < 39.09 => "41011",
        _ when lon < -84.55 => "45220",
        _ when lat < 39.12 => "45202",
        _ when lat < 39.13 => "45219",
        _ when lon > -84.45 => "45208",
        _ => "45206",
    };

    private sealed record OsmNode(
        long Id,
        double Lat,
        double Lon,
        Dictionary<string, string> Tags);
}
