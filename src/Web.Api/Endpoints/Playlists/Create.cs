using Application.Abstractions.Messaging;
using Application.Playlists.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Playlists;

internal sealed class Create : IEndpoint
{
    public sealed record CreatePlaylistRequest(string Name);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("playlists", async (
            [FromBody] CreatePlaylistRequest request,
            ICommandHandler<CreatePlaylistCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreatePlaylistCommand(request.Name);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Playlists)
        .RequireAuthorization();
    }
}