using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteBudz.Web.Mvc.Services.Backend.Contracts;
using TasteBudz.Web.Mvc.Services.Session;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class BackendApiRequestExecutor(
    IHttpClientFactory httpClientFactory,
    IUserSessionService userSessionService,
    AuthApiClient authApiClient)
{
    public Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, path),
            response => BackendApiResponseReader.ReadJsonAsync<TResponse>(response, cancellationToken),
            cancellationToken);

    public Task<TResponse> PutAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<TRequest, TResponse>(HttpMethod.Put, path, payload, cancellationToken);

    public Task<TResponse> PatchAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<TRequest, TResponse>(HttpMethod.Patch, path, payload, cancellationToken);

    private async Task<TResponse> SendJsonAsync<TRequest, TResponse>(
        HttpMethod method,
        string path,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        return await SendAsync(
            () => new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(payload, options: BackendJson.Options),
            },
            response => BackendApiResponseReader.ReadJsonAsync<TResponse>(response, cancellationToken),
            cancellationToken);
    }

    private async Task<TResponse> SendAsync<TResponse>(
        Func<HttpRequestMessage> requestFactory,
        Func<HttpResponseMessage, Task<TResponse>> responseReader,
        CancellationToken cancellationToken)
    {
        using var response = await SendAuthorizedAsync(requestFactory, cancellationToken);
        return await responseReader(response);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var initialResponse = await SendOnceAsync(requestFactory, cancellationToken);

        if (initialResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            return initialResponse;
        }

        initialResponse.Dispose();

        var refreshed = await TryRefreshAsync(cancellationToken);

        if (!refreshed)
        {
            throw new BackendAuthenticationExpiredException("Your session has expired. Please sign in again.");
        }

        return await SendOnceAsync(requestFactory, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var snapshot = userSessionService.GetRequiredSnapshot();
        var client = httpClientFactory.CreateClient("BackendApi");
        var request = requestFactory();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", snapshot.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        var snapshot = userSessionService.GetSnapshot();

        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.RefreshToken))
        {
            await userSessionService.SignOutAsync(cancellationToken);
            return false;
        }

        try
        {
            var refreshedSession = await authApiClient.RefreshAsync(
                new RefreshSessionRequest { RefreshToken = snapshot.RefreshToken },
                cancellationToken);

            await userSessionService.UpdateBackendSessionAsync(refreshedSession, cancellationToken);
            return true;
        }
        catch (BackendAuthenticationExpiredException)
        {
            await userSessionService.SignOutAsync(cancellationToken);
            return false;
        }
    }
}
