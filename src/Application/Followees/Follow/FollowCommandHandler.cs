using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Follows;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Enums;

namespace Application.Followees.Follow;

internal sealed class FollowCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<FollowCommand, Guid>
{
    public async Task<Result<Guid>> Handle(FollowCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.FollowerId)
        {
            return Result.Failure<Guid>(FollowErrors.FollowerAndCurrentUserAreDifferent(command.FollowerId, userContext.UserId));
        }

        if (command.FollowerId == command.FolloweeId)
        {
            return Result.Failure<Guid>(FollowErrors.CannotFollowOneself(command.FollowerId));
        }

        if (!Enum.IsDefined(command.Type))
        {
            return Result.Failure<Guid>(FollowErrors.InvalidFollowType(command.Type));
        }

        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.FollowerId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(command.FollowerId));
        }

        if (user.Id != command.FollowerId)
        {
            return Result.Failure<Guid>(FollowErrors.FollowerAndCurrentUserAreDifferent(user.Id, command.FollowerId));
        }

        Following? existingFollow = await context.Followings.AsNoTracking()
            .SingleOrDefaultAsync(f =>
                f.FollowerId == command.FollowerId &&
                f.FolloweeId == command.FolloweeId &&
                f.Type == command.Type,
                cancellationToken);

        if (existingFollow is not null)
        {
            return Result.Failure<Guid>(FollowErrors.AlreadyFollowing(command.FollowerId, command.FolloweeId));
        }

        var follow = new Following
        {
            Id = Guid.NewGuid(),
            FollowerId = command.FollowerId,
            FolloweeId = command.FolloweeId,
            Type = command.Type,
            CreatedAt = dateTimeProvider.UtcNow,
        };

        context.Followings.Add(follow);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(follow.Id);
    }
}