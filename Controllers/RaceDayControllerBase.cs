using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class RaceDayControllerBase : ControllerBase
{
    /// <summary>ID of the logged-in user, read from the server-side session.</summary>
    protected int CurrentUserId => this.HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    /// <summary>Role of the logged-in user ("Organiser" or "Participant"), read from the session.</summary>
    protected string CurrentRole => this.HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;

    protected ObjectResult Fail(int status, string title, string detail) =>
        this.Problem(detail: detail, title: title, statusCode: status);
}
