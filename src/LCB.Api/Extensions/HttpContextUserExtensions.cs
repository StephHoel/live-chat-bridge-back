using System.Security.Claims;

namespace LCB.Api.Extensions;

public static class HttpContextUserExtensions
{
    public static bool TryGetAuthenticatedUserData(this HttpContext httpContext, out Guid userId, out string email)
    {
        userId = Guid.Empty;
        email = string.Empty;

        var rawUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rawEmail = httpContext.User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(rawUserId) || !Guid.TryParse(rawUserId, out userId))
            return false;

        if (string.IsNullOrWhiteSpace(rawEmail))
            return false;

        email = rawEmail;
        return true;
    }
}
