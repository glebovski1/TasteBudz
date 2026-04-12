using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Restaurants;

public sealed class ReserveEventSlotRequest
{
    [Required]
    public Guid? SlotId { get; init; }
}
