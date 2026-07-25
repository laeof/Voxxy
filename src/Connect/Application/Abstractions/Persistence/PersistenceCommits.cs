using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;

namespace Connect.Application.Abstractions.Persistence;

public sealed record PersistenceCommand(
    Guid CommandId,
    string CommandType,
    string Fingerprint,
    string Outcome);

public sealed record PlayerCommit(
    Guid UserId,
    PersistenceCommand Command,
    long ExpectedVersion,
    PlayerState State);

public sealed record QueueCommit(
    Guid UserId,
    PersistenceCommand Command,
    long ExpectedVersion,
    QueueState State);

public sealed record PresenceCommit(
    Guid UserId,
    PersistenceCommand Command,
    long ExpectedVersion,
    PresenceState State,
    string? RegisteredConnectionId = null);

public sealed record PlayerQueueCommit(
    Guid UserId,
    PersistenceCommand Command,
    long ExpectedPlayerVersion,
    PlayerState Player,
    long ExpectedQueueVersion,
    QueueState Queue);

public sealed record PlayerPresenceCommit(
    Guid UserId,
    PersistenceCommand Command,
    long ExpectedPlayerVersion,
    PlayerState Player,
    long ExpectedPresenceVersion,
    PresenceState Presence);
