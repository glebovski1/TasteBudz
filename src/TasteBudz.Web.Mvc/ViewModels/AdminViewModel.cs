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
    public int RestaurantCatalogTotalCount { get; init; }
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
    public const int PageSize = 25;

    public AdminRestaurantCatalogForm CreateForm { get; init; } = new();
    public IReadOnlyCollection<AdminRestaurantCatalogItemViewModel> Restaurants { get; init; } = [];
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<RestaurantAdminAssignmentDto>> AssignmentsByRestaurantId { get; init; } =
        new Dictionary<Guid, IReadOnlyCollection<RestaurantAdminAssignmentDto>>();
    public IReadOnlyCollection<string> SuggestedCuisineTags { get; init; } = [];
    public string? Q { get; init; }
    public AdminRestaurantCatalogStatus? FilterStatus { get; init; }
    public AdminRestaurantCatalogSource? FilterSource { get; init; }
    public int CurrentPage { get; init; } = 1;
    public int TotalCount { get; init; }
    public Guid? EditRestaurantId { get; init; }
    public RestaurantImportPreviewForm ImportForm { get; init; } = new();
    public RestaurantImportPreviewDto? ImportPreview { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public AdminRestaurantCatalogItemViewModel? EditItem => EditRestaurantId.HasValue
        ? Restaurants.FirstOrDefault(item => item.Restaurant.RestaurantId == EditRestaurantId.Value)
        : null;
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

public class RestaurantImportPreviewForm
{
    [MaxLength(40)]
    public string? Preset { get; set; } = "cincinnati";

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; set; } = "45202";

    [Range(1, 50)]
    public double? RadiusMiles { get; set; } = 25;

    public double? South { get; set; }
    public double? West { get; set; }
    public double? North { get; set; }
    public double? East { get; set; }

    public RestaurantImportPreviewQuery ToQuery() => new()
    {
        Preset = Preset,
        ZipCode = ZipCode,
        RadiusMiles = RadiusMiles,
        South = South,
        West = West,
        North = North,
        East = East,
    };
}

public sealed class RestaurantImportCommitForm : RestaurantImportPreviewForm
{
    public List<string> SelectedExternalPlaceIds { get; set; } = [];

    public CommitRestaurantImportRequest ToRequest() => new()
    {
        Preset = Preset,
        ZipCode = ZipCode,
        RadiusMiles = RadiusMiles,
        South = South,
        West = West,
        North = North,
        East = East,
        SelectedExternalPlaceIds = SelectedExternalPlaceIds,
    };
}
