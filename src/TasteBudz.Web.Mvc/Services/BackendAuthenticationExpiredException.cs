using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Web.Mvc.Services;


internal sealed class BackendAuthenticationExpiredException : BackendApiException
{
    public BackendAuthenticationExpiredException(string message)
        : base(HttpStatusCode.Unauthorized, message)
    {
    }
}
