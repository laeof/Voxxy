using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Application.Users.GetByEmail;
using Domain.Playlists;
using Domain.Todos;
using Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Tracks.GetById;

internal sealed class GetTrackByIdQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetTrackByIdQuery, TrackResponse>
{
    public async Task<Result<TrackResponse>> Handle(GetTrackByIdQuery query, CancellationToken cancellationToken)
    {
        TrackResponse? track = await context.Tracks
            .Where(track => track.Id == query.TrackId)
            .Select(track => new TrackResponse
            {
                Id = track.Id,
                Name = track.Name,
                CreatedAt = track.CreatedAt,
                UpdatedAt = track.UpdatedAt,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.ImageUrl).ToString(),
                AudioKey = track.AudioKey,
                Duration = track.Duration,
                Album = new AlbumResponse
                {
                    Id = track.AlbumId,
                    Name = track.Album.Name,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.Album.ImageUrl).ToString(),
                },
                Artist = new ArtistResponse
                {
                    Id = track.ArtistId,
                    Name = track.Artist.Name,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, track.Artist.ImageUrl).ToString(),
                }
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (track is null)
        {
            return Result.Failure<TrackResponse>(TrackErrors.NotFound(query.TrackId));
        }

        return track;
    }
}
