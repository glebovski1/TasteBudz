using System.Net;

namespace TasteBudz.Web.Mvc.Services.Backend;

public class BackendApiException : Exception
{
    public BackendApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
