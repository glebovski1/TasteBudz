using System.Net;
using System.Net.Http.Json;

namespace TasteBudz.Web.Mvc.Services.Backend;

public static class BackendApiResponseReader
{
    public static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(BackendJson.Options, cancellationToken);
        return payload ?? throw new InvalidOperationException("The backend returned an empty response body.");
    }

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    public static async Task<BackendApiException> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        BackendProblemDetails? problem = null;

        try
        {
            problem = await response.Content.ReadFromJsonAsync<BackendProblemDetails>(BackendJson.Options, cancellationToken);
        }
        catch
        {
            // Fall back to the status text when the backend returned a non-ProblemDetails payload.
        }

        var message = problem?.Detail
            ?? problem?.Title
            ?? GetDefaultMessage(response.StatusCode);

        return response.StatusCode == HttpStatusCode.Unauthorized
            ? new BackendAuthenticationExpiredException(message)
            : new BackendApiException(response.StatusCode, message);
    }

    private static string GetDefaultMessage(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.BadRequest => "The request could not be completed.",
            HttpStatusCode.Conflict => "The requested change conflicts with the current server state.",
            HttpStatusCode.NotFound => "The requested resource could not be found.",
            HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            _ => "The request to the backend failed.",
        };
}
