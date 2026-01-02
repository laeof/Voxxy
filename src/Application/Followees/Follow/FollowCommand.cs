using Application.Abstractions.Messaging;
using SharedKernel.Enums;

namespace Application.Followees.Follow;

public sealed record FollowCommand(Guid FollowerId, Guid FolloweeId, FollowType Type) : ICommand<Guid>;