using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Session;

public sealed record BackendSessionSnapshot(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    CurrentUserSummaryDto CurrentUser);
