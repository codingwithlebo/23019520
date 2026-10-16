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
public class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>List the categories of an event (age or distance categories such as Under 20, Senior, 10km, 21km).</summary>
    /// <response code="200">The categories.</response>
    /// <response code="404">No event with this id.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();

        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(categories.Select(ToResponse).ToList());
    }

    /// <summary>Add a category to an event (the Organiser who owns the event).</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers, and only for their own events.</response>
    /// <response code="404">No event with this id.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            Description = request.Description
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", ToResponse(category));
    }

    /// <summary>Update a category (the Organiser who owns its event).</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers, and only for their own events.</response>
    /// <response code="404">No category with this id.</response>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != User.GetUserId()) return Forbid();

        category.Name = request.Name.Trim();
        category.Description = request.Description;
        await db.SaveChangesAsync();

        return Ok(ToResponse(category));
    }

    /// <summary>Delete a category (the Organiser who owns its event).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers, and only for their own events.</response>
    /// <response code="404">No category with this id.</response>
    /// <response code="409">Participants are already enrolled in this category.</response>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != User.GetUserId()) return Forbid();

        if (await db.Enrolments.AnyAsync(en => en.CategoryId == id))
            return Conflict(new { message = "Participants are enrolled in this category, so it cannot be deleted." });

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        Id = c.Id,
        EventId = c.EventId,
        Name = c.Name,
        Description = c.Description
    };
}
