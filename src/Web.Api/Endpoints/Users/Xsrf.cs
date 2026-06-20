using Microsoft.AspNetCore.Antiforgery;
using Web.Api.Factories;

namespace Web.Api.Endpoints.Users;

public sealed class Xsrf : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/xsrf-token", (
            HttpContext context,
            CookieOptionsFactory cookieOptionsFactory,
            IAntiforgery antiforgery) =>
        {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);

            context.Response.Cookies.Append(
                "VOXXY-XSRF-TOKEN",
                tokens.RequestToken!,
                cookieOptionsFactory.XsrfToken()
            );

            return Results.Ok();
        })
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}