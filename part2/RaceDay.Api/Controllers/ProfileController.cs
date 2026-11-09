using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/{id:int}/profile")]
public class ProfileController(RaceDayDbContext db, IBlobStorageService blobs) : ControllerBase
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

        return Ok(ToResponse(user));
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

        return Ok(ToResponse(user));
    }

    /// <summary>Upload or replace your profile picture.</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called <c>file</c> (JPG, PNG, GIF or WebP, up to 5 MB).
    /// The API stores it in Azure Blob Storage.
    /// </remarks>
    /// <response code="200">The profile, now with a ProfilePictureUrl.</response>
    /// <response code="400">The file is missing, too large or not an image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You can only change your own picture.</response>
    /// <response code="503">Blob storage is not configured on the server.</response>
    [HttpPost("picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadPicture(int id, IFormFile file)
    {
        if (id != User.GetUserId()) return Forbid();

        var user = await db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id);
        if (user?.Profile is null) return NotFound();

        var error = ImageUpload.Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        var blobName = $"profiles/{id}/{Guid.NewGuid():N}{ImageUpload.Extension(file.ContentType)}";
        try
        {
            await using var stream = file.OpenReadStream();
            await blobs.UploadAsync(blobName, stream, file.ContentType);
        }
        catch (BlobStorageNotConfiguredException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured." });
        }

        var previous = user.Profile.ProfilePictureBlobName;
        user.Profile.ProfilePictureBlobName = blobName;
        await db.SaveChangesAsync();

        if (previous is not null)
        {
            try { await blobs.DeleteAsync(previous); }
            catch (Exception) { /* A leftover image is harmless. */ }
        }

        return Ok(ToResponse(user));
    }

    /// <summary>Get a profile picture.</summary>
    /// <response code="200">The image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No picture has been uploaded.</response>
    /// <response code="503">Blob storage is not configured on the server.</response>
    [HttpGet("picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPicture(int id)
    {
        var profile = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == id);
        if (profile?.ProfilePictureBlobName is null) return NotFound();

        try
        {
            var blob = await blobs.DownloadAsync(profile.ProfilePictureBlobName);
            return blob is null ? NotFound() : File(blob.Content, blob.ContentType);
        }
        catch (BlobStorageNotConfiguredException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured." });
        }
    }

    private static ProfileResponse ToResponse(AppUser user) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        Role = user.Role,
        FullName = user.Profile!.FullName,
        Phone = user.Profile.Phone,
        Bio = user.Profile.Bio,
        ProfilePictureUrl = user.Profile.ProfilePictureBlobName is null ? null : $"/api/users/{user.Id}/profile/picture"
    };
}
