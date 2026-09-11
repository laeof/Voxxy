using Connect.Contracts.States;

namespace Connect.Presentation.Transport;

public sealed record PlayerStateChangedEvent(
    Guid CommandId,
    PlayerStateDto Player);

public sealed record QueueStateChangedEvent(
    Guid CommandId,
    QueueStateDto Queue);

public sealed record PresenceStateChangedEvent(
    Guid CommandId,
    PresenceStateDto Presence);

public sealed record PlayerQueueStateChangedEvent(
    Guid CommandId,
    PlayerStateDto Player,
    QueueStateDto Queue);

public sealed record PlayerPresenceStateChangedEvent(
    Guid CommandId,
    PlayerStateDto Player,
    PresenceStateDto Presence);
