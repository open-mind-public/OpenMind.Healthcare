using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QuitSmokingApi.Services;

namespace QuitSmokingApi.Tests.TestSupport;

/// <summary>
/// Builds the real <see cref="UserService"/> over a request carrying (or missing) the identity
/// claims a signed-in caller would have, so handlers see the same thing they see in production.
/// </summary>
public static class SignedInUser
{
    public static UserService WithId(Guid userId, string email = "someone@example.com")
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        ], authenticationType: "Test");

        return ServiceFor(new ClaimsPrincipal(identity));
    }

    public static UserService Anonymous() => ServiceFor(new ClaimsPrincipal(new ClaimsIdentity()));

    private static UserService ServiceFor(ClaimsPrincipal principal)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return new UserService(accessor);
    }
}
