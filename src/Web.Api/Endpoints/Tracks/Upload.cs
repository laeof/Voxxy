using Application.Abstractions.Messaging;
using Application.Tracks.GetById;
using Application.Tracks.Upload;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints.Tracks;

internal sealed class Upload : IEndpoint
{
    public sealed record UploadTrackRequest(Guid TrackId, IFormFile File);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("tracks/upload", async (
            [FromForm] UploadTrackRequest request,
            ICommandHandler<UploadTrackCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            if (request.File is null)
            {
                return Results.BadRequest("File is required");
            }

            var command = new UploadTrackCommand(request.TrackId, request.File.OpenReadStream(), request.File.ContentType);

            Result<Guid> trackId = await handler.Handle(command, cancellationToken);

            return Results.Ok(trackId);
        })
        .WithTags(Tags.Tracks)
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}