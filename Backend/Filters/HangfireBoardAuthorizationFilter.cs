using Backend.Interfaces;
using Hangfire.Dashboard;

namespace Backend.Filters;

/// <summary>
/// Restricts the Hangfire dashboard to board and candidate board members.
/// </summary>
public class HangfireBoardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <inheritdoc/>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        var userIdClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return false;

        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        return permissionService.IsBoardOrCandidateBoardMember(userId);
    }
}
