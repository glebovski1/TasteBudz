using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class OneOffAvailabilityItemViewModel
{
    public Guid Id { get; init; }

    public DateTimeOffset StartsAtUtc { get; init; }

    public DateTimeOffset EndsAtUtc { get; init; }

    public string? Label { get; init; }

    public DateTime StartsAtInput => StartsAtUtc.UtcDateTime;

    public DateTime EndsAtInput => EndsAtUtc.UtcDateTime;

    public static OneOffAvailabilityItemViewModel FromDto(OneOffAvailabilityWindowDto dto) =>
        new()
        {
            Id = dto.Id,
            StartsAtUtc = dto.StartsAtUtc,
            EndsAtUtc = dto.EndsAtUtc,
            Label = dto.Label,
        };
}
