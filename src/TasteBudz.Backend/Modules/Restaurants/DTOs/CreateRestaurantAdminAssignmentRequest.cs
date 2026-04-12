using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class CreateRestaurantAdminAssignmentRequest
{
    [Required]
    [MaxLength(80)]
    public string? Username { get; init; }
}
