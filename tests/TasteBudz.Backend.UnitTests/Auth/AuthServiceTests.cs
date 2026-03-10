// Unit tests for account registration defaults and session creation.
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Auth;

/// <summary>
/// Verifies the auth service's core registration behavior.
/// </summary>
public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_DuplicateUsernameReturnsConflict()
    {
        var (service, _, _) = CreateService();

        await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.RegisterAsync(new RegisterUserRequest
            {
                Username = "alex",
                Email = "other@example.com",
                Password = "Pa$$w0rd123",
                ZipCode = "45220",
            }));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task RegisterAsync_CreatesSessionAndDefaultProfileState()
    {
        var (service, authRepository, profileRepository) = CreateService();

        var session = await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        var account = await authRepository.GetByIdAsync(session.CurrentUser.UserId);
        var profile = await profileRepository.GetProfileAsync(session.CurrentUser.UserId);
        var preferences = await profileRepository.GetPreferencesAsync(session.CurrentUser.UserId);
        var privacySettings = await profileRepository.GetPrivacySettingsAsync(session.CurrentUser.UserId);

        Assert.NotNull(account);
        Assert.NotNull(profile);
        Assert.NotNull(preferences);
        Assert.NotNull(privacySettings);
        Assert.Equal("alex", profile!.DisplayName);
        Assert.True(privacySettings!.DiscoveryEnabled);
        Assert.Empty(preferences!.CuisineTags);
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));
    }

    [Fact]
    public async Task LoginAsync_WithEmailReturnsNewSession()
    {
        var (service, _, _) = CreateService();
        await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        var session = await service.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "alex@example.com",
            Password = "Pa$$w0rd123",
        });

        Assert.Equal("alex", session.CurrentUser.Username);
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
    }

    [Fact]
    public async Task RefreshAsync_RotatesRefreshToken()
    {
        var (service, _, _) = CreateService();
        var session = await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        var refreshed = await service.RefreshAsync(new RefreshSessionRequest
        {
            RefreshToken = session.RefreshToken,
        });

        Assert.NotEqual(session.RefreshToken, refreshed.RefreshToken);
        Assert.NotEqual(session.AccessToken, refreshed.AccessToken);
    }

    private static (AuthService Service, InMemoryAuthRepository AuthRepository, InMemoryProfileRepository ProfileRepository) CreateService()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var service = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        return (service, authRepository, profileRepository);
    }
}
