// Authenticates requests by resolving opaque bearer tokens back to stored sessions.
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Infrastructure.Time;

namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Custom authentication handler that maps stored session tokens to ASP.NET Core claims.
/// </summary>
public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAuthRepository authRepository,
    IClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Validates the bearer token, loads the owning account, and creates the claims principal.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        var queryAccessToken = Request.Query["access_token"].ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader) && string.IsNullOrWhiteSpace(queryAccessToken))
        {
            return AuthenticateResult.NoResult();
        }

        const string bearerPrefix = "Bearer ";
        string token;

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Unsupported authentication scheme.");
            }

            token = authorizationHeader[bearerPrefix.Length..].Trim();
        }
        else
        {
            token = queryAccessToken.Trim();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Missing bearer token.");
        }

        // Tokens are opaque. The session repository is the source of truth for revocation and expiry.
        var session = await authRepository.GetSessionByAccessTokenAsync(token, Context.RequestAborted);

        if (session is null || session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= clock.UtcNow)
        {
            return AuthenticateResult.Fail("The access token is invalid or expired.");
        }

        var account = await authRepository.GetByIdAsync(session.UserId, Context.RequestAborted);

        if (account is null || account.Status != AccountStatus.Active)
        {
            return AuthenticateResult.Fail("The access token does not map to an active account.");
        }

        // Keep the claim set intentionally small because the app rehydrates anything richer from storage.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Email, account.Email),
        };

        claims.AddRange(account.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SessionAuthenticationDefaults.Scheme));
        var ticket = new AuthenticationTicket(principal, SessionAuthenticationDefaults.Scheme);
        return AuthenticateResult.Success(ticket);
    }
}
