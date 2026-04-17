using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Centralizes all HTTP communication with the backend API.
/// Controllers and feature API services should never deal with raw HttpClient behavior directly.
/// This class hides the repeated infrastructure work:
/// building requests, adding auth headers, sending JSON, reading JSON, handling ProblemDetails,
/// and retrying one time after a refresh when the backend says the access token expired.
/// </summary>
public sealed class BackendHttpClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly UserSessionService userSessionService;
    private readonly IBackendApiBaseAddressProvider? baseAddressProvider;

    public BackendHttpClient(
        IHttpClientFactory httpClientFactory,
        UserSessionService userSessionService)
        : this(httpClientFactory, userSessionService, null)
    {
    }

    public BackendHttpClient(
        IHttpClientFactory httpClientFactory,
        UserSessionService userSessionService,
        IBackendApiBaseAddressProvider? baseAddressProvider)
    {
        this.httpClientFactory = httpClientFactory;
        this.userSessionService = userSessionService;
        this.baseAddressProvider = baseAddressProvider;
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

    public Task<TResponse> PostAsync<TResponse>(
        string path,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => new HttpRequestMessage(HttpMethod.Post, path),
            requiresAuth,
            cancellationToken);

    public Task<TResponse> PostMultipartAsync<TResponse>(
        string path,
        Func<MultipartFormDataContent> contentFactory,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(
            () => new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = contentFactory(),
            },
            requiresAuth,
            cancellationToken);

    public Task PostAsync<TRequest>(
        string path,
        TRequest payload,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
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

    public Task DeleteAsync(
        string path,
        bool requiresAuth = true,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, path),
            requiresAuth,
            cancellationToken);

    public Task<BackendFileResponse> GetFileAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        SendFileAsync(
            () => new HttpRequestMessage(HttpMethod.Get, path),
            requiresAuth: true,
            cancellationToken);

    private async Task<TResponse> SendAsync<TResponse>(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        // Step 1:
        // Send the request, including refresh/retry behavior when needed.
        using var response = await SendWithRefreshAsync(requestFactory, requiresAuth, cancellationToken);

        // Step 2:
        // If the backend returned success, deserialize the JSON body into the DTO the caller asked for.
        // If the backend returned failure, this helper throws a backend-specific exception instead.
        return await ReadJsonAsync<TResponse>(response, cancellationToken);
    }

    private async Task SendNoContentAsync(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        // This path is used for endpoints that succeed without returning a response body.
        using var response = await SendWithRefreshAsync(requestFactory, requiresAuth, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<BackendFileResponse> SendFileAsync(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithRefreshAsync(requestFactory, requiresAuth, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new BackendFileResponse(content, contentType);
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        // First attempt:
        // send the request exactly as the caller asked for it.
        var response = await SendOnceAsync(requestFactory, requiresAuth, cancellationToken);

        if (!requiresAuth || response.StatusCode != HttpStatusCode.Unauthorized)
        {
            // If the call does not require auth, or it succeeded, or it failed for a reason other than 401,
            // return that response directly to the next layer.
            return response;
        }

        response.Dispose();

        // Only protected calls reach this branch.
        // The backend said the access token is no longer accepted, so try to refresh once.
        var refreshed = await TryRefreshAsync(cancellationToken);

        if (!refreshed)
        {
            // Refresh failed, so the MVC app can no longer act on behalf of the user.
            throw new BackendAuthenticationExpiredException("Your session has expired. Please sign in again.");
        }

        // Refresh succeeded, so rebuild the original request and try the protected call one more time.
        return await SendOnceAsync(requestFactory, requiresAuth, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        Func<HttpRequestMessage> requestFactory,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        // Create the named HttpClient that Program.cs registered.
        // This keeps base URL and other shared HTTP configuration in one place.
        var client = httpClientFactory.CreateClient("BackendApi");
        client.BaseAddress ??= baseAddressProvider?.GetBaseAddress()
            ?? throw new InvalidOperationException("The BackendApi HttpClient must have a BaseAddress or an IBackendApiBaseAddressProvider.");
        using var request = requestFactory();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (requiresAuth)
        {
            // Protected calls always use the backend access token stored in the MVC session.
            // UserSessionService reads the session for the current browser request.
            var session = userSessionService.GetRequiredSession();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        // Send the HTTP request to the backend API.
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        // Read the current backend session from ASP.NET session storage.
        var session = userSessionService.GetSession();

        if (session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            // Without a refresh token there is nothing to recover, so clear local auth immediately.
            await userSessionService.SignOutAsync(cancellationToken);
            return false;
        }

        // Remember which refresh token this specific request started with.
        // If another request refreshes first, the stored session will contain a different refresh token.
        var attemptedRefreshToken = session.RefreshToken;

        try
        {
            // Refresh is handled here instead of via AuthApiService.
            // Keeping it here avoids a circular dependency where BackendHttpClient would need AuthApiService
            // while AuthApiService already depends on BackendHttpClient.
            var refreshedSession = await PostAsync<RefreshSessionRequest, SessionDto>(
                "/api/v1/auth/refresh",
                new RefreshSessionRequest { RefreshToken = session.RefreshToken },
                requiresAuth: false,
                cancellationToken);

            // Save the new tokens and rebuild the MVC cookie claims from the refreshed backend session.
            await userSessionService.UpdateSessionAsync(refreshedSession, cancellationToken);
            return true;
        }
        catch (BackendApiException)
        {
            var currentSession = userSessionService.GetSession();

            if (currentSession is not null &&
                !string.IsNullOrWhiteSpace(currentSession.RefreshToken) &&
                !string.Equals(currentSession.RefreshToken, attemptedRefreshToken, StringComparison.Ordinal))
            {
                // Another request already replaced the old token pair with a newer one.
                // Reuse that new session instead of clearing local auth and forcing a logout.
                return true;
            }

            // Refresh really failed for the currently stored session, so local auth must be cleared.
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
            // Convert backend failures into readable MVC-side exceptions before any controller sees them.
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        // Success responses are expected to contain JSON for the requested DTO type.
        var payload = await response.Content.ReadFromJsonAsync<T>(BackendJson.Options, cancellationToken);
        return payload ?? throw new InvalidOperationException("The backend returned an empty response body.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            // No-body calls still need the same backend error translation.
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
            // The backend usually returns standard ProblemDetails JSON for failures.
            problem = await response.Content.ReadFromJsonAsync<BackendProblemDetails>(BackendJson.Options, cancellationToken);
        }
        catch
        {
            // If the payload is not valid ProblemDetails JSON, fall back to status-code-based messages.
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

public sealed record BackendFileResponse(byte[] Content, string ContentType);

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
        // Use ASP.NET-style web defaults and string enum values so MVC matches the backend JSON shape.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
