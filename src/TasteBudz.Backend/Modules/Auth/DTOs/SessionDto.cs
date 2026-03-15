namespace TasteBudz.Backend.Modules.Auth;

/// <summary>
/// Auth response containing both tokens and the current-user summary needed by the client.
/// </summary>
public sealed record SessionDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    CurrentUserSummaryDto CurrentUser);
