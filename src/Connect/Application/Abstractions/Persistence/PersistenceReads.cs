using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;

namespace Connect.Application.Abstractions.Persistence;

public sealed record PersistenceReadResult<T>(
    PersistenceStatus Status,
    T? State,
    string? Error)
    where T : class;

public sealed record ConnectSnapshotState(
    PlayerState Player,
    QueueState Queue,
    PresenceState Presence);

public sealed record ConnectSnapshotReadResult(
    PersistenceStatus Status,
    ConnectSnapshotState? Snapshot,
    string? Error);

public sealed record ExpiredConnectionsReadResult(
    PersistenceStatus Status,
    IReadOnlyList<string> ConnectionIds,
    string? Error);
