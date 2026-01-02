using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Follows;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Followees.Unfollow;

internal sealed class UnfollowCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<UnfollowCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UnfollowCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.FollowerId)
        {
            return Result.Failure<Guid>(FollowErrors.FollowerAndCurrentUserAreDifferent(command.FollowerId, userContext.UserId));
        }

        if (command.FollowerId == command.FolloweeId)
        {
            return Result.Failure<Guid>(FollowErrors.CannotUnfollowOneself(command.FollowerId));
        }

        Following? existingFollow = await context.Followings.AsNoTracking()
            .SingleOrDefaultAsync(f => f.FollowerId == command.FollowerId
                && f.FolloweeId == command.FolloweeId, cancellationToken);

        if (existingFollow is null)
        {
            return Result.Failure<Guid>(FollowErrors.NotFollowing(command.FollowerId, command.FolloweeId));
        }

        context.Followings.Remove(existingFollow);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(command.FollowerId);
    }
}