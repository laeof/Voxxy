using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Artists.GetById;
using Application.Playlists.GetById;
using Application.Tracks.GetById;
using Domain.Follows;
using Domain.Playlists;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Enums;

namespace Application.Followees.Get;

internal sealed class GetUserFollowsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
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
                            ImageUrl = x.ImageUrl,
                            FollowedSince = followee.CreatedAt,
                            Artist = new ArtistResponse
                            {
                                Id = x.CreatedByUser.Id,
                                Name = x.CreatedByUser.FirstName + " " + x.CreatedByUser.LastName,
                            },
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
                            ImageUrl = x.ImageUrl,
                            FollowedSince = followee.CreatedAt,
                            Artist = new ArtistResponse
                            {
                                Id = x.Artist.Id,
                                Name = x.Artist.Name,
                                ImageUrl = x.Artist.ImageUrl,
                            },
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
                            ImageUrl = x.ImageUrl,
                            FollowedSince = followee.CreatedAt,
                        })
                        .SingleOrDefaultAsync(cancellationToken);

                    if (artistFollowResponse is not null)
                    {
                        followResponses.Add(artistFollowResponse);
                    }
                    break;
                default:
                    return Result.Failure<List<FollowResponse>>(FollowErrors.InvalidFollowType(followee.Type));
            }
        }

        return Result.Success(followResponses);
    }
}
