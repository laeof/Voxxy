namespace Connect.Contracts.States;

public sealed record ConnectSnapshot(
    Guid UserId,
    PlayerStateDto Player,
    QueueStateDto Queue,
    PresenceStateDto Presence,
    DateTimeOffset ServerTime);

public sealed record ConnectStateChanged(
    Guid EventId,
    Guid? CausationCommandId,
    DateTimeOffset ServerTime,
    PlayerStateDto? Player,
    QueueStateDto? Queue,
    PresenceStateDto? Presence);
