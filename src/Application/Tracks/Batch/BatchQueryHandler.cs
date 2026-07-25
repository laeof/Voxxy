using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Artists.GetById;
using Application.Users.GetByEmail;
using Domain.Playlists;
using Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Tracks.Batch;

internal sealed class BatchQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<BatchQuery, List<TrackResponse>>
{
    public async Task<Result<List<TrackResponse>>> Handle(BatchQuery query, CancellationToken cancellationToken)
    {
        List<TrackResponse> tracks = await context.Tracks
            .Where(track => query.TrackIds.Contains(track.Id))
            .Select(track => new TrackResponse
            {
                Id = track.Id,
                Name = track.Name,
                CreatedAt = track.CreatedAt,
                UpdatedAt = track.UpdatedAt,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.ImageKey).ToString(),
                AudioKey = track.AudioKey,
                Duration = track.Duration,
                Album = new AlbumResponse
                {
                    Id = track.Release.Id,
                    Name = track.Release.Title,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.Release.ImageKey).ToString(),
                },
                Artists = track.Artists.Select(artist => new ArtistResponse
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, artist.ImageKey).ToString(),
                }).ToList()

            })
            .ToListAsync(cancellationToken);

        return tracks;
    }
}
