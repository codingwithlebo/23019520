using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(RaceDayDbContext db, IPasswordService passwords, IConfiguration config) : ControllerBase
{
    /// <summary>Register a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Participants can register freely. Registering as an Organiser requires the organiser invite
    /// code, otherwise anyone could give themselves organiser powers.
    /// </remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed or the role is not Organiser/Participant.</response>
    /// <response code="403">The organiser invite code is missing or wrong.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!Roles.TryNormalize(request.Role, out var role))
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        if (role == Roles.Organiser)
        {
            var expected = config["Registration:OrganiserCode"];
            if (string.IsNullOrEmpty(expected) || request.OrganiserCode != expected)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "A valid organiser invite code is required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "An account with this email already exists." });

        var user = new AppUser
        {
            Email = email,
            PasswordHash = passwords.Hash(request.Password),
            Role = role,
            Profile = new UserProfile
            {
                FullName = request.FullName.Trim(),
                Phone = request.Phone
            }
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = new AuthResponse { Id = user.Id, Email = user.Email, Role = user.Role, FullName = user.Profile.FullName };
        return Created($"/api/users/{user.Id}/profile", response);
    }

    /// <summary>Log in and start a session.</summary>
    /// <remarks>On success a session cookie is set, which keeps the user's identity and role for later requests.</remarks>
    /// <response code="200">Logged in.</response>
    /// <response code="401">Email or password is wrong.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new AuthResponse
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            FullName = user.Profile?.FullName ?? string.Empty
        });
    }

    /// <summary>End the current session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
