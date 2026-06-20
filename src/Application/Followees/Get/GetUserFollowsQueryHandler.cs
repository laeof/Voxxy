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
                    FollowResponse albumFollowResponse = await context.Releases.AsNoTracking()
                        .Where(x => x.Id == followee.FolloweeId)
                        .Select(x => new FollowResponse
                        {
                            Id = x.Id,
                            FollowType = (FollowType)x.Type,
                            Name = x.Title,
                            PrimaryColor = x.Color,
                            ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Albums, x.ImageKey).ToString(),
                            FollowedSince = followee.CreatedAt,
                            Artist = new ArtistResponse
                            {
                                Id = x.Artists[0].Id,
                                Name = x.Artists[0].Name,
                                ImageUrl = mediaUrlResolver.GetPublicUrl(AzureContainerNames.Artists, x.Artists[0].ImageKey).ToString(),
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
