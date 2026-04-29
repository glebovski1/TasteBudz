using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Web.Mvc.ViewModels;


internal static class GroupWallpaperOptions
{
    public static IReadOnlyList<GroupWallpaperOption> All { get; } =
    [
        new(GroupWallpaperTheme.Default, "TasteBudz Warm", "Soft neutral cards with a warm table glow."),
        new(GroupWallpaperTheme.PizzaNight, "Pizza Night", "Tomato, basil, and oven-baked energy."),
        new(GroupWallpaperTheme.SushiBar, "Sushi Bar", "Clean rice-paper texture with seaweed green."),
        new(GroupWallpaperTheme.TacoTable, "Taco Table", "Corn, lime, and salsa colors for casual meetups."),
        new(GroupWallpaperTheme.CoffeeBrunch, "Coffee Brunch", "Cafe tones for morning plans and pastries."),
        new(GroupWallpaperTheme.GardenFresh, "Garden Fresh", "Herb and market greens for lighter meals."),
    ];
}
