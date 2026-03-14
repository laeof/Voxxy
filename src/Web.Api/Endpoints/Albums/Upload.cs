using Application.Abstractions.Messaging;
using Application.Albums.Upload;
using Application.Artists.Upload;
using Application.Playlists.Upload;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints.Albums;

internal sealed class Upload : IEndpoint
{
    public sealed record UploadAlbumImageRequest(Guid AlbumId, IFormFile File);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("albums/upload", async (
            [FromForm] UploadAlbumImageRequest request,
            ICommandHandler<UploadAlbumImageCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            if (request.File is null)
            {
                return Results.BadRequest("File is required");
            }

            var command = new UploadAlbumImageCommand(request.AlbumId, request.File.OpenReadStream(), request.File.ContentType);

            Result<Guid> albumId = await handler.Handle(command, cancellationToken);

            return Results.Ok(albumId);
        })
        .WithTags(Tags.Albums)
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}