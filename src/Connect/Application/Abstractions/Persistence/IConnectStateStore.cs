using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;

namespace Connect.Application.Abstractions.Persistence;

public interface IConnectStateStore
{
    Task<PersistenceReadResult<PlayerState>> ReadPlayerAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default);

    Task<PersistenceReadResult<QueueState>> ReadQueueAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PersistenceReadResult<PresenceState>> ReadPresenceAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ConnectSnapshotReadResult> ReadSnapshotAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default);

    Task<PersistenceCommitResult> TryCommitPlayerAsync(
        PlayerCommit commit,
        CancellationToken cancellationToken = default);

    Task<PersistenceCommitResult> TryCommitQueueAsync(
        QueueCommit commit,
        CancellationToken cancellationToken = default);

    Task<PersistenceCommitResult> TryCommitPresenceAsync(
        PresenceCommit commit,
        CancellationToken cancellationToken = default);

    Task<PersistenceCommitResult> TryCommitPlayerAndQueueAsync(
        PlayerQueueCommit commit,
        CancellationToken cancellationToken = default);

    Task<PersistenceCommitResult> TryCommitPlayerAndPresenceAsync(
        PlayerPresenceCommit commit,
        CancellationToken cancellationToken = default);

    Task<PersistenceStatus> RefreshConnectionLeaseAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<ExpiredConnectionsReadResult> ReadExpiredConnectionsAsync(
        Guid userId,
        PresenceState presence,
        CancellationToken cancellationToken = default);
}
