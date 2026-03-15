using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Web.Mvc.Services.Session;

namespace TasteBudz.Web.Mvc.Services.Http;

/// <summary>
/// Centralizes all HTTP communication with the backend API.
/// Controllers and feature API services should never deal with raw HttpClient behavior directly.
/// </summary>
public sealed class BackendHttpClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly UserSessionService userSessionService;

    public BackendHttpClient(
        IHttpClientFactory httpClientFactory,
        UserSessionService userSessionService)
    {
        this.httpClientFactory = httpClientFactory;
        this.userSessionService = userSessionService;
    }

    public Task<TResponse> GetAsync<TResponse>(
        string path,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => new HttpRequestMessage(HttpMethod.Get, path),
            requiresAuth: true,
            cancellationToken);

    public Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => CreateJsonRequest(HttpMethod.Post, path, payload),
            requiresAuth,
            cancellationToken);

    public Task PostAsync(
        string path,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
            () => new HttpRequestMessage(HttpMethod.Post, path),
            requiresAuth,
            cancellationToken);

    public Task<TResponse> PutAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => CreateJsonRequest(HttpMethod.Put, path, payload),
            requiresAuth: true,
            cancellationToken);

    public Task<TResponse> PatchAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => CreateJsonRequest(HttpMethod.Patch, path, payload),
            requiresAuth: true,
            cancellationToken);

    private async Task<TResponse> SendAsync<TResponse>(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithRefreshAsync(requestFactory, requiresAuth, cancellationToken);
        return await ReadJsonAsync<TResponse>(response, cancellationToken);
    }

    private async Task SendNoContentAsync(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithRefreshAsync(requestFactory, requiresAuth, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        // Send the request once with the current access token first.
        var response = await SendOnceAsync(requestFactory, requiresAuth, cancellationToken);

        if (!requiresAuth || response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();

        // A protected request that returns 401 gets one refresh attempt and one retry.
        var refreshed = await TryRefreshAsync(cancellationToken);

        if (!refreshed)
        {
            throw new BackendAuthenticationExpiredException("Your session has expired. Please sign in again.");
        }

        return await SendOnceAsync(requestFactory, requiresAuth, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("BackendApi");
        using var request = requestFactory();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (requiresAuth)
        {
            // Protected calls always use the backend access token stored in the MVC session.
            var session = userSessionService.GetRequiredSession();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        var session = userSessionService.GetSession();

        if (session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            await userSessionService.SignOutAsync(cancellationToken);
            return false;
        }

        try
        {
            // Refresh is handled here instead of via AuthApiService to avoid a circular dependency.
            var refreshedSession = await PostAsync<RefreshSessionRequest, SessionDto>(
                "/api/v1/auth/refresh",
                new RefreshSessionRequest { RefreshToken = session.RefreshToken },
                requiresAuth: false,
                cancellationToken);

            await userSessionService.UpdateSessionAsync(refreshedSession, cancellationToken);
            return true;
        }
        catch (BackendApiException)
        {
            await userSessionService.SignOutAsync(cancellationToken);
            return false;
        }
    }

    private static HttpRequestMessage CreateJsonRequest<TRequest>(
        HttpMethod method,
        string path,
        TRequest payload) =>
        new(method, path)
        {
            Content = JsonContent.Create(payload, options: BackendJson.Options),
        };

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(BackendJson.Options, cancellationToken);
        return payload ?? throw new InvalidOperationException("The backend returned an empty response body.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    private static async Task<BackendApiException> CreateExceptionAsync(
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
            // Fall back to default status-based messages when the payload is not ProblemDetails.
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

/// <summary>
/// Base exception used when the backend returns an unsuccessful HTTP response.
/// </summary>
internal class BackendApiException : Exception
{
    public BackendApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}

/// <summary>
/// Specialized backend exception used when the MVC app can no longer authenticate on behalf of the user.
/// </summary>
internal sealed class BackendAuthenticationExpiredException : BackendApiException
{
    public BackendAuthenticationExpiredException(string message)
        : base(HttpStatusCode.Unauthorized, message)
    {
    }
}

/// <summary>
/// Small ProblemDetails shape used when the backend returns API errors.
/// </summary>
internal sealed class BackendProblemDetails
{
    public int? Status { get; init; }

    public string? Title { get; init; }

    public string? Detail { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; init; }
}

internal static class BackendJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
