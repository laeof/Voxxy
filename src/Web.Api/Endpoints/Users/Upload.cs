using Application.Abstractions.Messaging;
using Application.Artists.Upload;
using Application.Playlists.Upload;
using Application.Users.Upload;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints.Users;

internal sealed class Upload : IEndpoint
{
    public sealed record UploadUserImageRequest(Guid UserId, IFormFile File);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/upload", async (
            [FromForm] UploadUserImageRequest request,
            ICommandHandler<UploadUserImageCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            if (request.File is null)
            {
                return Results.BadRequest("File is required");
            }

            var command = new UploadUserImageCommand(request.UserId, request.File.OpenReadStream(), request.File.ContentType);

            Result<Guid> userId = await handler.Handle(command, cancellationToken);

            return Results.Ok(userId);
        })
        .WithTags(Tags.Users)
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}