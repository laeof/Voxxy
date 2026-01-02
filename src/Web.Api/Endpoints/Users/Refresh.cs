using Application.Abstractions.Messaging;
using Application.Users.Refresh;
using Domain.Users;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Factories;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Refresh : IEndpoint
{
    public sealed record Request(User user);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/refresh", async (
            Request requestrefreshToken,
            ICommandHandler<RefreshTokenCommand, RefreshTokenResponse> handler,
            HttpContext httpContext,
            CookieOptionsFactory cookieOptionsFactory,
            CancellationToken cancellationToken) =>
        {
            string? refreshToken = httpContext.Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Results.StatusCode(403);
            }

            var command = new RefreshTokenCommand(requestrefreshToken.user, refreshToken);

            Result<RefreshTokenResponse> result = await handler.Handle(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.StatusCode(403);
            }

            return httpContext.OkWithAuthCookies(
                result.Value.Me,
                result.Value.AccessToken,
                result.Value.RefreshToken,
                cookieOptionsFactory.AccessToken(),
                cookieOptionsFactory.RefreshToken()
            );
        }
        )
        .WithTags(Tags.Users);
    }
}