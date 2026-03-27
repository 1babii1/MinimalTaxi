using System.Security.Claims;
using MinimalTaxiService.Domain.Enums;

namespace MinimalTaxiService.Web.Extensions;

public static class UserClaimsExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue("userId")
                  ?? user.FindFirstValue("uid");

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    public static UserRole? GetUserRole(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.Role)
                  ?? user.FindFirstValue("role")
                  ?? user.FindFirstValue("roles");

        return Enum.TryParse<UserRole>(raw, true, out var role) ? role : null;
    }
}
