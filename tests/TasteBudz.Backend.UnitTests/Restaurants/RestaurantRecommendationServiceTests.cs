// Unit tests for restaurant suggestion context rules.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.UnitTests.Restaurants;

/// <summary>
/// Verifies that suggestion queries honor the documented group context inputs.
/// </summary>
public sealed class RestaurantRecommendationServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsync_WithClosedEventForNonParticipant_ReturnsNotFound()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var now = DateTimeOffset.UtcNow;
        var hostUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        store.Profiles[hostUserId] = new UserProfile(hostUserId, "Host", "Host", "45220", SocialGoal.Friends, now, now);
        store.Events[eventId] = new Event(
            eventId,
            hostUserId,
            "Invite only",
            EventType.Closed,
            EventStatus.Open,
            now.AddDays(1),
            now.AddHours(12),
            4,
            2,
            null,
            "Sushi",
            null,
            null,
            now,
            now,
            null,
            null);

        var service = CreateService(store);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetSuggestionsAsync(
                new CurrentUser(Guid.NewGuid(), "outsider", Array.Empty<UserRole>()),
                new RestaurantSuggestionsQuery
                {
                    EventId = eventId,
                }));

        Assert.Equal(404, exception.StatusCode);
    }

    /// <summary>
    /// A bogus group id must fail fast so clients are not misled by generic fallback suggestions.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_WithUnknownGroupId_ReturnsNotFound()
    {
        var service = CreateService(new InMemoryTasteBudzStore());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetSuggestionsAsync(
                new CurrentUser(Guid.NewGuid(), "caller", Array.Empty<UserRole>()),
                new RestaurantSuggestionsQuery
                {
                    GroupId = Guid.NewGuid(),
                }));

        Assert.Equal(404, exception.StatusCode);
    }

    /// <summary>
    /// Inactive groups are rejected because event-planning suggestions should only use live group context.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_WithInactiveGroup_ReturnsConflict()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        // This group is intentionally dissolved to prove the lifecycle guard.
        var groupId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        store.Groups[groupId] = new Group(groupId, ownerUserId, "Inactive group", null, GroupVisibility.Public, GroupLifecycleState.Dissolved, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var service = CreateService(store);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetSuggestionsAsync(
                new CurrentUser(ownerUserId, "owner", Array.Empty<UserRole>()),
                new RestaurantSuggestionsQuery
                {
                    GroupId = groupId,
                }));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithGroupForNonMember_ReturnsNotFound()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var groupId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.Groups[groupId] = new Group(groupId, ownerUserId, "Public group", null, GroupVisibility.Public, GroupLifecycleState.Active, now, now);
        store.GroupMembers[$"{groupId:N}:{ownerUserId:N}"] = new GroupMember(groupId, ownerUserId, GroupMemberState.Active, now, now);
        store.Profiles[ownerUserId] = new UserProfile(ownerUserId, "Owner", "Host", "45220", SocialGoal.Friends, now, now);

        var service = CreateService(store);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetSuggestionsAsync(
                new CurrentUser(Guid.NewGuid(), "outsider", Array.Empty<UserRole>()),
                new RestaurantSuggestionsQuery
                {
                    GroupId = groupId,
                }));

        Assert.Equal(404, exception.StatusCode);
    }

    /// <summary>
    /// When the caller omits `zipCode`, group owner/member location should still make suggestions deterministic.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_WithActiveGroup_UsesOwnerZipCodeWhenQueryZipMissing()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        // The owner profile supplies the ZIP context that the request itself leaves blank.
        var ownerUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.Groups[groupId] = new Group(groupId, ownerUserId, "Sushi Budz", null, GroupVisibility.Public, GroupLifecycleState.Active, now, now);
        store.GroupMembers[$"{groupId:N}:{ownerUserId:N}"] = new GroupMember(groupId, ownerUserId, GroupMemberState.Active, now, now);
        store.Profiles[ownerUserId] = new UserProfile(ownerUserId, "Owner", "Host", "45220", SocialGoal.Friends, now, now);

        var service = CreateService(store);

        var suggestions = await service.GetSuggestionsAsync(
            new CurrentUser(ownerUserId, "owner", Array.Empty<UserRole>()),
            new RestaurantSuggestionsQuery
            {
                GroupId = groupId,
                CuisineTags = new[] { "Sushi" },
                RadiusMiles = 5,
            });
        var restaurant = Assert.Single(suggestions);

        Assert.Equal("Maki Social", restaurant.Name);
    }

    /// <summary>
    /// Explicit caller input wins over derived group context so the endpoint stays predictable.
    /// </summary>
    [Fact]
    public async Task GetSuggestionsAsync_WithExplicitZipCode_DoesNotOverrideItFromGroupContext()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        // The group owner lives in 45220, but the request intentionally asks for 41011 instead.
        var ownerUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.Groups[groupId] = new Group(groupId, ownerUserId, "Travel group", null, GroupVisibility.Public, GroupLifecycleState.Active, now, now);
        store.GroupMembers[$"{groupId:N}:{ownerUserId:N}"] = new GroupMember(groupId, ownerUserId, GroupMemberState.Active, now, now);
        store.Profiles[ownerUserId] = new UserProfile(ownerUserId, "Owner", "Host", "45220", SocialGoal.Friends, now, now);

        var service = CreateService(store);

        var suggestions = await service.GetSuggestionsAsync(
            new CurrentUser(ownerUserId, "owner", Array.Empty<UserRole>()),
            new RestaurantSuggestionsQuery
            {
                GroupId = groupId,
                ZipCode = "41011",
                CuisineTags = new[] { "American" },
                RadiusMiles = 5,
            });
        var restaurant = Assert.Single(suggestions);

        Assert.Equal("Riverfront Grill", restaurant.Name);
    }

    /// <summary>
    /// Builds the smallest deterministic dependency graph needed for recommendation-rule tests.
    /// </summary>
    private static RestaurantRecommendationService CreateService(InMemoryTasteBudzStore store)
    {
        var restaurantRepository = new InMemoryRestaurantRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var groupRepository = new InMemoryGroupRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var searchService = new RestaurantSearchService(restaurantRepository);
        return new RestaurantRecommendationService(restaurantRepository, eventRepository, groupRepository, profileRepository, searchService);
    }
}
