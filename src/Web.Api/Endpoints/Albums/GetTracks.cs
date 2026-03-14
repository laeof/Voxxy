using Application.Abstractions.Messaging;
using Application.Albums.GetTracks;
using Application.Playlists.GetTracks;
using Application.Tracks.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Albums;

internal sealed class GetTracks : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("albums/{id:guid}/tracks", async (
            Guid id,
            IQueryHandler<GetAlbumTracksQuery, List<TrackResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GetAlbumTracksQuery(id);

            Result<List<TrackResponse>> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Albums);
    }
}