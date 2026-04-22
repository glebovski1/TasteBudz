using TasteBudz.Backend.Domain;
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed record AdminIndexViewModel
{
    public IReadOnlyCollection<ModerationReportDto> PendingReports { get; init; } = [];
    public bool RestaurantOperationsAvailable { get; init; }
    public IReadOnlyCollection<RestaurantAssignmentPanelItem> RestaurantAssignments { get; init; } = [];
    public IReadOnlyCollection<PasswordResetRequestDto> OpenPasswordResetRequests { get; init; } = [];
    public PasswordResetTokenDto? GeneratedPasswordResetToken { get; init; }
}

public sealed class AdminReportsViewModel
{
    public IReadOnlyCollection<ModerationReportDto> Reports { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalCount { get; init; }
    public ModerationReportStatus? FilterStatus { get; init; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / 20.0);
}

public sealed class AdminReportDetailViewModel
{
    public ModerationReportDto Report { get; init; } = null!;
}

public sealed class RestaurantAssignmentPanelItem
{
    public RestaurantDto Restaurant { get; init; } = null!;
    public IReadOnlyCollection<RestaurantAdminAssignmentDto> Assignments { get; init; } = [];
}

public sealed class AdminSupportThreadsViewModel
{
    public IReadOnlyCollection<SupportThreadDto> Threads { get; init; } = [];
}

public sealed class AdminRestaurantsViewModel
{
    public AdminRestaurantCatalogForm CreateForm { get; init; } = new();
    public IReadOnlyCollection<AdminRestaurantCatalogItemViewModel> Restaurants { get; init; } = [];
    public IReadOnlyCollection<string> SuggestedCuisineTags { get; init; } = [];
}

public sealed class AdminRestaurantCatalogItemViewModel
{
    public AdminRestaurantCatalogItemDto Restaurant { get; init; } = null!;
    public AdminRestaurantCatalogForm Form { get; init; } = new();
}

public sealed class AdminRestaurantCatalogForm
{
    public Guid RestaurantId { get; init; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? StreetAddress { get; set; }

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

    [Required]
    public string CuisineTagsText { get; set; } = string.Empty;

    public SaveRestaurantCatalogRequest ToRequest() => new()
    {
        Name = Name,
        StreetAddress = StreetAddress,
        City = City,
        State = State,
        ZipCode = ZipCode,
        PriceTier = PriceTier,
        CuisineTags = CuisineTagsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray(),
    };

    public static AdminRestaurantCatalogForm FromDto(AdminRestaurantCatalogItemDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name,
        StreetAddress = dto.StreetAddress,
        City = dto.City,
        State = dto.State,
        ZipCode = dto.ZipCode,
        PriceTier = dto.PriceTier,
        CuisineTagsText = string.Join(", ", dto.CuisineTags),
    };
}
