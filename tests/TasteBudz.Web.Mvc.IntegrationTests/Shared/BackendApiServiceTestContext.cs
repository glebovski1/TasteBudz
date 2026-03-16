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

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            Session = new InMemorySession(),
        };

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext,
        };

        userSessionService = new UserSessionService(httpContextAccessor);
        BackendHandler = new StubBackendApiHandler();
        httpClientFactory = new SingleClientFactory(new HttpClient(BackendHandler)
        {
            BaseAddress = new Uri("https://backend.test", UriKind.Absolute),
        });
    }

    public StubBackendApiHandler BackendHandler { get; }

    public string? LastSignInScheme => authenticationService.LastSignInScheme;

    public SessionDto? GetStoredSession() => userSessionService.GetSession();

    public async Task SignInAsync(SessionDto? session = null)
    {
        await userSessionService.SignInAsync(session ?? MvcTestHelpers.CreateSession());
    }

    public BackendHttpClient CreateBackendHttpClient() => new(httpClientFactory, userSessionService);

    public TService CreateService<TService>(Func<BackendHttpClient, TService> factory) =>
        factory(CreateBackendHttpClient());

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient httpClient;

        public SingleClientFactory(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpClient CreateClient(string name) => httpClient;
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public string? LastSignInScheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            LastSignInScheme = scheme;
            context.User = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> values = [];

        public IEnumerable<string> Keys => values.Keys;

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public bool IsAvailable => true;

        public void Clear() => values.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => values.Remove(key);

        public void Set(string key, byte[] value) => values[key] = value;

        public bool TryGetValue(string key, out byte[] value) => values.TryGetValue(key, out value!);
    }
}
