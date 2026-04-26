using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

[Authorize]
public sealed class RestaurantController(
    RestaurantApiService restaurantApiService,
    UserSessionService userSessionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? cuisine,
        PriceTier? priceTier,
        string? zipCode,
        double? radiusMiles,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(page, 1);
        var query = new BrowseRestaurantsQuery
        {
            Q = Normalize(q),
            Cuisine = Normalize(cuisine),
            PriceTier = priceTier,
            ZipCode = Normalize(zipCode),
            RadiusMiles = radiusMiles,
            Page = currentPage,
            PageSize = RestaurantIndexViewModel.PageSize,
        };

        try
        {
            var response = await restaurantApiService.BrowseAsync(query, cancellationToken);

            return View(new RestaurantIndexViewModel
            {
                Restaurants = response.Items,
                TotalCount = response.TotalCount,
                CurrentPage = currentPage,
                SearchQuery = query.Q,
                Cuisine = query.Cuisine,
                PriceTier = priceTier,
                ZipCode = query.ZipCode,
                RadiusMiles = radiusMiles,
                SuggestedCuisineTags = CuisineData.AvailableCuisineTags,
            });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not load restaurants: {ex.Message}";

            return View(new RestaurantIndexViewModel
            {
                CurrentPage = currentPage,
                SearchQuery = query.Q,
                Cuisine = query.Cuisine,
                PriceTier = priceTier,
                ZipCode = query.ZipCode,
                RadiusMiles = radiusMiles,
                SuggestedCuisineTags = CuisineData.AvailableCuisineTags,
            });
        }
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
