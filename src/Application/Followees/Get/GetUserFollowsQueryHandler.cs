using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Media;
using Application.Abstractions.Messaging;
using Application.Albums.GetById;
using Application.Artists.GetById;
using Application.Playlists.GetById;
using Application.Tracks.GetById;
using Domain.Follows;
using Domain.Playlists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Constants;
using SharedKernel.Enums;

namespace Application.Followees.Get;

internal sealed class GetUserFollowsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IMediaUrlResolver mediaUrlResolver)
    : IQueryHandler<GetUserFollowsQuery, List<FollowResponse>>
{
    public async Task<Result<List<FollowResponse>>> Handle(
        GetUserFollowsQuery query,
        CancellationToken cancellationToken)
    {
        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<List<FollowResponse>>(UserErrors.NotFound(userContext.UserId));
        }

        List<Following> followees = await context.Followings.AsNoTracking()
            .Where(f => f.FollowerId == userContext.UserId)
            .ToListAsync(cancellationToken);

        var followResponses = new List<FollowResponse>();

        foreach (Following followee in followees)
        {
            switch (followee.Type)
            {
                case FollowType.LovedSongs:
                case FollowType.Playlist:
                    FollowResponse followResponse = await context.Playlists.AsNoTracking()
                        .Where(x => x.Id == followee.FolloweeId)
                        .Select(x => new FollowResponse
                        {
                            Id = x.Id,
                            FollowType = (FollowType)x.Type,
                            Name = x.Name,
                            PrimaryColor = x.Color,
                            ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Playlists, x.ImageKey).ToString(),
                            FollowedSince = followee.CreatedAt,
                            Artist = new ArtistResponse
                            {
                                Id = x.CreatedByUser.Id,
                                Name = x.CreatedByUser.FirstName + " " + x.CreatedByUser.LastName,
                            },
                            Tracks = x.Tracks.Select(track => new TrackResponse
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
                                FromPlaylist = x.Id
                            }).ToList(),
                        })
                        .SingleOrDefaultAsync(cancellationToken);

                    if (followResponse is not null)
                    {
                        followResponses.Add(followResponse);
                    }
                    break;

                case FollowType.Album:
                case FollowType.Single:
                    FollowResponse albumFollowResponse = await context.Albums.AsNoTracking()
                        .Where(x => x.Id == followee.FolloweeId)
                        .Select(x => new FollowResponse
                        {
                            Id = x.Id,
                            FollowType = (FollowType)x.Type,
                            Name = x.Name,
                            PrimaryColor = x.Color,
                            ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, x.ImageKey).ToString(),
                            FollowedSince = followee.CreatedAt,
                            Artist = new ArtistResponse
                            {
                                Id = x.Artist.Id,
                                Name = x.Artist.Name,
                                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, x.Artist.ImageKey).ToString(),
                            },
                            Tracks = x.Tracks.Select(track => new TrackResponse
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
                                FromPlaylist = x.Id
                            }).ToList(),
                        })
                        .SingleOrDefaultAsync(cancellationToken);

                    if (albumFollowResponse is not null)
                    {
                        followResponses.Add(albumFollowResponse);
                    }
                    break;
                case FollowType.Artist:
                    FollowResponse artistFollowResponse = await context.Artists.AsNoTracking()
                        .Where(x => x.Id == followee.FolloweeId)
                        .Select(x => new FollowResponse
                        {
                            Id = x.Id,
                            FollowType = FollowType.Artist,
                            Name = x.Name,
                            ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, x.ImageKey).ToString(),
                            FollowedSince = followee.CreatedAt,
                        })
                        .SingleOrDefaultAsync(cancellationToken);

                    if (artistFollowResponse is not null)
                    {
                        followResponses.Add(artistFollowResponse);
                    }
                    break;
                case FollowType.User:
                    FollowResponse userFollowResponse = await context.Users.AsNoTracking()
                        .Where(x => x.Id == followee.FolloweeId)
                        .Select(x => new FollowResponse
                        {
                            Id = x.Id,
                            FollowType = FollowType.User,
                            Name = x.FirstName + " " + x.LastName,
                            ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Users, x.ImageKey).ToString(),
                            FollowedSince = followee.CreatedAt,
                        })
                        .SingleOrDefaultAsync(cancellationToken);

                    if (userFollowResponse is not null)
                    {
                        followResponses.Add(userFollowResponse);
                    }
                    break;
                default:
                    return Result.Failure<List<FollowResponse>>(FollowErrors.InvalidFollowType(followee.Type));
            }
        }

        return Result.Success(followResponses);
    }
}
