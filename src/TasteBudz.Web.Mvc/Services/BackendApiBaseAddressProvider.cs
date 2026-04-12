using Microsoft.Extensions.Options;
using TasteBudz.Web.Mvc.Options;

namespace TasteBudz.Web.Mvc.Services;

public sealed class BackendApiBaseAddressProvider(
    IOptions<BackendApiOptions> options,
    IHttpContextAccessor httpContextAccessor) : IBackendApiBaseAddressProvider
{
    public Uri GetBaseAddress()
    {
        if (Uri.TryCreate(options.Value.BaseUrl, UriKind.Absolute, out var configuredBaseUrl))
        {
            return configuredBaseUrl;
        }

        var request = httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("BackendApi:BaseUrl must be configured when there is no active request context.");

        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
        return new Uri(baseUrl, UriKind.Absolute);
    }
}
