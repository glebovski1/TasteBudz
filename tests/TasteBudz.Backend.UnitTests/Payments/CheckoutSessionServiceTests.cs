using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Payments;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Payments;

public sealed class CheckoutSessionServiceTests
{
    [Fact]
    public async Task CreateForEventAsync_ForJoinedParticipantCreatesPendingCheckout()
    {
        var services = CreateServices(checkoutEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);

        var checkout = await services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id);

        Assert.Equal(eventRecord.Id, checkout.EventId);
        Assert.Equal(host.UserId, checkout.UserId);
        Assert.Equal(CheckoutSessionStatus.Pending, checkout.Status);
        Assert.Equal("USD", checkout.Currency);
        Assert.Equal(2500, checkout.SubtotalCents);
        Assert.Equal(0, checkout.DiscountCents);
        Assert.Equal(2500, checkout.TotalCents);
    }

    [Fact]
    public async Task CreateForEventAsync_AppliesActiveSimulatedDiscount()
    {
        var services = CreateServices(checkoutEnabled: true, discountsEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var guest = await RegisterCurrentUserAsync(services, "guest", "guest@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, guest.UserId, EventParticipantState.Joined, null, services.Clock.UtcNow, services.Clock.UtcNow, null, null));
        await SeedDiscountReservationAsync(services, eventRecord.Id);

        var checkout = await services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id);

        Assert.Equal(2500, checkout.SubtotalCents);
        Assert.Equal(500, checkout.DiscountCents);
        Assert.Equal(2000, checkout.TotalCents);
    }

    [Fact]
    public async Task CreateForEventAsync_WhenCallerIsNotJoinedReturnsNotFound()
    {
        var services = CreateServices(checkoutEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var guest = await RegisterCurrentUserAsync(services, "guest", "guest@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.CheckoutSessionService.CreateForEventAsync(guest, eventRecord.Id));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task CreateForEventAsync_WhenEventHasNoSelectedRestaurantReturnsConflict()
    {
        var services = CreateServices(checkoutEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host, hasSelectedRestaurant: false);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task CompleteAsync_PendingSessionMarksCompleted()
    {
        var services = CreateServices(checkoutEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);
        var checkout = await services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id);

        var completed = await services.CheckoutSessionService.CompleteAsync(host, checkout.CheckoutSessionId);

        Assert.Equal(CheckoutSessionStatus.Completed, completed.Status);
        Assert.NotNull(completed.CompletedAtUtc);
    }

    [Fact]
    public async Task CompleteAsync_WhenCallerDoesNotOwnSessionReturnsNotFound()
    {
        var services = CreateServices(checkoutEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var guest = await RegisterCurrentUserAsync(services, "guest", "guest@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);
        var checkout = await services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.CheckoutSessionService.CompleteAsync(guest, checkout.CheckoutSessionId));

        Assert.Equal(404, exception.StatusCode);
    }


    [Fact]
    public async Task CancelAsync_PendingSessionMarksCancelled()
    {
        var services = CreateServices(checkoutEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);
        var checkout = await services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id);

        var cancelled = await services.CheckoutSessionService.CancelAsync(host, checkout.CheckoutSessionId);

        Assert.Equal(CheckoutSessionStatus.Cancelled, cancelled.Status);
        Assert.NotNull(cancelled.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelAsync_CompletedSessionReturnsConflict()
    {
        var services = CreateServices(checkoutEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);
        var checkout = await services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id);
        await services.CheckoutSessionService.CompleteAsync(host, checkout.CheckoutSessionId);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.CheckoutSessionService.CancelAsync(host, checkout.CheckoutSessionId));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task CreateForEventAsync_WhenFeatureDisabledReturnsNotFound()
    {
        var services = CreateServices(checkoutEnabled: false);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var eventRecord = await SeedJoinedEventAsync(services, host);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.CheckoutSessionService.CreateForEventAsync(host, eventRecord.Id));

        Assert.Equal(404, exception.StatusCode);
    }

    private static async Task<CurrentUser> RegisterCurrentUserAsync(TestServices services, string username, string email)
    {
        var session = await services.AuthService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        return new CurrentUser(session.CurrentUser.UserId, session.CurrentUser.Username, session.CurrentUser.Roles);
    }

    private static async Task<Event> SeedJoinedEventAsync(TestServices services, CurrentUser host, bool hasSelectedRestaurant = true)
    {
        var now = services.Clock.UtcNow;
        var selectedRestaurantId = hasSelectedRestaurant
            ? Guid.Parse("11111111-1111-1111-1111-111111111111")
            : (Guid?)null;
        var eventRecord = new Event(
            Guid.NewGuid(),
            host.UserId,
            "Checkout event",
            EventType.Open,
            EventStatus.Open,
            now.AddHours(2),
            now.AddHours(1),
            4,
            2,
            selectedRestaurantId,
            hasSelectedRestaurant ? null : "Japanese",
            null,
            null,
            now,
            now,
            null,
            null);

        await services.EventRepository.SaveAsync(eventRecord);
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, host.UserId, EventParticipantState.Joined, null, now, now, null, null));
        return eventRecord;
    }

    private static async Task SeedDiscountReservationAsync(TestServices services, Guid eventId)
    {
        var now = services.Clock.UtcNow;
        var slot = new RestaurantSlot(
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            now.AddHours(1),
            now.AddHours(3),
            4,
            now.AddMinutes(30),
            2,
            20,
            RestaurantSlotStatus.Open,
            now,
            now,
            null,
            null);
        var reservation = new EventSlotReservation(Guid.NewGuid(), eventId, slot.Id, EventSlotReservationStatus.Active, now, null, null);

        await services.RestaurantOperationsRepository.SaveSlotAsync(slot);
        await services.RestaurantOperationsRepository.SaveReservationAsync(reservation);
    }

    private static TestServices CreateServices(bool checkoutEnabled, bool discountsEnabled = false)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 2, 18, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var restaurantRepository = new InMemoryRestaurantRepository(store);
        var restaurantOperationsRepository = new InMemoryRestaurantOperationsRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var checkoutRepository = new InMemoryCheckoutSessionRepository(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var discountEligibilityService = new DiscountEligibilityService(restaurantOperationsRepository, eventRepository, new TestFeatureFlagService(checkoutEnabled, discountsEnabled), clock);
        var checkoutSessionService = new CheckoutSessionService(checkoutRepository, eventRepository, restaurantRepository, new TestFeatureFlagService(checkoutEnabled, discountsEnabled), discountEligibilityService, clock);

        return new TestServices(
            clock,
            authService,
            eventRepository,
            restaurantOperationsRepository,
            checkoutSessionService);
    }

    private sealed record TestServices(
        TestClock Clock,
        AuthService AuthService,
        IEventRepository EventRepository,
        IRestaurantOperationsRepository RestaurantOperationsRepository,
        CheckoutSessionService CheckoutSessionService);

}
