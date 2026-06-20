using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Application.Users.GetByEmail;
using Domain.Playlists;
using Domain.Tracks;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Playlists.GetTracks;

internal sealed class GetPlaylistTracksQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetPlaylistTracksQuery, List<TrackResponse>>
{
    public async Task<Result<List<TrackResponse>>> Handle(GetPlaylistTracksQuery query, CancellationToken cancellationToken)
    {
        List<TrackResponse>? tracks = await context.Playlists
            .Where(playlist => playlist.Id == query.PlaylistId)
            .SelectMany(playlist => playlist.Tracks)
            .Select(track => new TrackResponse
            {
                Id = track.Id,
                Name = track.Name,
                Duration = track.Duration,
                CreatedAt = track.CreatedAt,
                UpdatedAt = track.UpdatedAt,
                AudioKey = track.AudioKey,
                AlbumOrder = track.AlbumOrder,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.ImageKey).ToString(),
                Album = new AlbumResponse
                {
                    Id = track.Release.Id,
                    Name = track.Release.Title,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.Release.ImageKey).ToString(),
                    CreatedAt = track.Release.ReleaseDate,
                    PrimaryColor = track.Release.Color,
                    PlaylistType = (PlaylistType)track.Release.Type,
                },
                Artists = track.Release.Artists.Select(artist => new ArtistResponse
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    CreatedAt = artist.CreatedAt,
                    UpdatedAt = artist.UpdatedAt,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, artist.ImageKey).ToString(),
                }).ToList(),
                FromPlaylist = query.PlaylistId
            })
            .ToListAsync(cancellationToken);

        if (tracks is null)
        {
            return new List<TrackResponse>();
        }

        return tracks;
    }
}
