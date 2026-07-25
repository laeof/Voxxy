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
    CorruptState,
    Unavailable,
    ValidationFailed
}

public sealed record ConnectCommandOutcome(
    Guid? DeviceId = null,
    string? ConnectionId = null,
    long? PresenceVersion = null,
    int? RemovedConnectionCount = null);

public sealed record ConnectApplicationResult(
    ConnectCommandStatus Status,
    ConnectSnapshot? Snapshot = null,
    PlayerStateDto? Player = null,
    QueueStateDto? Queue = null,
    PresenceStateDto? Presence = null,
    ConnectCommandOutcome? Outcome = null,
    string? Error = null);
