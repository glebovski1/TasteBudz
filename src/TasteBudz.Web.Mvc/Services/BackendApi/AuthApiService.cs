using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over backend authentication routes.
/// It keeps route strings out of controllers and leaves HTTP mechanics to BackendHttpClient.
/// Register this class in Program.cs, then ask for AuthApiService in a controller constructor.
/// ASP.NET Core DI will create it automatically and supply it to the controller.
/// </summary>
public sealed class AuthApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public AuthApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    /// <summary>
    /// Sends the registration form data to the backend and returns the backend session DTO.
    /// </summary>
    public Task<SessionDto> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<RegisterUserRequest, SessionDto>(
            "/api/v1/auth/register",
            request,
            requiresAuth: false,
            cancellationToken);

    /// <summary>
    /// Sends the login form data to the backend and returns the backend session DTO.
    /// </summary>
    public Task<SessionDto> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<LoginRequest, SessionDto>(
            "/api/v1/auth/login",
            request,
            requiresAuth: false,
            cancellationToken);

    public Task<PasswordResetTokenDto> CreatePasswordResetTokenAsync(
        CreatePasswordResetTokenRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreatePasswordResetTokenRequest, PasswordResetTokenDto>(
            "/api/v1/admin/users/password-reset-tokens",
            request,
            cancellationToken: cancellationToken);

    public Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            "/api/v1/auth/password-reset",
            request,
            requiresAuth: false,
            cancellationToken);

    /// <summary>
    /// Tells the backend to invalidate the current session token pair.
    /// The local MVC sign-out still happens separately in UserSessionService.
    /// </summary>
    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            "/api/v1/auth/logout",
            cancellationToken: cancellationToken);
}
