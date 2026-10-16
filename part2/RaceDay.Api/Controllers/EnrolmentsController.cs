using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Enter an event by selecting one of its categories (Participant only).</summary>
    /// <remarks>The enrolment records the link between you, the event and the chosen category.</remarks>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">The category does not belong to the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can enrol.</response>
    /// <response code="404">The event or category does not exist.</response>
    /// <response code="409">You are already enrolled in this event.</response>
    [HttpPost("enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolmentRequest request)
    {
        var userId = User.GetUserId();

        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CategoryId);
        if (category is null) return NotFound(new { message = "Category not found." });
        if (category.EventId != ev.Id)
            return BadRequest(new { message = "That category does not belong to this event." });

        if (await db.Enrolments.AnyAsync(en => en.ParticipantId == userId && en.EventId == ev.Id))
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = userId,
            EventId = ev.Id,
            CategoryId = category.Id,
            EnrolmentDate = DateTime.UtcNow
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var participantName = await db.UserProfiles
            .Where(p => p.UserId == userId).Select(p => p.FullName).FirstOrDefaultAsync() ?? string.Empty;

        var response = new EnrolmentResponse
        {
            Id = enrolment.Id,
            ParticipantId = userId,
            ParticipantName = participantName,
            EventId = ev.Id,
            EventName = ev.Name,
            CategoryId = category.Id,
            CategoryName = category.Name,
            EnrolmentDate = enrolment.EnrolmentDate
        };
        return Created($"/api/enrolments/{enrolment.Id}", response);
    }

    /// <summary>View your own enrolments (Participant only).</summary>
    /// <response code="200">Your enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants have enrolments.</response>
    [HttpGet("enrolments/me")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = User.GetUserId();
        var enrolments = await db.Enrolments.AsNoTracking()
            .Include(en => en.Event).Include(en => en.Category)
            .Include(en => en.Participant).ThenInclude(p => p!.Profile)
            .Where(en => en.ParticipantId == userId)
            .OrderBy(en => en.Event!.Date)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse).ToList());
    }

    /// <summary>View every enrolment for one of your events (Organiser only).</summary>
    /// <response code="200">The enrolments for the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can see its enrolments.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var enrolments = await db.Enrolments.AsNoTracking()
            .Include(en => en.Event).Include(en => en.Category)
            .Include(en => en.Participant).ThenInclude(p => p!.Profile)
            .Where(en => en.EventId == eventId)
            .OrderBy(en => en.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse).ToList());
    }

    /// <summary>Cancel one of your own enrolments (Participant only).</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants, and only their own enrolments.</response>
    /// <response code="404">No enrolment with this id.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await db.Enrolments.Include(en => en.Result).FirstOrDefaultAsync(en => en.Id == id);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantId != User.GetUserId()) return Forbid();

        if (enrolment.Result is not null) db.Results.Remove(enrolment.Result);
        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static EnrolmentResponse ToResponse(Enrolment en) => new()
    {
        Id = en.Id,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant?.Profile?.FullName ?? string.Empty,
        EventId = en.EventId,
        EventName = en.Event?.Name ?? string.Empty,
        CategoryId = en.CategoryId,
        CategoryName = en.Category?.Name ?? string.Empty,
        EnrolmentDate = en.EnrolmentDate
    };
}
