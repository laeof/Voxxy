using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Tracks.GetById;
using Domain.Artists;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Artists.GetById;

internal sealed class GetArtistByIdQueryHandler(IApplicationDbContext context, IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetArtistByIdQuery, ArtistResponse>
{
    public async Task<Result<ArtistResponse>> Handle(GetArtistByIdQuery query, CancellationToken cancellationToken)
    {
        ArtistResponse? artist = await context.Artists
            .Where(artist => artist.Id == query.ArtistId)
            .Select(artist => new ArtistResponse
            {
                Id = artist.Id,
                Name = artist.Name,
                CreatedAt = artist.CreatedAt,
                UpdatedAt = artist.UpdatedAt,
                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, artist.ImageKey).ToString(),
                Albums = artist.Releases
                    .Select(album => new AlbumResponse
                    {
                        Id = album.Id,
                        Name = album.Title,
                        ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, album.ImageKey).ToString(),
                        CreatedAt = album.ReleaseDate,
                        PrimaryColor = album.Color,
                        Tracks = album.Tracks.Select(track => new TrackResponse
                        {
                            Id = track.Id,
                            Name = track.Name,
                            Duration = track.Duration,
                            CreatedAt = track.CreatedAt,
                            UpdatedAt = track.UpdatedAt,
                            AudioKey = track.AudioKey,
                            AlbumOrder = track.AlbumOrder,
                            ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, track.ImageKey).ToString(),

                        }).ToList(),
                        PlaylistType = (PlaylistType)album.Type,
                    })
                    .ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (artist is null)
        {
            return Result.Failure<ArtistResponse>(ArtistErrors.NotFound(query.ArtistId));
        }

        return artist;
    }
}
