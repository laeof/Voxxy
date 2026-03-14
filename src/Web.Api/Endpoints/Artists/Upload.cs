using Application.Abstractions.Messaging;
using Application.Artists.Upload;
using Application.Playlists.Upload;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints.Artists;

internal sealed class Upload : IEndpoint
{
    public sealed record UploadArtistImageRequest(Guid ArtistId, IFormFile File);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("artists/upload", async (
            [FromForm] UploadArtistImageRequest request,
            ICommandHandler<UploadArtistImageCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            if (request.File is null)
            {
                return Results.BadRequest("File is required");
            }

            var command = new UploadArtistImageCommand(request.ArtistId, request.File.OpenReadStream(), request.File.ContentType);

            Result<Guid> artistId = await handler.Handle(command, cancellationToken);

            return Results.Ok(artistId);
        })
        .WithTags(Tags.Artists)
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}