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
    public sealed record Request(string RefreshToken);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/refresh", async (
            Request requestRefreshToken,
            ICommandHandler<RefreshTokenCommand, RefreshTokenResponse> handler,
            CookieOptionsFactory cookieOptionsFactory,
            CancellationToken cancellationToken) =>
        {
            string? refreshToken = requestRefreshToken.RefreshToken;

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Results.StatusCode(403);
            }

            var command = new RefreshTokenCommand(refreshToken);

            Result<RefreshTokenResponse> result = await handler.Handle(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.StatusCode(403);
            }

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Users);
    }
}