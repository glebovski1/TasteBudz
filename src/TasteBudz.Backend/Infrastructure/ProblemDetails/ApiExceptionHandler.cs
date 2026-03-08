// Central exception-to-ProblemDetails adapter for all API requests.
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TasteBudz.Backend.Infrastructure.ProblemDetails;

/// <summary>
/// Converts application exceptions into consistent ProblemDetails payloads.
/// </summary>
public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Maps known failure types to API-friendly responses and hides unexpected exception details.
    /// The handler also ensures that every error uses the same ProblemDetails envelope and media type.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The tuple keeps the downstream response-writing path simple regardless of which exception branch matched.
        var (statusCode, title, detail, errors) = exception switch
        {
            ApiException apiException => (apiException.StatusCode, apiException.Title, apiException.Detail, apiException.Errors),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required for this operation.", null),
            _ => (StatusCodes.Status500InternalServerError, "Server Error", "An unexpected server error occurred.", null),
        };

        // Only unexpected server failures are logged as errors; expected business failures stay quiet.
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };

        // requestId makes client bug reports much easier to correlate with server logs.
        problemDetails.Extensions["requestId"] = httpContext.TraceIdentifier;

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        // Set the response metadata explicitly before serializing so test clients and real clients both see ProblemDetails.
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
