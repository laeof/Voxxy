using Microsoft.AspNetCore.Http;

namespace Web.Api.Factories;

internal sealed class CookieOptionsFactory
{
    private readonly IConfiguration _configuration;
    public CookieOptionsFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public CookieOptions AccessToken() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpirationInMinutes"))
    };

    public CookieOptions RefreshToken() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:ExpirationInDays"))
    };
}
