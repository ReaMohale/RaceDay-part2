using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;

    public AuthController(RaceDayDbContext db, IPasswordService passwords)
    {
        this._db = db;
        this._passwords = passwords;
    }

    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <remarks>
    /// Send accountType as "Organiser" or "Participant". Emails are unique across both account types.
    /// The password is hashed before it is stored.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileResponse>> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await this._db.Organisers.AnyAsync(o => o.Email == email)
                         || await this._db.Participants.AnyAsync(p => p.Email == email);
        if (emailTaken)
            return this.Fail(StatusCodes.Status409Conflict, "Email already registered", "An account with this email address already exists.");

        var hash = this._passwords.Hash(request.Password);
        UserProfileResponse profile;

        try
        {
            if (request.AccountType == Roles.Organiser)
            {
                var organiser = new Organiser
                {
                    FullName = request.FullName.Trim(),
                    Email = email,
                    PasswordHash = hash,
                    PhoneNumber = request.PhoneNumber
                };
                this._db.Organisers.Add(organiser);
                await this._db.SaveChangesAsync();
                profile = new UserProfileResponse(organiser.OrganiserId, organiser.FullName, organiser.Email,
                    organiser.PhoneNumber, Roles.Organiser, organiser.CreatedAt);
            }
            else
            {
                var participant = new Participant
                {
                    FullName = request.FullName.Trim(),
                    Email = email,
                    PasswordHash = hash,
                    PhoneNumber = request.PhoneNumber
                };
                this._db.Participants.Add(participant);
                await this._db.SaveChangesAsync();
                profile = new UserProfileResponse(participant.ParticipantId, participant.FullName, participant.Email,
                    participant.PhoneNumber, Roles.Participant, participant.CreatedAt);
            }
        }
        catch (Exception)
        {
            // Two registrations with the same email at the same moment hit the unique index.
            // If the email is now taken that is a conflict; any other failure goes to the global handler (500).
            var nowTaken = await this._db.Organisers.AnyAsync(o => o.Email == email)
                           || await this._db.Participants.AnyAsync(p => p.Email == email);
            if (nowTaken)
                return this.Fail(StatusCodes.Status409Conflict, "Email already registered", "An account with this email address already exists.");
            throw;
        }

        return this.Created("/api/users/me", profile);
    }

    /// <summary>Logs in and starts a server-side session holding the user's ID and role.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        UserProfileResponse? profile = null;

        var organiser = await this._db.Organisers.AsNoTracking().FirstOrDefaultAsync(o => o.Email == email);
        if (organiser is not null && this._passwords.Verify(request.Password, organiser.PasswordHash))
        {
            profile = new UserProfileResponse(organiser.OrganiserId, organiser.FullName, organiser.Email,
                organiser.PhoneNumber, Roles.Organiser, organiser.CreatedAt);
        }
        else
        {
            var participant = await this._db.Participants.AsNoTracking().FirstOrDefaultAsync(p => p.Email == email);
            if (participant is not null && this._passwords.Verify(request.Password, participant.PasswordHash))
            {
                profile = new UserProfileResponse(participant.ParticipantId, participant.FullName, participant.Email,
                    participant.PhoneNumber, Roles.Participant, participant.CreatedAt);
            }
        }

        if (profile is null)
            return this.Fail(StatusCodes.Status401Unauthorized, "Invalid credentials", "The email or password is incorrect.");

        this.HttpContext.Session.Clear();
        this.HttpContext.Session.SetInt32(SessionKeys.UserId, profile.UserId);
        this.HttpContext.Session.SetString(SessionKeys.Role, profile.Role);

        return this.Ok(profile);
    }

    /// <summary>Ends the current session.</summary>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        this.HttpContext.Session.Clear();
        return this.NoContent();
    }
}
