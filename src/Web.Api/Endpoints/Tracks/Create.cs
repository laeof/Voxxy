using Application.Abstractions.Messaging;
using Application.Tracks.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints.Tracks;

internal sealed class Create : IEndpoint
{
    public sealed record CreateTrackRequest(string Name, Guid ArtistId, Guid AlbumId, int AlbumOrder);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("tracks/create", async (
            [FromBody] CreateTrackRequest request,
            ICommandHandler<CreateTrackCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateTrackCommand(request.Name, request.ArtistId, request.AlbumId, request.AlbumOrder);

            Result<Guid> trackId = await handler.Handle(command, cancellationToken);

            return Results.Ok(trackId);
        })
        .WithTags(Tags.Tracks)
        .RequireAuthorization();
    }
}