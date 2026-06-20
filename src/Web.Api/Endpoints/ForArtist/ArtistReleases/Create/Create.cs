using System.Text.Json;
using Application.Abstractions.Messaging;
using Application.ArtistReleases.Create;
using Domain.ArtistReleases.Enums;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ForArtist.Releases.Create;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("for-artist/releases", async (
            [FromForm] CreateReleaseRequest request,
            ICommandHandler<CreateReleaseCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var ReleaseTracks = request.Tracks
                .Select(t => new CreateReleaseTrack(
                    t.Title,
                    t.Position,
                    t.Duration,
                    t.IsRemix,
                    t.AudioFile.OpenReadStream()
                )).ToList();

            var command = new CreateReleaseCommand(request.Title, request.AdditionalInformation, request.Copyright, request.ReleaseDate, request.ReleaseType, request.ArtistIds, request.GenreIds, request.MoodIds, ReleaseTracks, request.CoverImage.OpenReadStream());

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.ForArtistReleases)
        .RequireAuthorization();
    }
}