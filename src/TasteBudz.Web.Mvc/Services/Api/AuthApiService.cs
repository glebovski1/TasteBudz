using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Web.Mvc.Services.Http;

namespace TasteBudz.Web.Mvc.Services.Api;

/// <summary>
/// Thin wrapper over backend authentication routes.
/// It keeps route strings out of controllers and leaves HTTP mechanics to BackendHttpClient.
/// </summary>
public sealed class AuthApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public AuthApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<SessionDto> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<RegisterUserRequest, SessionDto>(
            "/api/v1/auth/register",
            request,
            requiresAuth: false,
            cancellationToken);

    public Task<SessionDto> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<LoginRequest, SessionDto>(
            "/api/v1/auth/login",
            request,
            requiresAuth: false,
            cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            "/api/v1/auth/logout",
            cancellationToken: cancellationToken);
}
