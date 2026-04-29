using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;

namespace TasteBudz.Backend.Modules.Restaurants;


internal sealed class NominatimSearchResult
{
    [JsonPropertyName("lat")]
    public string Latitude { get; init; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Longitude { get; init; } = string.Empty;

    [JsonPropertyName("osm_type")]
    public string? OsmType { get; init; }

    [JsonPropertyName("osm_id")]
    public long? OsmId { get; init; }
}
