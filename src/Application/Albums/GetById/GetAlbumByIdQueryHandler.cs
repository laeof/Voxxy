using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Application.Users.GetByEmail;
using Domain.Albums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Albums.GetById;

internal sealed class GetAlbumByIdQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetAlbumByIdQuery, AlbumResponse>
{
    public async Task<Result<AlbumResponse>> Handle(GetAlbumByIdQuery query, CancellationToken cancellationToken)
    {
        AlbumResponse? album = await context.Albums
            .Where(album => album.Id == query.AlbumId)
            .Select(album => new AlbumResponse
            {
                Id = album.Id,
                Name = album.Name,
                CreatedAt = album.CreatedAt,
                UpdatedAt = album.UpdatedAt,
                PrimaryColor = album.Color,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, album.ImageUrl).ToString(),
                CreatedBy = new ArtistResponse
                {
                    Id = album.Artist.Id,
                    Name = album.Artist.Name,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, album.Artist.ImageUrl).ToString(),
                },
                Tracks = album.Tracks.Select(track => new TrackResponse
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
                    FromPlaylist = album.Id
                }).ToList(),
                PlaylistType = (PlaylistType)album.Type,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (album is null)
        {
            return Result.Failure<AlbumResponse>(AlbumErrors.NotFound(query.AlbumId));
        }

        return album;
    }
}
