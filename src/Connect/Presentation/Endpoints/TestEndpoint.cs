using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Connect.Domain.Player;
using Connect.Shared;
using Microsoft.AspNetCore.SignalR;
using Connect.Presentation.Hubs;
using Connect.Application.Abstractions.Services;

namespace Presentation.Endpoints;

internal static class TestEndpoint
{
    public static IEndpointRouteBuilder MapSetPlayerStateEndpoint(this IEndpointRouteBuilder app)
    {
        var trackId = Guid.NewGuid();

        app.MapGet("player/play", async (
                IPlayerSessionService playerSessionService,
                IHubContext<PlayerHub, IPlayerClient> hubContext) =>
        {
            var userId = Guid.Parse("33358d2f-d8f2-4419-b34e-6a7be9e0b10b");

            var playRequest = new PlayRequest
            {
                TrackId = trackId
            };

            PlayerState state = await playerSessionService.PlayAsync(userId, playRequest);

            await hubContext.Clients
                .Group($"user:{userId}")
                .PlayerStateChanged(state);

            return Results.Ok(state);
        })
        .WithTags("test");

        app.MapGet("player/pause", async (
            IPlayerSessionService playerSessionService,
            IHubContext<PlayerHub, IPlayerClient> hubContext) =>
        {
            var userId = Guid.Parse("33358d2f-d8f2-4419-b34e-6a7be9e0b10b");

            PlayerState state = await playerSessionService.PauseAsync(userId);

            await hubContext.Clients
                .Group($"user:{userId}")
                .PlayerStateChanged(state);

            return Results.Ok(state);
        })
        .WithTags("test");

        return app;
    }
}