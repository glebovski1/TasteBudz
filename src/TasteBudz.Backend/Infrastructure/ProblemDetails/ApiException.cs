// Application exception type that maps domain-level failures to HTTP ProblemDetails responses.
namespace TasteBudz.Backend.Infrastructure.ProblemDetails;

/// <summary>
/// Represents a request failure that should be returned to the client without being treated as a crash.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string title, string detail, IDictionary<string, string[]>? errors = null)
        : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Errors = errors;
    }

    public int StatusCode { get; }

    public string Title { get; }

    public string Detail { get; }

    public IDictionary<string, string[]>? Errors { get; }

    /// <summary>
    /// Creates a 400 response payload, optionally including field-level validation details.
    /// </summary>
    public static ApiException BadRequest(string detail, IDictionary<string, string[]>? errors = null) =>
        new(StatusCodes.Status400BadRequest, "Bad Request", detail, errors);

    /// <summary>
    /// Creates a 401 response payload for missing or invalid authentication.
    /// </summary>
    public static ApiException Unauthorized(string detail = "Authentication is required for this operation.") =>
        new(StatusCodes.Status401Unauthorized, "Unauthorized", detail);

    /// <summary>
    /// Creates a 403 response payload for authenticated users that are not allowed to proceed.
    /// </summary>
    public static ApiException Forbidden(string detail) =>
        new(StatusCodes.Status403Forbidden, "Forbidden", detail);

    /// <summary>
    /// Creates a 404 response payload when a requested resource should be hidden or is missing.
    /// </summary>
    public static ApiException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "Not Found", detail);

    /// <summary>
    /// Creates a 409 response payload for invariant violations or state conflicts.
    /// </summary>
    public static ApiException Conflict(string detail) =>
        new(StatusCodes.Status409Conflict, "Conflict", detail);
}