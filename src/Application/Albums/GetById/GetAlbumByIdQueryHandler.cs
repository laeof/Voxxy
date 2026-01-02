using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Artists.GetById;
using Application.Tracks.GetById;
using Application.Users.GetByEmail;
using Domain.Albums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Enums;

namespace Application.Albums.GetById;

internal sealed class GetAlbumByIdQueryHandler(IApplicationDbContext context)
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
                ImageUrl = album.ImageUrl,
                CreatedBy = new ArtistResponse
                {
                    Id = album.Artist.Id,
                    Name = album.Artist.Name,
                    ImageUrl = album.Artist.ImageUrl,
                },
                Tracks = album.Tracks
                    .Select(track => new TrackResponse
                    {
                        Id = track.Id,
                        Name = track.Name,
                        Duration = track.Duration,
                        AlbumOrder = track.AlbumOrder,
                        CreatedAt = track.CreatedAt,
                        UpdatedAt = track.UpdatedAt,
                    })
                    .ToList(),
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
