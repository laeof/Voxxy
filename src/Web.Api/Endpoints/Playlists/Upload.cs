using Application.Abstractions.Messaging;
using Application.Playlists.Upload;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints.Playlists;

internal sealed class Upload : IEndpoint
{
    public sealed record UploadPlaylistImageRequest(Guid PlaylistId, IFormFile File);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("playlists/upload", async (
            [FromForm] UploadPlaylistImageRequest request,
            ICommandHandler<UploadPlaylistImageCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            if (request.File is null)
            {
                return Results.BadRequest("File is required");
            }

            var command = new UploadPlaylistImageCommand(request.PlaylistId, request.File.OpenReadStream(), request.File.ContentType);

            Result<Guid> playlistId = await handler.Handle(command, cancellationToken);

            return Results.Ok(playlistId);
        })
        .WithTags(Tags.Playlists)
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}