using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController(ApiClient api) : AppController
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUser is { } user) return RedirectToHomeFor(user);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await api.PostAsync<AuthDto>("/api/auth/login", new { email = model.Email, password = model.Password });
        if (!result.Success)
        {
            AddApiErrors(result);
            return View(model);
        }

        var user = StartSession(result.Data!);
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToHomeFor(user);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentUser is { } user) return RedirectToHomeFor(user);
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var registration = await api.PostAsync<AuthDto>("/api/auth/register", new
        {
            email = model.Email,
            password = model.Password,
            fullName = model.FullName,
            phone = model.Phone,
            role = model.Role,
            organiserCode = model.Role == "Organiser" ? model.OrganiserCode : null
        });

        if (!registration.Success)
        {
            AddApiErrors(registration);
            return View(model);
        }

        // Log the new user straight in.
        var login = await api.PostAsync<AuthDto>("/api/auth/login", new { email = model.Email, password = model.Password });
        if (!login.Success)
        {
            Success("Your account was created. Please log in.");
            return RedirectToAction(nameof(Login));
        }

        var user = StartSession(login.Data!);
        Success($"Welcome to RaceDay, {user.FullName}!");
        return RedirectToHomeFor(user);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (CurrentUser is not null)
            await api.PostNoContentAsync("/api/auth/logout");

        HttpContext.Session.Clear();
        Success("You have been logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private SessionUser StartSession(AuthDto auth)
    {
        var user = new SessionUser(auth.Id, auth.Email, auth.FullName, auth.Role);
        HttpContext.Session.SetUser(user);
        return user;
    }
}
