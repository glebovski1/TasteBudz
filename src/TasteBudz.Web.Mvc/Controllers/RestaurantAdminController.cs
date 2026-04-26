using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

[Authorize(Roles = "RestaurantAdmin")]
public sealed class RestaurantAdminController(
    RestaurantApiService restaurantApiService,
    UserSessionService userSessionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var restaurants = await restaurantApiService.ListManagedRestaurantsAsync(cancellationToken);
            return View(new RestaurantAdminIndexViewModel { Restaurants = restaurants });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return View(new RestaurantAdminIndexViewModel { OperationsAvailable = false });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Manage(
        Guid restaurantId,
        int slotPage = 1,
        RestaurantSlotStatus? slotStatus = null,
        Guid? editSlotId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var restaurants = await restaurantApiService.ListManagedRestaurantsAsync(cancellationToken);
            var restaurant = restaurants.FirstOrDefault(item => item.RestaurantId == restaurantId);

            if (restaurant is null)
            {
                TempData["StatusMessage"] = "That restaurant is not assigned to you.";
                return RedirectToAction(nameof(Index));
            }

            var slots = await restaurantApiService.ListManagedSlotsAsync(restaurantId, cancellationToken);
            return View(BuildManageViewModel(restaurant, slots, slotPage, slotStatus, editSlotId));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = ex.StatusCode == HttpStatusCode.NotFound
                ? "Restaurant operations are not enabled."
                : ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRestaurant([Bind(Prefix = "RestaurantForm")] ManagedRestaurantForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Restaurant profile changes are invalid.";
            return RedirectToAction(nameof(Manage), new { restaurantId = form.RestaurantId });
        }

        try
        {
            await restaurantApiService.UpdateManagedRestaurantAsync(
                form.RestaurantId,
                new UpdateManagedRestaurantRequest
                {
                    Name = form.Name,
                    City = form.City,
                    State = form.State,
                    ZipCode = form.ZipCode,
                    PriceTier = form.PriceTier,
                },
                cancellationToken);
            TempData["StatusMessage"] = "Restaurant profile updated.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Restaurant update failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { restaurantId = form.RestaurantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSlot([Bind(Prefix = "SlotForm")] RestaurantSlotForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Slot details are invalid.";
            return RedirectToAction(nameof(Manage), new { restaurantId = form.RestaurantId });
        }

        try
        {
            await restaurantApiService.CreateManagedSlotAsync(form.RestaurantId, form.ToRequest(), cancellationToken);
            TempData["StatusMessage"] = "Restaurant slot created.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Slot creation failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { restaurantId = form.RestaurantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSlot(RestaurantSlotEditForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Slot changes are invalid.";
            return RedirectToAction(nameof(Manage), new { restaurantId = form.RestaurantId });
        }

        try
        {
            await restaurantApiService.UpdateManagedSlotAsync(form.SlotId, form.ToRequest(), cancellationToken);
            TempData["StatusMessage"] = "Restaurant slot updated.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Slot update failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { restaurantId = form.RestaurantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSlot(Guid restaurantId, Guid slotId, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await restaurantApiService.CancelManagedSlotAsync(
                slotId,
                new CancelRestaurantSlotRequest { Reason = string.IsNullOrWhiteSpace(reason) ? "Restaurant slot cancelled." : reason.Trim() },
                cancellationToken);
            TempData["StatusMessage"] = "Restaurant slot cancelled.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Slot cancellation failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { restaurantId });
    }

    private static RestaurantAdminManageViewModel BuildManageViewModel(
        RestaurantDto restaurant,
        IReadOnlyCollection<RestaurantSlotDto> slots,
        int slotPage,
        RestaurantSlotStatus? slotStatus,
        Guid? editSlotId)
    {
        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddDays(30);
        var filteredSlots = slots
            .Where(slot => slot.StartsAtUtc >= now && slot.StartsAtUtc <= windowEnd)
            .Where(slot => !slotStatus.HasValue || slot.Status == slotStatus.Value)
            .OrderBy(slot => slot.StartsAtUtc)
            .ThenBy(slot => slot.SlotId)
            .ToArray();

        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredSlots.Length / (double)RestaurantAdminManageViewModel.SlotPageSize));
        var currentPage = Math.Clamp(slotPage, 1, totalPages);
        var visibleSlots = filteredSlots
            .Skip((currentPage - 1) * RestaurantAdminManageViewModel.SlotPageSize)
            .Take(RestaurantAdminManageViewModel.SlotPageSize)
            .ToArray();
        var editSlot = editSlotId.HasValue
            ? visibleSlots.FirstOrDefault(slot => slot.SlotId == editSlotId.Value)
            : null;

        return new()
        {
            Restaurant = restaurant,
            Slots = filteredSlots,
            VisibleSlots = visibleSlots,
            EditSlot = editSlot,
            EditSlotForm = editSlot is null ? null : BuildEditSlotForm(restaurant.RestaurantId, editSlot),
            SlotPage = currentPage,
            SlotTotalCount = filteredSlots.Length,
            SlotStatus = slotStatus,
            RestaurantForm = new ManagedRestaurantForm
            {
                RestaurantId = restaurant.RestaurantId,
                Name = restaurant.Name,
                City = restaurant.City,
                State = restaurant.State,
                ZipCode = restaurant.ZipCode,
                PriceTier = restaurant.PriceTier,
            },
            SlotForm = BuildCreateSlotDefaults(restaurant.RestaurantId),
        };
    }

    private static RestaurantSlotForm BuildCreateSlotDefaults(Guid restaurantId)
    {
        var startsAt = DateTime.Now.Date.AddDays(1).AddHours(18);

        return new()
        {
            RestaurantId = restaurantId,
            StartsAt = startsAt,
            EndsAt = startsAt.AddHours(2),
            CutoffAt = startsAt.AddHours(-1),
            Capacity = 4,
        };
    }

    private static RestaurantSlotEditForm BuildEditSlotForm(Guid restaurantId, RestaurantSlotDto slot) =>
        new()
        {
            RestaurantId = restaurantId,
            SlotId = slot.SlotId,
            StartsAt = slot.StartsAtUtc.ToLocalTime().DateTime,
            EndsAt = slot.EndsAtUtc.ToLocalTime().DateTime,
            Capacity = slot.Capacity,
            CutoffAt = slot.CutoffAtUtc.ToLocalTime().DateTime,
            MinThresholdForDiscount = slot.MinThresholdForDiscount,
            DiscountPercent = slot.DiscountPercent,
        };

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
