using System.Text.Json.Serialization;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class BackendProblemDetails
{
    public int? Status { get; init; }

    public string? Title { get; init; }

    public string? Detail { get; init; }

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? Extensions { get; init; }
}
