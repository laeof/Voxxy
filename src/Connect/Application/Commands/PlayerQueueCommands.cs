using Connect.Domain.Queue;

namespace Connect.Application.Commands;

public sealed record PlayCommand(Guid UserId, Guid CommandId, DateTimeOffset ServerTime);

public sealed record PauseCommand(Guid UserId, Guid CommandId, DateTimeOffset ServerTime);

public sealed record ChangePositionCommand(
    Guid UserId,
    Guid CommandId,
    long PositionMs,
    DateTimeOffset ServerTime);

public sealed record ChangeVolumeCommand(
    Guid UserId,
    Guid CommandId,
    int VolumePercent,
    DateTimeOffset ServerTime);

public sealed record AddQueueItemCommand(
    Guid UserId,
    Guid CommandId,
    Guid QueueItemId,
    Guid TrackId,
    DateTimeOffset ServerTime);

public sealed record RemoveQueueItemCommand(
    Guid UserId,
    Guid CommandId,
    Guid QueueItemId,
    DateTimeOffset ServerTime);

public sealed record MoveQueueItemCommand(
    Guid UserId,
    Guid CommandId,
    Guid QueueItemId,
    int TargetIndex,
    DateTimeOffset ServerTime);

public sealed record SelectQueueItemCommand(
    Guid UserId,
    Guid CommandId,
    Guid QueueItemId,
    DateTimeOffset ServerTime);

public sealed record ShuffleQueueCommand(
    Guid UserId,
    Guid CommandId,
    int Seed,
    DateTimeOffset ServerTime);

public sealed record UnshuffleQueueCommand(
    Guid UserId,
    Guid CommandId,
    DateTimeOffset ServerTime);

public sealed record ChangeRepeatModeCommand(
    Guid UserId,
    Guid CommandId,
    RepeatMode RepeatMode,
    DateTimeOffset ServerTime);

public sealed record NextQueueItemCommand(
    Guid UserId,
    Guid CommandId,
    DateTimeOffset ServerTime);

public sealed record PreviousQueueItemCommand(
    Guid UserId,
    Guid CommandId,
    DateTimeOffset ServerTime);
