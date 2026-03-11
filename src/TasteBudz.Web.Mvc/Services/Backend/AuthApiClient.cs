using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class AuthApiClient(IHttpClientFactory httpClientFactory)
{
    public Task<SessionDto> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RegisterUserRequest, SessionDto>("/api/v1/auth/register", request, cancellationToken);

    public Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<LoginRequest, SessionDto>("/api/v1/auth/login", request, cancellationToken);

    public Task<SessionDto> RefreshAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RefreshSessionRequest, SessionDto>("/api/v1/auth/refresh", request, cancellationToken);

    public async Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        await BackendApiResponseReader.EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync(path, request, BackendJson.Options, cancellationToken);
        return await BackendApiResponseReader.ReadJsonAsync<TResponse>(response, cancellationToken);
    }

    private HttpClient CreateClient() => httpClientFactory.CreateClient("BackendApiAuth");
}
