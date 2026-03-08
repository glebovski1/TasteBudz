// Shared constants for the custom bearer-token authentication handler.
namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Keeps the scheme name in one place for DI registration and ticket creation.
/// </summary>
public static class SessionAuthenticationDefaults
{
    public const string Scheme = "Bearer";
}