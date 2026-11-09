using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize]
public class ProfileController(ApiClient api) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<ProfileDto>($"/api/users/{CurrentUser!.Id}/profile");
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            Fail(result.Message);
            return RedirectToHomeFor(CurrentUser!);
        }

        return View(ToModel(result.Data!));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        var user = CurrentUser!;

        if (!ModelState.IsValid)
        {
            var current = await api.GetAsync<ProfileDto>($"/api/users/{user.Id}/profile");
            if (SessionExpired(current) is { } expired) return expired;
            model.Profile = current.Data ?? new ProfileDto { UserId = user.Id };
            return View("Index", model);
        }

        var result = await api.PutAsync<ProfileDto>($"/api/users/{user.Id}/profile",
            new { fullName = model.FullName, phone = model.Phone, bio = model.Bio });
        if (SessionExpired(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            Fail(result.Message);
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetUser(user with { FullName = result.Data!.FullName });
        Success("Your profile was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            Fail("Choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await api.PostFileAsync<ProfileDto>($"/api/users/{CurrentUser!.Id}/profile/picture", picture);
        if (SessionExpired(result) is { } redirect) return redirect;

        if (result.Success) Success("Your profile picture was updated.");
        else Fail(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToModel(ProfileDto profile) => new()
    {
        Profile = profile,
        FullName = profile.FullName,
        Phone = profile.Phone,
        Bio = profile.Bio
    };
}
