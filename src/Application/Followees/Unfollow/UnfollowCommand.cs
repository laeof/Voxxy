using Application.Abstractions.Messaging;
using SharedKernel.Enums;

namespace Application.Followees.Unfollow;

public sealed record UnfollowCommand(Guid FollowerId, Guid FolloweeId) : ICommand<Guid>;