// Imports real restaurant data from OpenStreetMap via the Overpass API.
// Free, no API key required. Queries by bounding box over Greater Cincinnati / Northern Kentucky.
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Fetches restaurants from OpenStreetMap (Overpass API) and persists them
/// to the local SQLite catalog. Safe to run multiple times — skips existing entries.
/// </summary>
public sealed class OverpassRestaurantImporter(
    TasteBudzDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<OverpassRestaurantImporter> logger)
{
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";

    // Bounding box covering Greater Cincinnati and Northern Kentucky:
    // South: 38.95 (south of Covington/Newport)
    // North: 39.35 (north of I-275 loop)
    // West:  -84.75 (west of Harrison/Colerain)
    // East:  -84.25 (east of Milford/Anderson)
    private const double BBoxSouth = 38.95;
    private const double BBoxNorth = 39.35;
    private const double BBoxWest = -84.75;
    private const double BBoxEast = -84.25;

    // Maps OSM cuisine tag values to your existing Cuisine names in the database.
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

    /// <summary>
    /// Runs the import for the Greater Cincinnati bounding box.
    /// Returns the number of new restaurants inserted.
    /// </summary>
    public async Task<int> ImportAsync(CancellationToken cancellationToken = default)
    {
        // Load existing cuisine lookup and already-imported OSM IDs
        var cuisines = await dbContext.Cuisines.AsNoTracking()
            .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existingOsmIds = await dbContext.Restaurants.AsNoTracking()
            .Where(r => r.ExternalPlaceId != null)
            .Select(r => r.ExternalPlaceId!)
            .ToHashSetAsync(cancellationToken);

        // Add any new cuisine names from the map that aren't in the database yet
        foreach (var cuisineName in CuisineTagMap.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!cuisines.ContainsKey(cuisineName))
            {
                var newCuisine = new CuisineEntity { Id = Guid.NewGuid(), Name = cuisineName };
                dbContext.Cuisines.Add(newCuisine);
                cuisines[cuisineName] = newCuisine.Id;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var client = httpClientFactory.CreateClient("Overpass");

        logger.LogInformation(
            "Querying Overpass for restaurants in Greater Cincinnati bounding box ({S},{W}) to ({N},{E})...",
            BBoxSouth, BBoxWest, BBoxNorth, BBoxEast);

        var nodes = await QueryOverpassAsync(client, cancellationToken);

        logger.LogInformation("Found {Count} OSM restaurant nodes.", nodes.Count);

        var inserted = 0;

        foreach (var node in nodes)
        {
            var osmId = $"osm:{node.Id}";
            if (existingOsmIds.Contains(osmId))
                continue;

            var name = node.Tags.GetValueOrDefault("name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var restaurantId = Guid.NewGuid();
            var cuisineTagRaw = node.Tags.GetValueOrDefault("cuisine") ?? "";
            var matchedCuisines = ResolveCuisines(cuisineTagRaw, cuisines);
            var city = node.Tags.GetValueOrDefault("addr:city") ?? DeriveCity(node.Lat, node.Lon);
            var state = node.Tags.GetValueOrDefault("addr:state") ?? DeriveState(node.Lat, node.Lon);
            var zipCode = node.Tags.GetValueOrDefault("addr:postcode") ?? DeriveZip(node.Lat, node.Lon);

            dbContext.Restaurants.Add(new RestaurantEntity
            {
                Id = restaurantId,
                Name = name.Trim(),
                City = city,
                State = state,
                ZipCode = zipCode,
                Latitude = node.Lat,
                Longitude = node.Lon,
                PriceTier = PriceTier.Two,
                ExternalPlaceId = osmId,
            });

            foreach (var cuisineId in matchedCuisines)
            {
                dbContext.RestaurantCuisines.Add(new RestaurantCuisineEntity
                {
                    RestaurantId = restaurantId,
                    CuisineId = cuisineId,
                });
            }

            existingOsmIds.Add(osmId);
            inserted++;

            // Save in batches of 100 to avoid holding too many changes in memory
            if (inserted % 100 == 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("  Saved {Count} restaurants so far...", inserted);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Overpass import complete. Inserted {Count} new restaurants.", inserted);
        return inserted;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<List<OsmNode>> QueryOverpassAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        // Single bounding box query — much faster and more complete than per-ZIP queries.
        // Format: (south, west, north, east)
        var query = $"""
            [out:json][timeout:60];
            node["amenity"="restaurant"]({BBoxSouth},{BBoxWest},{BBoxNorth},{BBoxEast});
            out;
            """;

        try
        {
            var content = new FormUrlEncodedContent([new("data", query)]);
            var response = await client.PostAsync(OverpassUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("elements")
                .EnumerateArray()
                .Select(el =>
                {
                    var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (el.TryGetProperty("tags", out var tagsEl))
                    {
                        foreach (var tag in tagsEl.EnumerateObject())
                            tags[tag.Name] = tag.Value.GetString() ?? "";
                    }

                    return new OsmNode(
                        el.GetProperty("id").GetInt64(),
                        el.GetProperty("lat").GetDouble(),
                        el.GetProperty("lon").GetDouble(),
                        tags);
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Overpass query failed.");
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

    /// <summary>Derives city from coordinates — Kentucky south of the river, Ohio north.</summary>
    private static string DeriveCity(double lat, double lon) => lat < 39.09 ? "Covington" : "Cincinnati";

    /// <summary>Derives state from coordinates.</summary>
    private static string DeriveState(double lat, double lon) => lat < 39.09 ? "KY" : "OH";

    /// <summary>Returns the nearest seeded ZIP code based on rough coordinate ranges.</summary>
    private static string DeriveZip(double lat, double lon) => (lat, lon) switch
    {
        _ when lat < 39.09 => "41011", // Northern KY / Covington
        _ when lon < -84.55 => "45220", // Clifton / Northside
        _ when lat < 39.12 => "45202", // Downtown Cincinnati
        _ when lat < 39.13 => "45219", // UC area
        _ when lon > -84.45 => "45208", // Hyde Park / Columbia Tusculum
        _ => "45206", // Default — Evanston / Hyde Park area
    };

    private sealed record OsmNode(
        long Id,
        double Lat,
        double Lon,
        Dictionary<string, string> Tags);
}