using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController(ApiClient api) : AppController
{
    /// <summary>Upcoming events, with filtering. Anyone can browse; enrolling needs a Participant login.</summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilter filter)
    {
        var result = await api.GetAsync<List<EventDto>>("/api/events" + filter.ToQueryString());

        return View(new HomeViewModel
        {
            Filter = filter,
            Events = result.Data ?? new List<EventDto>(),
            Error = result.Success ? null : result.Message
        });
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Error() => View("Error", new ErrorViewModel
    {
        Code = 500,
        Title = "Something went wrong",
        Message = "We hit a problem on our side. Please try again, and if it keeps happening, come back a little later."
    });

    [IgnoreAntiforgeryToken]
    public IActionResult StatusCodePage(int code)
    {
        var model = code switch
        {
            404 => new ErrorViewModel { Code = 404, Title = "Page not found", Message = "We couldn't find the page you were looking for." },
            403 => new ErrorViewModel { Code = 403, Title = "No access", Message = "You don't have permission to view that page." },
            400 => new ErrorViewModel { Code = 400, Title = "That didn't work", Message = "Something in that request wasn't right. Please go back and try again." },
            _ => new ErrorViewModel { Code = code, Title = "Something went wrong", Message = "Please try again in a moment." }
        };
        return View("Error", model);
    }
}
