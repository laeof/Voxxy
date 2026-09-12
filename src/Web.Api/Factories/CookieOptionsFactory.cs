using Microsoft.AspNetCore.Http;

namespace Web.Api.Factories;

internal sealed class CookieOptionsFactory
{
    private readonly IConfiguration _configuration;
    public CookieOptionsFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public CookieOptions XsrfToken() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:ExpirationInDays"))
    };
}
