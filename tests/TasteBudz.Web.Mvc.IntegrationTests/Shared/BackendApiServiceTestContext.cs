using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;

public sealed class BackendApiServiceTestContext
{
    private readonly RecordingAuthenticationService authenticationService = new();
    private readonly UserSessionService userSessionService;
    private readonly SingleClientFactory httpClientFactory;

    public BackendApiServiceTestContext()
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authenticationService)
            .BuildServiceProvider();

        HttpContext = new DefaultHttpContext
        {
            RequestServices = services,
            Session = new InMemorySession(),
        };

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = HttpContext,
        };

        userSessionService = new UserSessionService(httpContextAccessor);
        BackendHandler = new StubBackendApiHandler();
        httpClientFactory = new SingleClientFactory(new HttpClient(BackendHandler)
        {
            BaseAddress = new Uri("https://backend.test", UriKind.Absolute),
        });
    }

    public StubBackendApiHandler BackendHandler { get; }

    public HttpContext HttpContext { get; }

    public string? LastSignInScheme => authenticationService.LastSignInScheme;

    public string? LastSignOutScheme => authenticationService.LastSignOutScheme;

    public UserSessionService UserSessionService => userSessionService;

    public SessionDto? GetStoredSession() => userSessionService.GetSession();

    public async Task SignInAsync(SessionDto? session = null)
    {
        await userSessionService.SignInAsync(session ?? MvcTestHelpers.CreateSession());
    }

    public void ClearStoredSession() => HttpContext.Session.Clear();

    public BackendHttpClient CreateBackendHttpClient() => new(httpClientFactory, userSessionService);

    public TService CreateService<TService>(Func<BackendHttpClient, TService> factory) =>
        factory(CreateBackendHttpClient());
}
