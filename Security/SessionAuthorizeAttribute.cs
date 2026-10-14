using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

/// <summary>
/// Protects an endpoint using the server-side session.
/// No session -> 401 Unauthorized. Logged in with the wrong role -> 403 Forbidden.
/// With no roles supplied, any logged-in user is allowed.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles) => this._roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId is null || role is null)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "You must log in to use this endpoint."
            })
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        if (this._roles.Length > 0 && !this._roles.Contains(role))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = $"This endpoint is only available to: {string.Join(", ", this._roles)}."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
