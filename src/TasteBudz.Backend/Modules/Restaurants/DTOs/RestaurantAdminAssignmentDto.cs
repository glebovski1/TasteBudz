namespace TasteBudz.Backend.Modules.Restaurants;

public sealed record RestaurantAdminAssignmentDto(
    Guid RestaurantId,
    Guid UserId,
    string Username,
    DateTimeOffset CreatedAtUtc);
