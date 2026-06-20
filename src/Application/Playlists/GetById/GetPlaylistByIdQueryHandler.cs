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
                        Id = track.Release.Id,
                        Name = track.Release.Title,
                        ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.Release.ImageKey).ToString(),
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
