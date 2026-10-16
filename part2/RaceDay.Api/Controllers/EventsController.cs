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
public class EventsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>List all events, optionally filtered by type.</summary>
    /// <param name="type">Optional filter: Run, Walk or Cycle.</param>
    /// <response code="200">The list of events, soonest first.</response>
    /// <response code="400">The type filter is not Run, Walk or Cycle.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents([FromQuery] string? type)
    {
        var query = db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!EventTypes.TryNormalize(type, out var normalised))
                return BadRequest(new { message = "Type must be Run, Walk or Cycle." });
            query = query.Where(e => e.EventType == normalised);
        }

        var events = await query.OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(ToResponse).ToList());
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

        // Remove dependants explicitly so this also works with providers that do not cascade.
        foreach (var enrolment in ev.Enrolments.Where(en => en.Result is not null))
            db.Results.Remove(enrolment.Result!);
        db.Enrolments.RemoveRange(ev.Enrolments);
        db.Categories.RemoveRange(ev.Categories);
        db.Events.Remove(ev);
        await db.SaveChangesAsync();

        return NoContent();
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
        EventType = e.EventType
    };
}
