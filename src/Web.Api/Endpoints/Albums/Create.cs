using Application.Abstractions.Messaging;
using Application.Albums.Create;
using Application.Playlists.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Albums;

internal sealed class Create : IEndpoint
{
    public sealed record CreateAlbumRequest(string Name);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("albums", async (
            [FromBody] CreateAlbumRequest request,
            ICommandHandler<CreateAlbumCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateAlbumCommand(request.Name);
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Albums)
        .RequireAuthorization();
    }
}