using System.Net;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class BackendAuthenticationExpiredException : BackendApiException
{
    public BackendAuthenticationExpiredException(string message)
        : base(HttpStatusCode.Unauthorized, message)
    {
    }
}
