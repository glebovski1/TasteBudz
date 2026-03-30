using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Public-facing landing page. Authenticated users are redirected to their dashboard.
/// </summary>
[AllowAnonymous]
public sealed class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        // Send logged-in users straight to their dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(ProfileController.View), "Profile");
        }

        return View();
    }
}