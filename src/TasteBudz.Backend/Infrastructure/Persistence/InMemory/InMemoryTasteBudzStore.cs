// Shared in-memory backing store used by all repository implementations during the MVP phase.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Infrastructure.Persistence.InMemory;

/// <summary>
/// Holds the process-local state for repositories until a concrete SQL persistence path is chosen.
/// </summary>
public sealed class InMemoryTasteBudzStore
{
    /// <summary>
    /// Single lock used by repository implementations to keep compound reads and writes consistent.
    /// </summary>
    public object SyncRoot { get; } = new();

    public Dictionary<Guid, UserAccount> UserAccounts { get; } = new();

    public Dictionary<Guid, UserSession> Sessions { get; } = new();

    public Dictionary<Guid, UserProfile> Profiles { get; } = new();

    public Dictionary<Guid, UserPreferences> Preferences { get; } = new();

    public Dictionary<Guid, RecurringAvailabilityWindow> RecurringAvailability { get; } = new();

    public Dictionary<Guid, OneOffAvailabilityWindow> OneOffAvailability { get; } = new();

    public Dictionary<Guid, PrivacySettings> PrivacySettings { get; } = new();

    public Dictionary<string, UserBlock> Blocks { get; } = new(StringComparer.Ordinal);

    public Dictionary<Guid, Restaurant> Restaurants { get; } = new();

    public Dictionary<string, (double Latitude, double Longitude)> ZipCoordinates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<Guid, Event> Events { get; } = new();

    public Dictionary<string, EventParticipant> EventParticipants { get; } = new(StringComparer.Ordinal);

    public Dictionary<Guid, Group> Groups { get; } = new();

    public Dictionary<string, GroupMember> GroupMembers { get; } = new(StringComparer.Ordinal);

    public Dictionary<Guid, GroupInvite> GroupInvites { get; } = new();

    public Dictionary<string, SwipeDecision> SwipeDecisions { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, BudConnection> BudConnections { get; } = new(StringComparer.Ordinal);

    public Dictionary<Guid, Notification> Notifications { get; } = new();

    public Dictionary<string, ChatThread> ChatThreads { get; } = new(StringComparer.Ordinal);

    public Dictionary<Guid, ChatMessage> ChatMessages { get; } = new();

    public Dictionary<Guid, ModerationReport> ModerationReports { get; } = new();

    public Dictionary<Guid, ModerationAction> ModerationActions { get; } = new();

    public Dictionary<Guid, UserRestriction> UserRestrictions { get; } = new();

    public Dictionary<Guid, AuditLogEntry> AuditLogEntries { get; } = new();

    public InMemoryTasteBudzStore()
    {
        // Restaurants are seeded once so the catalog exists even in a fresh test host.
        SeedRestaurants();
    }

    public void Reset()
    {
        lock (SyncRoot)
        {
            // Tests reuse the same host and store instance, so reset must clear every mutable bucket.
            UserAccounts.Clear();
            Sessions.Clear();
            Profiles.Clear();
            Preferences.Clear();
            RecurringAvailability.Clear();
            OneOffAvailability.Clear();
            PrivacySettings.Clear();
            Blocks.Clear();
            Events.Clear();
            EventParticipants.Clear();
            Groups.Clear();
            GroupMembers.Clear();
            GroupInvites.Clear();
            SwipeDecisions.Clear();
            BudConnections.Clear();
            Notifications.Clear();
            ChatThreads.Clear();
            ChatMessages.Clear();
            ModerationReports.Clear();
            ModerationActions.Clear();
            UserRestrictions.Clear();
            AuditLogEntries.Clear();
            Restaurants.Clear();
            ZipCoordinates.Clear();

            SeedRestaurants();
        }
    }

    private void SeedRestaurants()
    {
        // Keep the seed data small and deterministic so integration tests have stable ids and distances.
        var restaurants = new[]
        {
            new Restaurant(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Maki Social", "Cincinnati", "OH", "45220", 39.1275, -84.5200, PriceTier.Two, new[] { "Sushi", "Japanese" }, null),
            new Restaurant(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Queen City Curry", "Cincinnati", "OH", "45202", 39.1034, -84.5120, PriceTier.Two, new[] { "Indian" }, null),
            new Restaurant(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Over-the-Rhine Tacos", "Cincinnati", "OH", "45202", 39.1111, -84.5161, PriceTier.One, new[] { "Mexican", "Tacos" }, null),
            new Restaurant(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Campus Noodles", "Cincinnati", "OH", "45219", 39.1290, -84.5145, PriceTier.One, new[] { "Thai", "Noodles" }, null),
            new Restaurant(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Riverfront Grill", "Covington", "KY", "41011", 39.0837, -84.5086, PriceTier.Three, new[] { "American" }, null),
            new Restaurant(Guid.Parse("66666666-6666-6666-6666-666666666666"), "Garden Falafel", "Cincinnati", "OH", "45206", 39.1402, -84.4819, PriceTier.One, new[] { "Mediterranean", "Vegetarian" }, null),
            new Restaurant(Guid.Parse("77777777-7777-7777-7777-777777777777"), "Little Saigon Table", "Cincinnati", "OH", "45208", 39.1362, -84.4312, PriceTier.Two, new[] { "Vietnamese" }, null),
            new Restaurant(Guid.Parse("88888888-8888-8888-8888-888888888888"), "Late Night Pizza Co", "Cincinnati", "OH", "45212", 39.1610, -84.4433, PriceTier.One, new[] { "Pizza", "Italian" }, null),
        };

        foreach (var restaurant in restaurants)
        {
            Restaurants[restaurant.Id] = restaurant;
        }

        // Approximate ZIP centroids are enough for browse and recommendation distance calculations.
        ZipCoordinates["45220"] = (39.1280, -84.5170);
        ZipCoordinates["45202"] = (39.1067, -84.5120);
        ZipCoordinates["45219"] = (39.1270, -84.5144);
        ZipCoordinates["41011"] = (39.0831, -84.5088);
        ZipCoordinates["45206"] = (39.1375, -84.4850);
        ZipCoordinates["45208"] = (39.1370, -84.4356);
        ZipCoordinates["45212"] = (39.1631, -84.4347);
    }
}
