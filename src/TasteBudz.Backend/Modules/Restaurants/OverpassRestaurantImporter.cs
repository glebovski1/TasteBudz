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

    // Expanded bounding box covering the full Greater Cincinnati metro area
    // including Northern Kentucky, Mason, Harrison, Batavia, and Lawrenceburg IN.
    // Format for Overpass: (south, west, north, east)
    private const double BBoxSouth = 38.90;  // South of Florence / Erlanger KY
    private const double BBoxNorth = 39.40;  // North of Mason / Lebanon OH
    private const double BBoxWest = -84.90; // West past Harrison / Lawrenceburg IN
    private const double BBoxEast = -84.15; // East past Batavia / Milford OH

    // Maps OSM cuisine tag values to display cuisine names in the database.
    // OSM tags are lowercase and use underscores; values are the display names
    // shown in the UI. Multiple OSM tags can map to the same display name.
    // Maps OSM cuisine tag values to display cuisine names in the database.
    // OSM tags are lowercase and use underscores; values are the display names
    // shown in the UI. Multiple OSM tags can map to the same display name.
    private static readonly Dictionary<string, string> CuisineTagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // American
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
        ["american;regional"] = "American",
        ["hot_dog"] = "American",
        ["donut"] = "American",
        ["donuts"] = "American",
        ["bagel"] = "American",
        ["bagels"] = "American",
        ["ice_cream"] = "American",
        ["frozen_yogurt"] = "American",
        ["cookie"] = "American",
        ["cookies"] = "American",

        // Italian
        ["italian"] = "Italian",
        ["pizza"] = "Italian",
        ["pasta"] = "Italian",

        // Mexican
        ["mexican"] = "Mexican",
        ["tacos"] = "Mexican",
        ["taco"] = "Mexican",
        ["tex-mex"] = "Tex-Mex",
        ["tex_mex"] = "Tex-Mex",

        // Chinese
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
        ["chinese;sushi"] = "Chinese",

        // Japanese
        ["japanese"] = "Japanese",
        ["sushi"] = "Japanese",
        ["ramen"] = "Japanese",
        ["udon"] = "Japanese",
        ["tempura"] = "Japanese",
        ["teppanyaki"] = "Japanese",
        ["teriyaki"] = "Japanese",
        ["japanese;sushi"] = "Japanese",
        ["sushi;japanese"] = "Japanese",
        ["hibachi"] = "Japanese",

        // Indian
        ["indian"] = "Indian",
        ["curry"] = "Indian",
        ["pakistani"] = "Indian",

        // Thai
        ["thai"] = "Thai",
        ["thai;sushi"] = "Thai",

        // Vietnamese
        ["vietnamese"] = "Vietnamese",
        ["pho"] = "Vietnamese",

        // Korean
        ["korean"] = "Korean",
        ["korean_bbq"] = "Korean",

        // Mediterranean
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

        // Greek
        ["greek"] = "Greek",

        // French
        ["french"] = "French",
        ["crepe"] = "French",
        ["crepes"] = "French",
        ["belgian"] = "French",

        // Seafood
        ["seafood"] = "Seafood",
        ["fish_and_chips"] = "Seafood",
        ["fish"] = "Seafood",
        ["sushi;seafood"] = "Seafood",

        // Latin American
        ["latin_american"] = "Latin American",
        ["caribbean"] = "Caribbean",
        ["cuban"] = "Caribbean",
        ["haitian"] = "Caribbean",
        ["jamaican"] = "Caribbean",
        ["brazilian"] = "Brazilian",
        ["peruvian"] = "Latin American",
        ["colombian"] = "Latin American",

        // African
        ["african"] = "African",
        ["ethiopian"] = "African",
        ["west_african"] = "African",
        ["cambodian"] = "Asian",

        // Other Asian
        ["asian"] = "Asian",
        ["asian_fusion"] = "Asian",
        ["fusion"] = "Asian",
        ["filipino"] = "Asian",
        ["taiwanese"] = "Asian",
        ["malaysian"] = "Asian",
        ["indonesian"] = "Asian",
        ["singaporean"] = "Asian",

        // European
        ["german"] = "German",
        ["spanish"] = "Spanish",
        ["portuguese"] = "Spanish",
    };

    // Fallback cuisine name used when a restaurant has no OSM cuisine tag at all.
    private const string FallbackCuisine = "Other";
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

        // Ensure any new cuisine names from the map exist in the database,
        // including the fallback used for restaurants with no OSM cuisine tag.
        var allCuisineNames = CuisineTagMap.Values
            .Append(FallbackCuisine)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var cuisineName in allCuisineNames)
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
            "Querying Overpass for restaurants in Greater Cincinnati ({S},{W}) to ({N},{E})...",
            BBoxSouth, BBoxWest, BBoxNorth, BBoxEast);

        var nodes = await QueryOverpassAsync(client, cancellationToken);

        logger.LogInformation("Found {Count} OSM restaurant elements (nodes + ways).", nodes.Count);

        var inserted = 0;

        foreach (var node in nodes)
        {
            var osmId = $"osm:{node.Type}:{node.Id}";
            if (existingOsmIds.Contains(osmId))
                continue;

            var name = node.Tags.GetValueOrDefault("name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            // Skip entries without coordinates (ways without center data)
            if (!node.Lat.HasValue || !node.Lon.HasValue)
                continue;

            var restaurantId = Guid.NewGuid();
            var cuisineTagRaw = node.Tags.GetValueOrDefault("cuisine") ?? "";
            var matchedCuisines = ResolveCuisines(cuisineTagRaw, cuisines);

            // If OSM has no cuisine data, assign the fallback so the restaurant
            // still appears in the UI rather than showing as untagged.
            if (matchedCuisines.Count == 0 && cuisines.TryGetValue(FallbackCuisine, out var fallbackId))
                matchedCuisines = [fallbackId];
            var city = node.Tags.GetValueOrDefault("addr:city") ?? DeriveCity(node.Lat.Value, node.Lon.Value);
            var state = node.Tags.GetValueOrDefault("addr:state") ?? DeriveState(node.Lat.Value, node.Lon.Value);
            var zipCode = node.Tags.GetValueOrDefault("addr:postcode") ?? DeriveZip(node.Lat.Value, node.Lon.Value);

            dbContext.Restaurants.Add(new RestaurantEntity
            {
                Id = restaurantId,
                Name = name.Trim(),
                City = city,
                State = state,
                ZipCode = zipCode,
                Latitude = node.Lat.Value,
                Longitude = node.Lon.Value,
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

            // Save in batches of 200 to avoid holding too many changes in memory
            if (inserted % 200 == 0)
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

    private async Task<List<OsmElement>> QueryOverpassAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        // Query BOTH nodes (point markers) and ways (building polygons).
        // "out center" returns the centroid lat/lon for way elements.
        // This typically doubles the number of results vs node-only queries.
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

                    var type = el.GetProperty("type").GetString() ?? "node";

                    // Nodes have lat/lon directly; ways have a "center" object
                    double? lat = null;
                    double? lon = null;

                    if (el.TryGetProperty("lat", out var latEl))
                        lat = latEl.GetDouble();
                    else if (el.TryGetProperty("center", out var center))
                    {
                        lat = center.GetProperty("lat").GetDouble();
                        lon = center.GetProperty("lon").GetDouble();
                    }

                    if (el.TryGetProperty("lon", out var lonEl))
                        lon = lonEl.GetDouble();

                    return new OsmElement(
                        el.GetProperty("id").GetInt64(),
                        type,
                        lat,
                        lon,
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

    /// <summary>Derives city from coordinates.</summary>
    private static string DeriveCity(double lat, double lon) => lat < 39.09 ? "Covington" : "Cincinnati";

    /// <summary>Derives state from coordinates — Kentucky south of ~39.09, Ohio north.</summary>
    private static string DeriveState(double lat, double lon) => lat < 39.09 ? "KY" : "OH";

    /// <summary>Returns the nearest seeded ZIP code based on coordinate ranges.</summary>
    private static string DeriveZip(double lat, double lon) => (lat, lon) switch
    {
        _ when lat < 39.09 => "41011", // Northern KY / Covington
        _ when lon < -84.55 => "45220", // Clifton / Northside
        _ when lat < 39.12 => "45202", // Downtown Cincinnati
        _ when lat < 39.13 => "45219", // UC area
        _ when lon > -84.45 => "45208", // Hyde Park / Columbia Tusculum
        _ => "45206", // Default — Evanston area
    };

    private sealed record OsmElement(
        long Id,
        string Type,
        double? Lat,
        double? Lon,
        Dictionary<string, string> Tags);
}