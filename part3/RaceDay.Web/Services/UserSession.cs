using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Services;

public static class SessionKeys
{
    public const string User = "raceday.user";
    public const string ApiCookie = "raceday.apiCookie";
}

public record SessionUser(int Id, string Email, string FullName, string Role)
{
    public bool IsOrganiser => Role == "Organiser";
    public bool IsParticipant => Role == "Participant";
}

public static class UserSessionExtensions
{
    public static SessionUser? GetUser(this ISession session)
    {
        var json = session.GetString(SessionKeys.User);
        return json is null ? null : JsonSerializer.Deserialize<SessionUser>(json);
    }

    public static void SetUser(this ISession session, SessionUser user) =>
        session.SetString(SessionKeys.User, JsonSerializer.Serialize(user));
}

/// <summary>
/// Protects a page. With no roles, any logged-in user may enter; with roles, only those roles.
/// Anonymous visitors are sent to the login page, and the wrong role to an access-denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.GetUser();

        if (user is null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == "GET" ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (roles.Length > 0 && !roles.Contains(user.Role))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
