using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Web.Mvc.Services;


internal class BackendApiException : Exception
{
    public BackendApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
