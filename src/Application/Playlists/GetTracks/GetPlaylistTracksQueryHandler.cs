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
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.ImageUrl).ToString(),
                Album = new AlbumResponse
                {
                    Id = track.Album.Id,
                    Name = track.Album.Name,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.Album.ImageUrl).ToString(),
                    CreatedAt = track.Album.CreatedAt,
                    UpdatedAt = track.Album.UpdatedAt,
                    PrimaryColor = track.Album.Color,
                    PlaylistType = (PlaylistType)track.Album.Type,
                },
                Artist = new ArtistResponse
                {
                    Id = track.Album.Artist.Id,
                    Name = track.Album.Artist.Name,
                    CreatedAt = track.Album.Artist.CreatedAt,
                    UpdatedAt = track.Album.Artist.UpdatedAt,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, track.Album.Artist.ImageUrl).ToString(),
                },
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
