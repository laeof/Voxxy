using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Playlists.GetById;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Albums;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("albums/{id:guid}", async (
            Guid id,
            IQueryHandler<GetAlbumByIdQuery, AlbumResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GetAlbumByIdQuery(id);

            Result<AlbumResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Albums)
        .RequireAuthorization();
    }
}