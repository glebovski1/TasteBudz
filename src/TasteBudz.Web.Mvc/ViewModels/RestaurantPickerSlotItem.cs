using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class RestaurantPickerSlotItem
{
    public Guid SlotId { get; init; }
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public int Capacity { get; init; }
    public int? MinThresholdForDiscount { get; init; }
    public int? DiscountPercent { get; init; }
    public string DisplayText { get; init; } = string.Empty;
    public string? DiscountText { get; init; }

    public static RestaurantPickerSlotItem FromDto(RestaurantSlotDto dto)
    {
        var starts = dto.StartsAtUtc.ToLocalTime().ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
        var ends = dto.EndsAtUtc.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture);
        var discountText = dto.MinThresholdForDiscount.HasValue && dto.DiscountPercent.HasValue
            ? $"{dto.DiscountPercent.Value}% at {dto.MinThresholdForDiscount.Value} guests"
            : null;

        return new()
        {
            SlotId = dto.SlotId,
            StartsAtUtc = dto.StartsAtUtc,
            EndsAtUtc = dto.EndsAtUtc,
            Capacity = dto.Capacity,
            MinThresholdForDiscount = dto.MinThresholdForDiscount,
            DiscountPercent = dto.DiscountPercent,
            DisplayText = $"{starts}-{ends} · up to {dto.Capacity}",
            DiscountText = discountText,
        };
    }
}
