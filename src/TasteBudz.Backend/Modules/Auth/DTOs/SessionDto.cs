namespace TasteBudz.Backend.Modules.Auth;

public sealed record SessionDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    CurrentUserSummaryDto CurrentUser);
