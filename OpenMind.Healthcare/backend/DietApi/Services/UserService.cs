using System.Security.Claims;

namespace DietApi.Services;

public interface IUserService
{
    Guid? GetCurrentUserId();
    string? GetCurrentUserEmail();
}

/// <summary>
/// The only sanctioned source of the acting member's identity. No endpoint accepts a user id
/// from a route, query string, or body - it is always read from the authenticated token here.
/// </summary>
public class UserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    public Guid? GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public string? GetCurrentUserEmail()
    {
        return httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Email)?.Value;
    }
}
