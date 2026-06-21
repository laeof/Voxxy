using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Application.Users.GetByEmail;
using Domain.ArtistReleases.Constants;
using Domain.ArtistReleases.Enums;
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
        AlbumResponse? album = await context.Releases
            .Where(album => album.Id == query.AlbumId)
            .Select(album => new AlbumResponse
            {
                Id = album.Id,
                Name = album.Title,
                CreatedAt = album.ReleaseDate,
                PrimaryColor = album.Color,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, album.ImageKey).ToString(),
                CreatedBy = album.Artists.Select(artist => new ArtistResponse
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, artist.ImageKey).ToString(),
                }).ToList(),
                Tracks = album.Tracks
                    .OrderBy(track => track.AlbumOrder)
                    .Select(track => new TrackResponse
                    {
                        Id = track.Id,
                        AudioKey = track.AudioKey,
                        ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, album.ImageKey).ToString(),
                        Name = track.Name,
                        Duration = track.Duration,
                        Artists = album.Artists.Select(artist => new ArtistResponse
                        {
                            Id = artist.Id,
                            Name = artist.Name,
                            ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, artist.ImageKey).ToString(),
                        }).ToList(),
                        FromPlaylist = album.Id
                    }).ToList(),
                PlaylistType = (PlaylistType)album.Type,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (album is null)
        {
            return Result.Failure<AlbumResponse>(ReleaseErrors.NotFound(query.AlbumId));
        }

        return album;
    }
}
