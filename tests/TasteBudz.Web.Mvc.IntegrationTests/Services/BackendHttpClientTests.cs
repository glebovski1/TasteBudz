using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class BackendHttpClientTests
{
    [Fact]
    public async Task GetAsync_WhenRefreshFailsAfterAnotherRequestAlreadyUpdatedSession_RetriesWithNewStoredToken()
    {
        var authenticationService = new RecordingAuthenticationService();
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

        var userSessionService = new UserSessionService(httpContextAccessor);
        await userSessionService.SignInAsync(MvcTestHelpers.CreateSession());

        var refreshedSession = MvcTestHelpers.CreateSession(
            accessToken: "refreshed-access-token",
            refreshToken: "refreshed-refresh-token");

        var backendHandler = new StubBackendApiHandler();
        backendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Problem(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Access token expired."));
        backendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/auth/refresh",
            (_, _) =>
            {
                // Simulate another concurrent request successfully refreshing before this request handles the failure.
                userSessionService.UpdateSessionAsync(refreshedSession).GetAwaiter().GetResult();

                return StubBackendApiHandler.Problem(
                    HttpStatusCode.Unauthorized,
                    "Unauthorized",
                    "Refresh token already rotated.");
            });
        backendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/onboarding/status",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new OnboardingStatusDto(true, Array.Empty<string>())));

        var httpClientFactory = new SingleClientFactory(new HttpClient(backendHandler)
        {
            BaseAddress = new Uri("https://backend.test", UriKind.Absolute),
        });

        var backendHttpClient = new BackendHttpClient(httpClientFactory, userSessionService);

        var result = await backendHttpClient.GetAsync<OnboardingStatusDto>("/api/v1/onboarding/status");
        var storedSession = userSessionService.GetSession();
        var requests = backendHandler.Requests.ToArray();

        Assert.True(result.IsComplete);
        Assert.NotNull(storedSession);
        Assert.Equal("refreshed-access-token", storedSession.AccessToken);
        Assert.Equal("refreshed-refresh-token", storedSession.RefreshToken);
        Assert.Equal(2, requests.Count(request => request.Method == HttpMethod.Get));
        Assert.Equal("access-token", requests[0].AuthorizationParameter);
        Assert.Equal("refreshed-access-token", requests[2].AuthorizationParameter);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authenticationService.LastSignInScheme);
        backendHandler.AssertDrained();
    }

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
