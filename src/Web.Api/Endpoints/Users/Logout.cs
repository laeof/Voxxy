using Web.Api.Factories;

namespace Web.Api.Endpoints.Users;

internal sealed class Logout : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/logout", (HttpContext httpContext, CookieOptionsFactory cookieOptionsFactory) =>
        {
            httpContext.Response.Cookies.Delete("access_token", cookieOptionsFactory.AccessToken());
            httpContext.Response.Cookies.Delete("refresh_token", cookieOptionsFactory.RefreshToken());

            return Results.Ok();
        })
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}