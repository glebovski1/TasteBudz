// Unit tests for account registration defaults and session creation.
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Domain;
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

    [Fact]
    public async Task ResetPasswordAsync_WithAdminToken_UpdatesPasswordAndRevokesSessions()
    {
        var (service, authRepository, _) = CreateService();
        var user = await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });
        var admin = new CurrentUser(Guid.NewGuid(), "admin", new[] { UserRole.Admin });

        var token = await service.CreatePasswordResetTokenAsync(admin, new CreatePasswordResetTokenRequest
        {
            UsernameOrEmail = "alex@example.com",
        });
        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = token.ResetToken,
            NewPassword = "N3wPa$$w0rd",
        });

        var login = await service.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "alex",
            Password = "N3wPa$$w0rd",
        });
        var oldSession = await authRepository.GetSessionByAccessTokenAsync(user.AccessToken);
        var storedTokens = await authRepository.ListPasswordResetTokensForUserAsync(user.CurrentUser.UserId);

        Assert.Equal(user.CurrentUser.UserId, token.UserId);
        Assert.Equal(user.CurrentUser.UserId, login.CurrentUser.UserId);
        Assert.NotNull(oldSession!.RevokedAtUtc);
        Assert.NotNull(storedTokens.Single().UsedAtUtc);
    }

    [Fact]
    public async Task CreatePasswordResetTokenAsync_WhenCurrentUserIsNotAdmin_ReturnsForbidden()
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
            service.CreatePasswordResetTokenAsync(
                new CurrentUser(Guid.NewGuid(), "notadmin", new[] { UserRole.User }),
                new CreatePasswordResetTokenRequest { UsernameOrEmail = "alex" }));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task CreatePasswordResetRequestAsync_KnownAndUnknownUsersReturnSameGenericMessageWithoutDisclosure()
    {
        var (service, authRepository, _) = CreateService();
        var session = await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        var known = await service.CreatePasswordResetRequestAsync(new CreatePasswordResetRequestRequest
        {
            Username = "alex",
            Message = "Lost access to my email.",
        });
        var unknown = await service.CreatePasswordResetRequestAsync(new CreatePasswordResetRequestRequest
        {
            Username = "missing-user",
            Message = "Please help me reset my password.",
        });
        var requests = await authRepository.ListOpenPasswordResetRequestsAsync();
        var knownRequest = Assert.Single(requests, request => request.Username == "alex");
        var unknownRequest = Assert.Single(requests, request => request.Username == "missing-user");

        Assert.Equal(known.Message, unknown.Message);
        Assert.Equal(session.CurrentUser.UserId, knownRequest.MatchedUserId);
        Assert.Null(unknownRequest.MatchedUserId);
    }

    [Fact]
    public async Task CreatePasswordResetTokenAsync_WithPasswordResetRequestId_ClosesRequestAndUsesMatchedUser()
    {
        var (service, authRepository, _) = CreateService();
        var session = await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });
        var admin = new CurrentUser(Guid.NewGuid(), "admin", new[] { UserRole.Admin });

        await service.CreatePasswordResetRequestAsync(new CreatePasswordResetRequestRequest
        {
            Username = "alex",
            Message = "Need help signing in.",
        });
        var openRequest = Assert.Single(await authRepository.ListOpenPasswordResetRequestsAsync());

        var token = await service.CreatePasswordResetTokenAsync(
            admin,
            new CreatePasswordResetTokenRequest
            {
                PasswordResetRequestId = openRequest.Id,
            });
        var storedRequest = await authRepository.GetPasswordResetRequestAsync(openRequest.Id);

        Assert.Equal(session.CurrentUser.UserId, token.UserId);
        Assert.NotNull(storedRequest);
        Assert.NotNull(storedRequest!.ClosedAtUtc);
        Assert.Equal(admin.UserId, storedRequest.ClosedByUserId);
        Assert.Empty(await authRepository.ListOpenPasswordResetRequestsAsync());
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenIsExpired_ReturnsBadRequest()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var (service, _, _) = CreateService(clock);
        await service.RegisterAsync(new RegisterUserRequest
        {
            Username = "alex",
            Email = "alex@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });
        var token = await service.CreatePasswordResetTokenAsync(
            new CurrentUser(Guid.NewGuid(), "admin", new[] { UserRole.Admin }),
            new CreatePasswordResetTokenRequest { UsernameOrEmail = "alex" });

        clock.Advance(TimeSpan.FromHours(25));
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.ResetPasswordAsync(new ResetPasswordRequest
            {
                Token = token.ResetToken,
                NewPassword = "N3wPa$$w0rd",
            }));

        Assert.Equal(400, exception.StatusCode);
    }

    private static (AuthService Service, InMemoryAuthRepository AuthRepository, InMemoryProfileRepository ProfileRepository) CreateService(TestClock? clock = null)
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var service = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock ?? new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero)));
        return (service, authRepository, profileRepository);
    }
}
