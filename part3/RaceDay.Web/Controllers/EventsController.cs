using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController(ApiClient api) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await api.GetAsync<EventDto>($"/api/events/{id}");
        if (!ev.Success)
        {
            if (ev.StatusCode == 404) return NotFound();
            Fail(ev.Message);
            return RedirectToAction("Index", "Home");
        }

        var categories = await api.GetAsync<List<CategoryDto>>($"/api/events/{id}/categories");
        var model = new EventDetailsViewModel
        {
            Event = ev.Data!,
            Categories = categories.Data ?? new List<CategoryDto>()
        };

        // A logged-in Participant sees whether they are already enrolled.
        if (CurrentUser is { IsParticipant: true })
        {
            var mine = await api.GetAsync<List<EnrolmentDto>>("/api/enrolments/me");
            if (SessionExpired(mine) is { } redirect) return redirect;
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id);
        }

        return View(model);
    }

    [HttpPost]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            Fail("Choose a category to enter.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await api.PostAsync<EnrolmentDto>("/api/enrolments", new { eventId, categoryId });
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            Fail(result.Message);
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Success($"You're entered in {result.Data!.EventName} ({result.Data.CategoryName}). The organiser will confirm your place.");
        return RedirectToAction("MyEnrolments", "Participant");
    }
}
