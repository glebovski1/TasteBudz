using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class RestaurantAdminIndexViewModel
{
    public bool OperationsAvailable { get; init; } = true;
    public IReadOnlyCollection<RestaurantDto> Restaurants { get; init; } = [];
}

public sealed class RestaurantAdminManageViewModel
{
    public const int SlotPageSize = 10;

    public RestaurantDto Restaurant { get; init; } = null!;
    public IReadOnlyCollection<RestaurantSlotDto> Slots { get; init; } = [];
    public IReadOnlyCollection<RestaurantSlotDto> VisibleSlots { get; init; } = [];
    public RestaurantSlotDto? EditSlot { get; init; }
    public ManagedRestaurantForm RestaurantForm { get; init; } = new();
    public RestaurantSlotForm SlotForm { get; init; } = new();
    public RestaurantSlotEditForm? EditSlotForm { get; init; }
    public int SlotPage { get; init; } = 1;
    public int SlotTotalCount { get; init; }
    public int SlotTotalPages => Math.Max(1, (int)Math.Ceiling(SlotTotalCount / (double)SlotPageSize));
    public RestaurantSlotStatus? SlotStatus { get; init; }
}

public sealed class ManagedRestaurantForm
{
    public Guid RestaurantId { get; init; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    public PriceTier PriceTier { get; set; }
}

public sealed class RestaurantSlotForm
{
    public Guid RestaurantId { get; init; }

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

    public CreateRestaurantSlotRequest ToRequest() => new()
    {
        StartsAtUtc = new DateTimeOffset(StartsAt!.Value, TimeSpan.Zero),
        EndsAtUtc = new DateTimeOffset(EndsAt!.Value, TimeSpan.Zero),
        Capacity = Capacity!.Value,
        CutoffAtUtc = new DateTimeOffset(CutoffAt!.Value, TimeSpan.Zero),
        MinThresholdForDiscount = MinThresholdForDiscount,
        DiscountPercent = DiscountPercent,
    };
}

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
