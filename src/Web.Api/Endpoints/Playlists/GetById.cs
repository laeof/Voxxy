using Application.Abstractions.Messaging;
using Application.Playlists.GetById;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Playlists;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("playlists/{id:guid}", async (
            Guid id,
            IQueryHandler<GetPlaylistByIdQuery, PlaylistResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GetPlaylistByIdQuery(id);

            Result<PlaylistResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Playlists)
        .RequireAuthorization();
    }
}