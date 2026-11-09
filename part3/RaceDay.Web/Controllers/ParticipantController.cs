using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize("Participant")]
public class ParticipantController(ApiClient api) : AppController
{
    [HttpGet]
    public async Task<IActionResult> MyEnrolments()
    {
        var result = await api.GetAsync<List<EnrolmentDto>>("/api/enrolments/me");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success) Fail(result.Message);
        return View(result.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await api.DeleteAsync($"/api/enrolments/{id}");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (result.Success) Success("Your enrolment was cancelled.");
        else Fail(result.Message);
        return RedirectToAction(nameof(MyEnrolments));
    }

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var result = await api.GetAsync<List<ResultDto>>("/api/results/me");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success) Fail(result.Message);
        return View(result.Data ?? new List<ResultDto>());
    }
}
