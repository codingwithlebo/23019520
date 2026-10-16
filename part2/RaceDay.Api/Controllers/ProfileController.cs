using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/{id:int}/profile")]
public class ProfileController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>View your own profile.</summary>
    /// <response code="200">The profile.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You can only view your own profile.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProfile(int id)
    {
        if (id != User.GetUserId()) return Forbid();

        var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id);
        if (user?.Profile is null) return NotFound();

        return Ok(ToResponse(user.Id, user.Email, user.Role, user.Profile.FullName, user.Profile.Phone, user.Profile.Bio));
    }

    /// <summary>Update your own profile.</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You can only update your own profile.</response>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProfile(int id, UpdateProfileRequest request)
    {
        if (id != User.GetUserId()) return Forbid();

        var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id);
        if (user?.Profile is null) return NotFound();

        user.Profile.FullName = request.FullName.Trim();
        user.Profile.Phone = request.Phone;
        user.Profile.Bio = request.Bio;
        await db.SaveChangesAsync();

        return Ok(ToResponse(user.Id, user.Email, user.Role, user.Profile.FullName, user.Profile.Phone, user.Profile.Bio));
    }

    private static ProfileResponse ToResponse(int id, string email, string role, string fullName, string? phone, string? bio) =>
        new() { UserId = id, Email = email, Role = role, FullName = fullName, Phone = phone, Bio = bio };
}
