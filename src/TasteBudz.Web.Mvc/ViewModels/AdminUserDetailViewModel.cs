using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class AdminUserDetailViewModel
{
    public ModerationUserDetailDto Detail { get; init; } = null!;

    public bool HasActiveFullBan =>
        FullBanScopes.All(scope => Detail.Restrictions.Any(restriction =>
            restriction.Scope == scope &&
            IsActiveNow(restriction)));

    private static readonly RestrictionScope[] FullBanScopes =
    [
        RestrictionScope.DiscoveryVisibility,
        RestrictionScope.ChatSend,
        RestrictionScope.EventJoin,
        RestrictionScope.EventCreate,
    ];

    private static bool IsActiveNow(RestrictionDto restriction)
    {
        var now = DateTimeOffset.UtcNow;

        return restriction.Status == RestrictionStatus.Active &&
            restriction.StartsAtUtc <= now &&
            (!restriction.ExpiresAtUtc.HasValue || restriction.ExpiresAtUtc.Value > now) &&
            !restriction.RevokedAtUtc.HasValue;
    }
}
