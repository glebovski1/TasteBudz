using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class BackendSessionCookieEventsTests
{
    [Fact]
    public async Task ValidatePrincipal_WhenBackendSessionExists_KeepsPrincipalAuthenticated()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();

        var validationContext = CreateValidationContext(context);
        var events = new BackendSessionCookieEvents(context.UserSessionService);

        await events.ValidatePrincipal(validationContext);

        Assert.NotNull(validationContext.Principal);
        Assert.Null(context.LastSignOutScheme);
        Assert.True(context.HttpContext.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task ValidatePrincipal_WhenBackendSessionIsMissing_RejectsPrincipalAndSignsOut()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        context.ClearStoredSession();

        var validationContext = CreateValidationContext(context);
        var events = new BackendSessionCookieEvents(context.UserSessionService);

        await events.ValidatePrincipal(validationContext);

        Assert.Null(validationContext.Principal);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, context.LastSignOutScheme);
        Assert.False(context.HttpContext.User.Identity?.IsAuthenticated ?? true);
    }

    private static CookieValidatePrincipalContext CreateValidationContext(BackendApiServiceTestContext context)
    {
        var scheme = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme,
            typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(
            context.HttpContext.User,
            new AuthenticationProperties(),
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new CookieValidatePrincipalContext(
            context.HttpContext,
            scheme,
            new CookieAuthenticationOptions(),
            ticket);
    }
}
