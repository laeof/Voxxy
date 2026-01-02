using System.Security.Claims;

namespace Infrastructure.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        string? userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userId, out Guid parsedUserId)
            ? parsedUserId
            : throw new ApplicationException("User id is unavailable");
    }

    public static string GetEmail(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirstValue(ClaimTypes.Email)
            ?? throw new ApplicationException("Email is unavailable");
    }

    public static string GetFullName(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirstValue(ClaimTypes.Name)
            ?? throw new ApplicationException("Full name is unavailable");
    }

    public static string GetImageUrl(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirstValue(ClaimTypes.Uri)
            ?? throw new ApplicationException("Image URL is unavailable");
    }

    public static string[] GetUserClaims(this ClaimsPrincipal? principal)
    {
        return principal?.FindAll("permission")
            .Select(c => c.Value)
            .ToArray()
            ?? Array.Empty<string>();
    }
}
