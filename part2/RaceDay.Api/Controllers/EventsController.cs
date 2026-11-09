using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(RaceDayDbContext db, IBlobStorageService blobs) : ControllerBase
{
    /// <summary>List events, with optional filters.</summary>
    /// <param name="type">Run, Walk or Cycle.</param>
    /// <param name="search">Text to look for in the event name or location.</param>
    /// <param name="minDistanceKm">Only events at least this long.</param>
    /// <param name="maxDistanceKm">Only events at most this long.</param>
    /// <param name="upcomingOnly">When true, hide events that have already taken place.</param>
    /// <response code="200">The matching events, soonest first.</response>
    /// <response code="400">The type filter is not Run, Walk or Cycle.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? type,
        [FromQuery] string? search,
        [FromQuery] decimal? minDistanceKm,
        [FromQuery] decimal? maxDistanceKm,
        [FromQuery] bool upcomingOnly = false)
    {
        var query = db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!EventTypes.TryNormalize(type, out var normalised))
                return BadRequest(new { message = "Type must be Run, Walk or Cycle." });
            query = query.Where(e => e.EventType == normalised);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Name.Contains(term) || e.Location.Contains(term));
        }

        if (minDistanceKm.HasValue) query = query.Where(e => e.DistanceKm >= minDistanceKm.Value);
        if (maxDistanceKm.HasValue) query = query.Where(e => e.DistanceKm <= maxDistanceKm.Value);
        if (upcomingOnly) query = query.Where(e => e.Date >= DateTime.UtcNow);

        var events = await query.OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(ToResponse).ToList());
    }

    /// <summary>List your own events with their enrolment counts (Organiser dashboard).</summary>
    /// <response code="200">Your events, soonest first, each with the number of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers have a dashboard.</response>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<OrganiserEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = User.GetUserId();
        var rows = await db.Events.AsNoTracking()
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.Date)
            .Select(e => new { Event = e, Count = e.Enrolments.Count })
            .ToListAsync();

        return Ok(rows.Select(r => ToSummary(r.Event, r.Count)).ToList());
    }

    /// <summary>Get one event.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return ev is null ? NotFound() : Ok(ToResponse(ev));
    }

    /// <summary>Create an event (Organiser only).</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed (for example the event type is not Run, Walk or Cycle).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create events.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        if (!EventTypes.TryNormalize(request.EventType, out var eventType))
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        var ev = new Event
        {
            OrganiserId = User.GetUserId(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Date = request.Date!.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = eventType
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id }, ToResponse(ev));
    }

    /// <summary>Update an event (the Organiser who owns it).</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who created the event can change it.</response>
    /// <response code="404">No event with this id.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        if (!EventTypes.TryNormalize(request.EventType, out var eventType))
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.Date = request.Date!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = eventType;
        await db.SaveChangesAsync();

        return Ok(ToResponse(ev));
    }

    /// <summary>Delete an event (the Organiser who owns it).</summary>
    /// <remarks>Its categories, enrolments and results are removed with it.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who created the event can delete it.</response>
    /// <response code="404">No event with this id.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await db.Events
            .Include(e => e.Enrolments).ThenInclude(en => en.Result)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var bannerBlob = ev.BannerBlobName;

        // Remove dependants explicitly so this also works with providers that do not cascade.
        foreach (var enrolment in ev.Enrolments.Where(en => en.Result is not null))
            db.Results.Remove(enrolment.Result!);
        db.Enrolments.RemoveRange(ev.Enrolments);
        db.Categories.RemoveRange(ev.Categories);
        db.Events.Remove(ev);
        await db.SaveChangesAsync();

        if (bannerBlob is not null) await TryDeleteBlobAsync(bannerBlob);
        return NoContent();
    }

    /// <summary>Upload or replace an event's banner image (the Organiser who owns it).</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called <c>file</c> (JPG, PNG, GIF or WebP, up to 5 MB).
    /// The API stores it in Azure Blob Storage.
    /// </remarks>
    /// <response code="200">The event, now with a BannerUrl.</response>
    /// <response code="400">The file is missing, too large or not an image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who created the event can upload its banner.</response>
    /// <response code="404">No event with this id.</response>
    /// <response code="503">Blob storage is not configured on the server.</response>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var error = ImageUpload.Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        var blobName = $"events/{id}/{Guid.NewGuid():N}{ImageUpload.Extension(file.ContentType)}";
        try
        {
            await using var stream = file.OpenReadStream();
            await blobs.UploadAsync(blobName, stream, file.ContentType);
        }
        catch (BlobStorageNotConfiguredException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured." });
        }

        var previous = ev.BannerBlobName;
        ev.BannerBlobName = blobName;
        await db.SaveChangesAsync();

        if (previous is not null) await TryDeleteBlobAsync(previous);
        return Ok(ToResponse(ev));
    }

    /// <summary>Get an event's banner image.</summary>
    /// <response code="200">The image.</response>
    /// <response code="404">The event has no banner.</response>
    /// <response code="503">Blob storage is not configured on the server.</response>
    [HttpGet("{id:int}/banner")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetBanner(int id)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (ev?.BannerBlobName is null) return NotFound();

        try
        {
            var blob = await blobs.DownloadAsync(ev.BannerBlobName);
            return blob is null ? NotFound() : File(blob.Content, blob.ContentType);
        }
        catch (BlobStorageNotConfiguredException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured." });
        }
    }

    private async Task TryDeleteBlobAsync(string blobName)
    {
        try { await blobs.DeleteAsync(blobName); }
        catch (Exception) { /* A leftover image is harmless; do not fail the request over it. */ }
    }

    private static EventResponse ToResponse(Event e) => new()
    {
        Id = e.Id,
        OrganiserId = e.OrganiserId,
        Name = e.Name,
        Description = e.Description,
        Date = e.Date,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        BannerUrl = e.BannerBlobName is null ? null : $"/api/events/{e.Id}/banner"
    };

    private static OrganiserEventResponse ToSummary(Event e, int enrolmentCount) => new()
    {
        Id = e.Id,
        OrganiserId = e.OrganiserId,
        Name = e.Name,
        Description = e.Description,
        Date = e.Date,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        BannerUrl = e.BannerBlobName is null ? null : $"/api/events/{e.Id}/banner",
        EnrolmentCount = enrolmentCount
    };
}
