using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Web.Mvc.Services;


internal sealed class BackendProblemDetails
{
    public int? Status { get; init; }

    public string? Title { get; init; }

    public string? Detail { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; init; }
}
