using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Application.Users.GetByEmail;
using Domain.Playlists;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Playlists.GetById;

internal sealed class GetPlaylistByIdQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetPlaylistByIdQuery, PlaylistResponse>
{
    public async Task<Result<PlaylistResponse>> Handle(GetPlaylistByIdQuery query, CancellationToken cancellationToken)
    {
        PlaylistResponse? playlist = await context.Playlists
            .Where(playlist => playlist.Id == query.PlaylistId)
            .Select(playlist => new PlaylistResponse
            {
                Id = playlist.Id,
                Name = playlist.Name,
                CreatedAt = playlist.CreatedAt,
                UpdatedAt = playlist.UpdatedAt,
                PrimaryColor = playlist.Color,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Playlists, playlist.ImageKey).ToString(),
                CreatedBy = new UserResponse
                {
                    Id = playlist.CreatedByUser.Id,
                    FullName = playlist.CreatedByUser.FirstName + " " + playlist.CreatedByUser.LastName,
                    Email = playlist.CreatedByUser.Email,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Users, playlist.CreatedByUser.ImageKey).ToString()
                },
                PlaylistType = (PlaylistType)playlist.Type,
                Tracks = playlist.Tracks.Select(track => new TrackResponse
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
                        Id = track.Album.Id,
                        Name = track.Album.Name,
                        ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.Album.ImageKey).ToString(),
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
                        ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, track.Album.Artist.ImageKey).ToString(),
                    },
                    FromPlaylist = playlist.Id
                }).ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (playlist is null)
        {
            return Result.Failure<PlaylistResponse>(PlaylistErrors.NotFound(query.PlaylistId));
        }

        return playlist;
    }
}
