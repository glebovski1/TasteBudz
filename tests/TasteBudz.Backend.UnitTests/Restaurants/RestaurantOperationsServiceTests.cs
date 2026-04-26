using System.Text.Json;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Restaurants;

public sealed class RestaurantOperationsServiceTests
{
    [Fact]
    public async Task GrantAndRevokeAsync_UpdatesRestaurantAdminRole()
    {
        var services = CreateServices();
        var admin = await RegisterCurrentUserAsync(services, "admin", "admin@example.com", UserRole.Admin);
        var assignee = await services.AuthService.RegisterAsync(new RegisterUserRequest
        {
            Username = "manager",
            Email = "manager@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var assignment = await services.AssignmentService.GrantAsync(
            admin,
            restaurantId,
            new CreateRestaurantAdminAssignmentRequest { Username = "manager" });
        var afterGrant = await services.AuthRepository.GetByIdAsync(assignee.CurrentUser.UserId);

        await services.AssignmentService.RevokeAsync(admin, restaurantId, assignee.CurrentUser.UserId);
        var afterRevoke = await services.AuthRepository.GetByIdAsync(assignee.CurrentUser.UserId);

        Assert.Equal(restaurantId, assignment.RestaurantId);
        Assert.Contains(UserRole.RestaurantAdmin, afterGrant!.Roles);
        Assert.DoesNotContain(UserRole.RestaurantAdmin, afterRevoke!.Roles);
    }

    [Fact]
    public async Task ReserveAsync_EvaluatesDiscountFromJoinedParticipants()
    {
        var services = CreateServices(discountsEnabled: true);
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var guest = await RegisterCurrentUserAsync(services, "guest", "guest@example.com");
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var slot = await SeedOpenSlotAsync(services, restaurantId, threshold: 2, discountPercent: 20);
        var eventRecord = await SeedEventAsync(services, host.UserId, restaurantId, capacity: 4);
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, host.UserId, EventParticipantState.Joined, null, services.Clock.UtcNow, services.Clock.UtcNow, null, null));

        var reservation = await services.ReservationService.ReserveAsync(
            host,
            eventRecord.Id,
            new ReserveEventSlotRequest { SlotId = slot.Id });
        var inactive = await services.DiscountEligibilityService.EvaluateForEventAsync(eventRecord.Id);

        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, guest.UserId, EventParticipantState.Joined, null, services.Clock.UtcNow, services.Clock.UtcNow, null, null));
        var active = await services.DiscountEligibilityService.EvaluateForEventAsync(eventRecord.Id);

        Assert.Equal(slot.Id, reservation.SlotId);
        Assert.NotNull(inactive);
        Assert.False(inactive!.IsActive);
        Assert.NotNull(active);
        Assert.True(active!.IsActive);
        Assert.Equal(20, active.DiscountPercent);
    }

    [Fact]
    public async Task CreateAsync_WithDiscountThresholdAndPercent_PersistsPercentage()
    {
        var services = CreateServices();
        var manager = await RegisterCurrentUserAsync(services, "manager", "manager@example.com", UserRole.RestaurantAdmin);
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await services.RestaurantOperationsRepository.SaveAssignmentAsync(new RestaurantAdminAssignment(restaurantId, manager.UserId, services.Clock.UtcNow, null));

        var slot = await services.SlotService.CreateAsync(
            manager,
            restaurantId,
            new CreateRestaurantSlotRequest
            {
                StartsAtUtc = services.Clock.UtcNow.AddHours(1),
                EndsAtUtc = services.Clock.UtcNow.AddHours(3),
                Capacity = 4,
                CutoffAtUtc = services.Clock.UtcNow.AddMinutes(30),
                MinThresholdForDiscount = 3,
                DiscountPercent = 25,
            });

        Assert.Equal(3, slot.MinThresholdForDiscount);
        Assert.Equal(25, slot.DiscountPercent);
    }

    [Theory]
    [InlineData(2, null)]
    [InlineData(null, 15)]
    [InlineData(2, 0)]
    [InlineData(2, 101)]
    [InlineData(5, 15)]
    public async Task CreateAsync_WithInvalidDiscountConfiguration_ReturnsBadRequest(int? threshold, int? discountPercent)
    {
        var services = CreateServices();
        var manager = await RegisterCurrentUserAsync(services, "manager", "manager@example.com", UserRole.RestaurantAdmin);
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await services.RestaurantOperationsRepository.SaveAssignmentAsync(new RestaurantAdminAssignment(restaurantId, manager.UserId, services.Clock.UtcNow, null));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.SlotService.CreateAsync(
                manager,
                restaurantId,
                new CreateRestaurantSlotRequest
                {
                    StartsAtUtc = services.Clock.UtcNow.AddHours(1),
                    EndsAtUtc = services.Clock.UtcNow.AddHours(3),
                    Capacity = 4,
                    CutoffAtUtc = services.Clock.UtcNow.AddMinutes(30),
                    MinThresholdForDiscount = threshold,
                    DiscountPercent = discountPercent,
                }));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WithDiscountThresholdAndPercent_PersistsPercentage()
    {
        var services = CreateServices();
        var manager = await RegisterCurrentUserAsync(services, "manager", "manager@example.com", UserRole.RestaurantAdmin);
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await services.RestaurantOperationsRepository.SaveAssignmentAsync(new RestaurantAdminAssignment(restaurantId, manager.UserId, services.Clock.UtcNow, null));
        var slot = await SeedOpenSlotAsync(services, restaurantId, threshold: null);

        var updated = await services.SlotService.UpdateAsync(
            manager,
            slot.Id,
            new UpdateRestaurantSlotRequest
            {
                MinThresholdForDiscount = 3,
                DiscountPercent = 30,
            });

        Assert.Equal(3, updated.MinThresholdForDiscount);
        Assert.Equal(30, updated.DiscountPercent);
    }

    [Fact]
    public async Task UpdateAsync_WithClearDiscount_RemovesExistingDiscount()
    {
        var services = CreateServices();
        var manager = await RegisterCurrentUserAsync(services, "manager", "manager@example.com", UserRole.RestaurantAdmin);
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await services.RestaurantOperationsRepository.SaveAssignmentAsync(new RestaurantAdminAssignment(restaurantId, manager.UserId, services.Clock.UtcNow, null));
        var slot = await SeedOpenSlotAsync(services, restaurantId, threshold: 3, discountPercent: 25);
        var request = JsonSerializer.Deserialize<UpdateRestaurantSlotRequest>(
            """{"clearDiscount":true}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var updated = await services.SlotService.UpdateAsync(manager, slot.Id, request);

        Assert.Null(updated.MinThresholdForDiscount);
        Assert.Null(updated.DiscountPercent);
    }

    [Theory]
    [InlineData(2, null)]
    [InlineData(null, 15)]
    [InlineData(2, 0)]
    [InlineData(2, 101)]
    [InlineData(5, 15)]
    public async Task UpdateAsync_WithInvalidDiscountConfiguration_ReturnsBadRequest(int? threshold, int? discountPercent)
    {
        var services = CreateServices();
        var manager = await RegisterCurrentUserAsync(services, "manager", "manager@example.com", UserRole.RestaurantAdmin);
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await services.RestaurantOperationsRepository.SaveAssignmentAsync(new RestaurantAdminAssignment(restaurantId, manager.UserId, services.Clock.UtcNow, null));
        var slot = await SeedOpenSlotAsync(services, restaurantId, threshold: null);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.SlotService.UpdateAsync(
                manager,
                slot.Id,
                new UpdateRestaurantSlotRequest
                {
                    MinThresholdForDiscount = threshold,
                    DiscountPercent = discountPercent,
                }));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task CancelAsync_WithActiveReservation_CancelsLinkedEventAndReservation()
    {
        var services = CreateServices();
        var globalAdmin = await RegisterCurrentUserAsync(services, "admin", "admin@example.com", UserRole.Admin);
        var restaurantAdminSession = await services.AuthService.RegisterAsync(new RegisterUserRequest
        {
            Username = "manager",
            Email = "manager@example.com",
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });
        var host = await RegisterCurrentUserAsync(services, "host", "host@example.com");
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await services.AssignmentService.GrantAsync(
            globalAdmin,
            restaurantId,
            new CreateRestaurantAdminAssignmentRequest { Username = "manager" });
        var managerAccount = await services.AuthRepository.GetByIdAsync(restaurantAdminSession.CurrentUser.UserId);
        var manager = new CurrentUser(managerAccount!.Id, managerAccount.Username, managerAccount.Roles);
        var slot = await SeedOpenSlotAsync(services, restaurantId, threshold: null);
        var eventRecord = await SeedEventAsync(services, host.UserId, restaurantId, capacity: 4);
        await services.EventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, host.UserId, EventParticipantState.Joined, null, services.Clock.UtcNow, services.Clock.UtcNow, null, null));
        await services.ReservationService.ReserveAsync(host, eventRecord.Id, new ReserveEventSlotRequest { SlotId = slot.Id });

        await services.SlotService.CancelAsync(manager, slot.Id, new CancelRestaurantSlotRequest { Reason = "Kitchen closed." });

        var cancelledEvent = await services.EventRepository.GetAsync(eventRecord.Id);
        var activeReservation = await services.RestaurantOperationsRepository.GetActiveReservationForEventAsync(eventRecord.Id);

        Assert.Equal(EventStatus.Cancelled, cancelledEvent!.Status);
        Assert.Contains("Restaurant slot cancelled", cancelledEvent.CancellationReason);
        Assert.Null(activeReservation);
    }

    private static async Task<CurrentUser> RegisterCurrentUserAsync(TestServices services, string username, string email, params UserRole[] extraRoles)
    {
        var session = await services.AuthService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

        var account = await services.AuthRepository.GetByIdAsync(session.CurrentUser.UserId)
            ?? throw new InvalidOperationException("Registered account was not persisted.");
        var roles = account.Roles
            .Concat(extraRoles)
            .Distinct()
            .OrderBy(role => role)
            .ToArray();

        if (extraRoles.Length > 0)
        {
            account = account with { Roles = roles };
            await services.AuthRepository.UpdateAccountAsync(account);
        }

        return new CurrentUser(account.Id, account.Username, account.Roles);
    }

    private static async Task<RestaurantSlot> SeedOpenSlotAsync(TestServices services, Guid restaurantId, int? threshold, int? discountPercent = null)
    {
        var now = services.Clock.UtcNow;
        var slot = new RestaurantSlot(
            Guid.NewGuid(),
            restaurantId,
            now.AddHours(1),
            now.AddHours(3),
            4,
            now.AddMinutes(30),
            threshold,
            discountPercent,
            RestaurantSlotStatus.Open,
            now,
            now,
            null,
            null);
        await services.RestaurantOperationsRepository.SaveSlotAsync(slot);
        return slot;
    }

    private static async Task<Event> SeedEventAsync(TestServices services, Guid hostUserId, Guid restaurantId, int capacity)
    {
        var now = services.Clock.UtcNow;
        var eventRecord = new Event(
            Guid.NewGuid(),
            hostUserId,
            "Slot event",
            EventType.Open,
            EventStatus.Open,
            now.AddHours(2),
            now.AddMinutes(45),
            capacity,
            2,
            restaurantId,
            null,
            null,
            null,
            now,
            now,
            null,
            null);
        await services.EventRepository.SaveAsync(eventRecord);
        return eventRecord;
    }

    private static TestServices CreateServices(bool discountsEnabled = false)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 1, 18, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var restaurantRepository = new InMemoryRestaurantRepository(store);
        var restaurantOperationsRepository = new InMemoryRestaurantOperationsRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var discountEligibilityService = new DiscountEligibilityService(restaurantOperationsRepository, eventRepository, new TestFeatureFlagService(discountsEnabled), clock);
        var managedRestaurantService = new ManagedRestaurantService(restaurantRepository, restaurantOperationsRepository);
        var slotService = new RestaurantSlotService(restaurantRepository, restaurantOperationsRepository, eventRepository, notificationService, managedRestaurantService, clock);
        var reservationService = new EventSlotReservationService(eventRepository, restaurantOperationsRepository, lifecycleService, discountEligibilityService, new InMemoryKeyedLockProvider(), clock);
        var assignmentService = new RestaurantAdminAssignmentService(restaurantRepository, restaurantOperationsRepository, authRepository, clock);

        return new TestServices(
            clock,
            authRepository,
            authService,
            eventRepository,
            restaurantOperationsRepository,
            assignmentService,
            slotService,
            reservationService,
            discountEligibilityService);
    }

    private sealed record TestServices(
        TestClock Clock,
        IAuthRepository AuthRepository,
        AuthService AuthService,
        IEventRepository EventRepository,
        IRestaurantOperationsRepository RestaurantOperationsRepository,
        RestaurantAdminAssignmentService AssignmentService,
        RestaurantSlotService SlotService,
        EventSlotReservationService ReservationService,
        DiscountEligibilityService DiscountEligibilityService);

    private sealed class TestFeatureFlagService(bool discountsEnabled) : IFeatureFlagService
    {
        public bool IsMessagingDirectChatEnabled() => false;

        public bool IsMessagingGroupChatEnabled() => true;

        public bool IsNotificationsPushEnabled() => false;

        public bool IsRestaurantsOperationsEnabled() => true;

        public bool IsRestaurantsSlotsEnabled() => true;

        public bool IsRestaurantsDiscountsEnabled() => discountsEnabled;

        public bool IsPaymentsCheckoutEnabled() => false;

        public bool IsDiscoveryExperimentalSuggestionsEnabled() => false;
    }
}
