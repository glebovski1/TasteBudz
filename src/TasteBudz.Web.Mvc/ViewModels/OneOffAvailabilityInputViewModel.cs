using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class OneOffAvailabilityInputViewModel
{
    public Guid? WindowId { get; set; }

    [Required]
    [Display(Name = "Starts")]
    public DateTime? StartsAt { get; set; }

    [Required]
    [Display(Name = "Ends")]
    public DateTime? EndsAt { get; set; }

    [MaxLength(100)]
    public string? Label { get; set; }

    public UpsertOneOffAvailabilityWindowRequest ToRequest() =>
        new()
        {
            StartsAtUtc = StartsAt.HasValue ? new DateTimeOffset(StartsAt.Value, TimeSpan.Zero) : null,
            EndsAtUtc = EndsAt.HasValue ? new DateTimeOffset(EndsAt.Value, TimeSpan.Zero) : null,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label.Trim(),
        };
}
