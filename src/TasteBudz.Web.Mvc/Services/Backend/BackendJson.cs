using System.Text.Json;
using System.Text.Json.Serialization;

namespace TasteBudz.Web.Mvc.Services.Backend;

public static class BackendJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
