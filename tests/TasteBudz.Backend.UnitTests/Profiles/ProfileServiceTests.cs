// Unit tests for current-user profile updates.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Profiles;

/// <summary>
/// Verifies profile updates touch both account and profile records where appropriate.
/// </summary>
public sealed class ProfileServiceTests
{
    [Fact]
    public async Task UpdateMyProfileAsync_UpdatesUsernameAndProfileFields()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var userId = Guid.NewGuid();
        var account = new UserAccount(userId, "alex", "ALEX", "alex@example.com", "ALEX@EXAMPLE.COM", "hash", AccountStatus.Active, new[] { UserRole.User }, clock.UtcNow, clock.UtcNow, null);
        await authRepository.CreateAccountAsync(account);
        await profileRepository.SaveProfileAsync(new UserProfile(userId, "Alex", null, "45220", SocialGoal.Friends, clock.UtcNow, clock.UtcNow));

        var service = new ProfileService(authRepository, profileRepository, clock);

        var updated = await service.UpdateMyProfileAsync(userId, new UpdateMyProfileRequest
        {
            Username = "alexander",
            DisplayName = "Alexander",
            Bio = "Sushi first.",
            HomeAreaZipCode = "45219",
            SocialGoal = SocialGoal.Networking,
        });

        Assert.Equal("alexander", updated.Username);
        Assert.Equal("Alexander", updated.DisplayName);
        Assert.Equal("Sushi first.", updated.Bio);
        Assert.Equal("45219", updated.HomeAreaZipCode);
        Assert.Equal(SocialGoal.Networking, updated.SocialGoal);
    }
}