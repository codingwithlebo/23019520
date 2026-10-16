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
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Capture a participant's finish time and position after an event (Organiser only).</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can capture its results.</response>
    /// <response code="404">No enrolment with this id.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(ResultRequest request)
    {
        var enrolment = await db.Enrolments
            .Include(en => en.Event)
            .Include(en => en.Participant).ThenInclude(p => p!.Profile)
            .FirstOrDefaultAsync(en => en.Id == request.EnrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event!.OrganiserId != User.GetUserId()) return Forbid();

        if (await db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime.Trim(),
            Position = request.Position,
            Points = request.Points
        };
        db.Results.Add(result);
        await db.SaveChangesAsync();

        result.Enrolment = enrolment;
        return Created($"/api/results/{result.Id}", ToResponse(result));
    }

    /// <summary>Update a captured result (Organiser only).</summary>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can change its results.</response>
    /// <response code="404">No result with this id.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, ResultUpdateRequest request)
    {
        var result = await db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en!.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en!.Participant).ThenInclude(p => p!.Profile)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (result is null) return NotFound();
        if (result.Enrolment!.Event!.OrganiserId != User.GetUserId()) return Forbid();

        result.FinishTime = request.FinishTime.Trim();
        result.Position = request.Position;
        result.Points = request.Points;
        await db.SaveChangesAsync();

        return Ok(ToResponse(result));
    }

    /// <summary>View your own results (Participant only).</summary>
    /// <response code="200">Your results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants have personal results.</response>
    [HttpGet("results/me")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = User.GetUserId();
        var results = await db.Results.AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(en => en!.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en!.Participant).ThenInclude(p => p!.Profile)
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .ToListAsync();

        return Ok(results.Select(ToResponse).ToList());
    }

    /// <summary>View all results for an event, fastest position first (any logged-in user).</summary>
    /// <response code="200">The results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("events/{eventId:int}/results")]
    [Authorize]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();

        var results = await db.Results.AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(en => en!.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en!.Participant).ThenInclude(p => p!.Profile)
            .Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(ToResponse).ToList());
    }

    private static ResultResponse ToResponse(Result r) => new()
    {
        Id = r.Id,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment?.EventId ?? 0,
        EventName = r.Enrolment?.Event?.Name ?? string.Empty,
        ParticipantId = r.Enrolment?.ParticipantId ?? 0,
        ParticipantName = r.Enrolment?.Participant?.Profile?.FullName ?? string.Empty,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Points = r.Points
    };
}
