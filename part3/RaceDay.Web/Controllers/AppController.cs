using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected SessionUser? CurrentUser => HttpContext.Session.GetUser();

    protected void Success(string message) => TempData["Success"] = message;
    protected void Fail(string message) => TempData["Error"] = message;

    /// <summary>
    /// If the API says the session has ended, clear ours and send the user to log in again.
    /// Returns null when the session is fine.
    /// </summary>
    protected IActionResult? SessionExpired<T>(ApiResult<T> result)
    {
        if (!result.IsUnauthorized) return null;

        HttpContext.Session.Clear();
        Fail("Your session has expired. Please log in again.");
        return RedirectToAction("Login", "Account");
    }

    protected IActionResult RedirectToHomeFor(SessionUser user) =>
        user.IsOrganiser ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");

    protected void AddApiErrors<T>(ApiResult<T> result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);
    }
}
