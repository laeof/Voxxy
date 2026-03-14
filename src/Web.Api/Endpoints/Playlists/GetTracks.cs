using Application.Abstractions.Messaging;
using Application.Playlists.GetTracks;
using Application.Tracks.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Playlists;

internal sealed class GetTracks : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("playlists/{id:guid}/tracks", async (
            Guid id,
            IQueryHandler<GetPlaylistTracksQuery, List<TrackResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GetPlaylistTracksQuery(id);

            Result<List<TrackResponse>> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Playlists);
    }
}