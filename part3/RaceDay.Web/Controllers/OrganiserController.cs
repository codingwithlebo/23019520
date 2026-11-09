using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize("Organiser")]
public class OrganiserController(ApiClient api) : AppController
{
    // ---------- Dashboard ----------

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var result = await api.GetAsync<List<EventDto>>("/api/events/mine");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success) Fail(result.Message);
        return View(new DashboardViewModel { Events = result.Data ?? new List<EventDto>() });
    }

    // ---------- Events ----------

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        var result = await api.GetAsync<List<EventDto>>("/api/events/mine");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success) Fail(result.Message);
        return View(result.Data ?? new List<EventDto>());
    }

    [HttpGet]
    public IActionResult Create() => View("EventForm", new EventFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);

        var result = await api.PostAsync<EventDto>("/api/events", ToRequest(model));
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            AddApiErrors(result);
            return View("EventForm", model);
        }

        await UploadBannerAsync(result.Data!.Id, model.Banner);
        Success($"\"{result.Data.Name}\" was created. Add categories so participants can enter.");
        return RedirectToAction(nameof(Categories), new { eventId = result.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await api.GetAsync<EventDto>($"/api/events/{id}");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            Fail(result.Message);
            return RedirectToAction(nameof(Events));
        }

        var ev = result.Data!;
        return View("EventForm", new EventFormViewModel
        {
            Id = ev.Id,
            Name = ev.Name,
            Description = ev.Description,
            Date = ev.Date,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            HasBanner = ev.BannerUrl is not null
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View("EventForm", model);

        var result = await api.PutAsync<EventDto>($"/api/events/{id}", ToRequest(model));
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            AddApiErrors(result);
            return View("EventForm", model);
        }

        await UploadBannerAsync(id, model.Banner);
        Success("The event was updated.");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await api.DeleteAsync($"/api/events/{id}");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (result.Success) Success("The event was deleted.");
        else Fail(result.Message);
        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var model = await LoadCategoriesAsync(eventId);
        if (model.Redirect is not null) return model.Redirect;
        return View(model.Page);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int eventId, CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var page = await LoadCategoriesAsync(eventId);
            if (page.Redirect is not null) return page.Redirect;
            page.Page!.Form = form;
            return View("Categories", page.Page);
        }

        var result = await api.PostAsync<CategoryDto>($"/api/events/{eventId}/categories",
            new { name = form.Name, description = form.Description });
        if (SessionExpired(result) is { } redirect) return redirect;

        if (result.Success) Success($"Category \"{form.Name}\" was added.");
        else Fail(result.Message);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var result = await api.DeleteAsync($"/api/categories/{id}");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (result.Success) Success("The category was removed.");
        else Fail(result.Message);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------

    [HttpGet]
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await api.GetAsync<EventDto>($"/api/events/{eventId}");
        if (SessionExpired(ev) is { } expired) return expired;
        if (!ev.Success)
        {
            Fail(ev.Message);
            return RedirectToAction(nameof(Events));
        }

        var enrolments = await api.GetAsync<List<EnrolmentDto>>($"/api/events/{eventId}/enrolments");
        if (SessionExpired(enrolments) is { } redirect) return redirect;
        if (!enrolments.Success)
        {
            Fail(enrolments.Message);
            return RedirectToAction(nameof(Events));
        }

        return View(new EnrolmentsViewModel { Event = ev.Data!, Enrolments = enrolments.Data! });
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int eventId, int id, string status)
    {
        var result = await api.PutAsync<EnrolmentDto>($"/api/enrolments/{id}/status", new { status });
        if (SessionExpired(result) is { } redirect) return redirect;

        if (result.Success) Success($"{result.Data!.ParticipantName} is now {result.Data.Status}.");
        else Fail(result.Message);
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------- Results ----------

    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await api.GetAsync<EventDto>($"/api/events/{eventId}");
        if (SessionExpired(ev) is { } expired) return expired;
        if (!ev.Success)
        {
            Fail(ev.Message);
            return RedirectToAction(nameof(Events));
        }

        var enrolments = await api.GetAsync<List<EnrolmentDto>>($"/api/events/{eventId}/enrolments");
        if (SessionExpired(enrolments) is { } redirect) return redirect;
        if (!enrolments.Success)
        {
            Fail(enrolments.Message);
            return RedirectToAction(nameof(Events));
        }

        var results = await api.GetAsync<List<ResultDto>>($"/api/events/{eventId}/results");
        var byEnrolment = (results.Data ?? new List<ResultDto>()).ToDictionary(r => r.EnrolmentId);

        var rows = enrolments.Data!
            .OrderBy(e => e.ParticipantName)
            .Select(e => new ResultRow
            {
                Enrolment = e,
                Result = byEnrolment.TryGetValue(e.Id, out var result) ? result : null
            })
            .ToList();

        return View(new ResultsViewModel { Event = ev.Data!, Rows = rows });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int eventId, int enrolmentId, int? resultId, string? finishTime, int? position)
    {
        if (string.IsNullOrWhiteSpace(finishTime) || position is null or < 1)
        {
            Fail("Enter a finish time and a finishing position (1 or higher).");
            return RedirectToAction(nameof(Results), new { eventId });
        }

        ApiResult<ResultDto> result = resultId is > 0
            ? await api.PutAsync<ResultDto>($"/api/results/{resultId}", new { finishTime, position })
            : await api.PostAsync<ResultDto>("/api/results", new { enrolmentId, finishTime, position });
        if (SessionExpired(result) is { } redirect) return redirect;

        if (result.Success) Success($"Result saved for {result.Data!.ParticipantName}. It is now visible to the participant.");
        else Fail(result.Message);
        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ---------- Helpers ----------

    private static object ToRequest(EventFormViewModel model) => new
    {
        name = model.Name,
        description = model.Description,
        date = model.Date,
        location = model.Location,
        distanceKm = model.DistanceKm,
        eventType = model.EventType
    };

    /// <summary>Uploads the banner through the API. A failed upload does not undo the saved event.</summary>
    private async Task UploadBannerAsync(int eventId, IFormFile? banner)
    {
        if (banner is null || banner.Length == 0) return;

        var upload = await api.PostFileAsync<EventDto>($"/api/events/{eventId}/banner", banner);
        if (!upload.Success) Fail($"The event was saved, but the banner couldn't be uploaded: {upload.Message}");
    }

    private async Task<(CategoriesViewModel? Page, IActionResult? Redirect)> LoadCategoriesAsync(int eventId)
    {
        var ev = await api.GetAsync<EventDto>($"/api/events/{eventId}");
        if (SessionExpired(ev) is { } expired) return (null, expired);
        if (!ev.Success)
        {
            Fail(ev.Message);
            return (null, RedirectToAction(nameof(Events)));
        }

        var categories = await api.GetAsync<List<CategoryDto>>($"/api/events/{eventId}/categories");
        return (new CategoriesViewModel
        {
            Event = ev.Data!,
            Categories = categories.Data ?? new List<CategoryDto>()
        }, null);
    }
}
