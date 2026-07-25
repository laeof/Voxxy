using Connect.Contracts.States;

namespace Connect.Presentation.Transport;

public enum ConnectCommandAckStatus
{
    Applied,
    Duplicate,
    NoChanges,
    Conflict,
    CommandCollision,
    ConnectionNotFound,
    DeviceNotFound,
    DeviceOffline,
    QueueItemNotFound,
    InvalidQueueIndex,
    ValidationFailed,
    CorruptState,
    Unavailable
}

public sealed record ConnectCommandOutcomeContract(
    Guid? DeviceId,
    string? ConnectionId,
    Guid? QueueItemId,
    long? PlayerVersion,
    long? QueueVersion,
    long? PresenceVersion,
    int? RemovedConnectionCount);

public sealed record ConnectCommandAck(
    Guid? CommandId,
    ConnectCommandAckStatus Status,
    string? ErrorCode,
    ConnectCommandOutcomeContract? Outcome);

public sealed record ConnectSnapshotResponse(
    ConnectCommandAckStatus Status,
    PlayerStateDto? Player,
    QueueStateDto? Queue,
    PresenceStateDto? Presence,
    DateTimeOffset ServerTime,
    string? ErrorCode);
