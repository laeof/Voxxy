namespace Web.Api.Extensions;

internal static class AuthCookieExtensions
{
    public static IResult OkWithAuthCookies<T>(
        this HttpContext context,
        T body,
        string accessToken,
        string refreshToken,
        CookieOptions accessOptions,
        CookieOptions refreshOptions)
    {
        context.Response.Cookies.Append("access_token", accessToken, accessOptions);
        context.Response.Cookies.Append("refresh_token", refreshToken, refreshOptions);

        return Results.Ok(body);
    }
}