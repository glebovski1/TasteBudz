namespace TasteBudz.Web.Mvc.ViewModels;

/// <summary>
/// Shared reference lists used by both the profile editor and event creation form.
/// Must stay in sync with the cuisine names produced by OverpassRestaurantImporter.
/// </summary>
public static class CuisineData
{
    public static IReadOnlyList<string> AvailableCuisineTags { get; } = new[]
    {
        "African",
        "American",
        "Asian",
        "Brazilian",
        "Caribbean",
        "Chinese",
        "French",
        "German",
        "Greek",
        "Indian",
        "Italian",
        "Japanese",
        "Korean",
        "Latin American",
        "Mediterranean",
        "Mexican",
        "Noodles",
        "Pizza",
        "Seafood",
        "Spanish",
        "Sushi",
        "Tacos",
        "Tex-Mex",
        "Thai",
        "Vegetarian",
        "Vietnamese",
    };
}