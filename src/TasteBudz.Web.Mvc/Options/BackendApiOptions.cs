using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Web.Mvc.Options;

public sealed class BackendApiOptions
{
    public const string SectionName = "BackendApi";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}
