using Connect.Contracts.States;

namespace Connect.Application.Results;

public enum ConnectCommandStatus
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
    CorruptState,
    Unavailable,
    ValidationFailed
}

public sealed record ConnectCommandOutcome(
    Guid? DeviceId = null,
    string? ConnectionId = null,
    long? PresenceVersion = null,
    int? RemovedConnectionCount = null,
    Guid? QueueItemId = null,
    long? PlayerVersion = null,
    long? QueueVersion = null);

public sealed record ConnectApplicationResult(
    ConnectCommandStatus Status,
    ConnectSnapshot? Snapshot = null,
    PlayerStateDto? Player = null,
    QueueStateDto? Queue = null,
    PresenceStateDto? Presence = null,
    ConnectCommandOutcome? Outcome = null,
    string? Error = null);
