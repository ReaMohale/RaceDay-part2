using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => this._db = db;

    private static readonly Expression<Func<Event, EventResponse>> ToResponse = e => new EventResponse(
        e.EventId, e.OrganiserId, e.Organiser.FullName, e.EventName, e.Description,
        e.EventDate, e.Location, e.EventType, e.DistanceKm);

    /// <summary>Lists all events (public - no login needed).</summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetAll()
    {
        var events = await this._db.Events.AsNoTracking()
            .OrderBy(e => e.EventDate)
            .Select(ToResponse)
            .ToListAsync();
        return this.Ok(events);
    }

    /// <summary>Returns one event's details (public - no login needed).</summary>
    [HttpGet("events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await this._db.Events.AsNoTracking().Where(e => e.EventId == id).Select(ToResponse).FirstOrDefaultAsync();
        if (ev is null)
            return this.Fail(StatusCodes.Status404NotFound, "Event not found", $"No event with id {id}.");
        return this.Ok(ev);
    }

    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    /// <remarks>eventType must be Run, Walk or Cycle. eventDate format: yyyy-MM-dd.</remarks>
    [HttpPost("events")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> Create([FromBody] EventRequest request)
    {
        var ev = new Event
        {
            OrganiserId = this.CurrentUserId,
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            EventType = request.EventType,
            DistanceKm = request.DistanceKm
        };
        this._db.Events.Add(ev);
        await this._db.SaveChangesAsync();

        var response = await this._db.Events.AsNoTracking().Where(e => e.EventId == ev.EventId).Select(ToResponse).FirstAsync();
        return this.CreatedAtAction(nameof(GetById), new { id = ev.EventId }, response);
    }

    /// <summary>Updates an event the logged-in Organiser owns.</summary>
    [HttpPut("events/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(int id, [FromBody] EventRequest request)
    {
        var ev = await this._db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return this.Fail(StatusCodes.Status404NotFound, "Event not found", $"No event with id {id}.");
        if (ev.OrganiserId != this.CurrentUserId)
            return this.Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only change events you created.");

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.EventType = request.EventType;
        ev.DistanceKm = request.DistanceKm;
        await this._db.SaveChangesAsync();

        var response = await this._db.Events.AsNoTracking().Where(e => e.EventId == id).Select(ToResponse).FirstAsync();
        return this.Ok(response);
    }

    /// <summary>Deletes an event (and its categories) the logged-in Organiser owns.</summary>
    /// <remarks>An event that already has enrolments cannot be deleted (409).</remarks>
    [HttpDelete("events/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await this._db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return this.Fail(StatusCodes.Status404NotFound, "Event not found", $"No event with id {id}.");
        if (ev.OrganiserId != this.CurrentUserId)
            return this.Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only delete events you created.");

        if (await this._db.Enrolments.AnyAsync(e => e.EventId == id))
            return this.Fail(StatusCodes.Status409Conflict, "Event has enrolments", "This event already has enrolments and cannot be deleted.");

        this._db.Categories.RemoveRange(ev.Categories);
        this._db.Events.Remove(ev);
        await this._db.SaveChangesAsync();
        return this.NoContent();
    }
}
