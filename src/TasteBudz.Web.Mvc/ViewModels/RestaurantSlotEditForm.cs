using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantSlotEditForm
{
    public Guid RestaurantId { get; init; }
    public Guid SlotId { get; init; }

    [Required]
    public DateTime? StartsAt { get; set; }

    [Required]
    public DateTime? EndsAt { get; set; }

    [Required]
    [Range(2, 8)]
    public int? Capacity { get; set; }

    [Required]
    public DateTime? CutoffAt { get; set; }

    [Range(2, 8)]
    public int? MinThresholdForDiscount { get; set; }

    [Range(1, 100)]
    public int? DiscountPercent { get; set; }

    public UpdateRestaurantSlotRequest ToRequest() => new()
    {
        StartsAtUtc = new DateTimeOffset(StartsAt!.Value, TimeSpan.Zero),
        EndsAtUtc = new DateTimeOffset(EndsAt!.Value, TimeSpan.Zero),
        Capacity = Capacity!.Value,
        CutoffAtUtc = new DateTimeOffset(CutoffAt!.Value, TimeSpan.Zero),
        MinThresholdForDiscount = ShouldClearDiscount ? null : MinThresholdForDiscount,
        DiscountPercent = ShouldClearDiscount ? null : DiscountPercent,
        ClearDiscount = ShouldClearDiscount,
    };

    private bool ShouldClearDiscount => !MinThresholdForDiscount.HasValue && !DiscountPercent.HasValue;
}
