using Connect.Application.Abstractions.Persistence;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;

namespace Connect.Application.UnitTests.Fakes;

internal sealed class FakeConnectStateStore : IConnectStateStore
{
    public int ReadPlayerCalls { get; private set; }
    public int ReadQueueCalls { get; private set; }
    public int ReadPresenceCalls { get; private set; }
    public int ReadSnapshotCalls { get; private set; }
    public int RecordCommandCalls { get; private set; }
    public int CommitPresenceCalls { get; private set; }
    public int CommitPlayerPresenceCalls { get; private set; }
    public int RefreshLeaseCalls { get; private set; }
    public int ReadExpiredConnectionsCalls { get; private set; }

    public Func<Guid, DateTimeOffset, PersistenceReadResult<PlayerState>>? ReadPlayer { get; set; }
    public Func<Guid, PersistenceReadResult<QueueState>>? ReadQueue { get; set; }
    public Func<Guid, PersistenceReadResult<PresenceState>>? ReadPresence { get; set; }
    public Func<Guid, DateTimeOffset, ConnectSnapshotReadResult>? ReadSnapshot { get; set; }
    public Func<CommandRecordCommit, PersistenceCommitResult>? RecordCommand { get; set; }
    public Func<PlayerCommit, PersistenceCommitResult>? CommitPlayer { get; set; }
    public Func<QueueCommit, PersistenceCommitResult>? CommitQueue { get; set; }
    public Func<PresenceCommit, PersistenceCommitResult>? CommitPresence { get; set; }
    public Func<PlayerQueueCommit, PersistenceCommitResult>? CommitPlayerQueue { get; set; }
    public Func<PlayerPresenceCommit, PersistenceCommitResult>? CommitPlayerPresence { get; set; }
    public Func<Guid, string, PersistenceStatus>? RefreshLease { get; set; }
    public Func<Guid, PresenceState, ExpiredConnectionsReadResult>? ReadExpiredConnections { get; set; }

    public Task<PersistenceReadResult<PlayerState>> ReadPlayerAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadPlayerCalls++;
        return Task.FromResult(
            ReadPlayer?.Invoke(userId, serverTime) ??
            throw new InvalidOperationException("ReadPlayer was not configured."));
    }

    public Task<PersistenceReadResult<QueueState>> ReadQueueAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadQueueCalls++;
        return Task.FromResult(
            ReadQueue?.Invoke(userId) ??
            throw new InvalidOperationException("ReadQueue was not configured."));
    }

    public Task<PersistenceReadResult<PresenceState>> ReadPresenceAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadPresenceCalls++;
        return Task.FromResult(
            ReadPresence?.Invoke(userId) ??
            throw new InvalidOperationException("ReadPresence was not configured."));
    }

    public Task<ConnectSnapshotReadResult> ReadSnapshotAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadSnapshotCalls++;
        return Task.FromResult(
            ReadSnapshot?.Invoke(userId, serverTime) ??
            throw new InvalidOperationException("ReadSnapshot was not configured."));
    }

    public Task<PersistenceCommitResult> TryRecordCommandAsync(
        CommandRecordCommit commit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RecordCommandCalls++;
        return Task.FromResult(
            RecordCommand?.Invoke(commit) ??
            throw new InvalidOperationException("RecordCommand was not configured."));
    }

    public Task<PersistenceCommitResult> TryCommitPlayerAsync(
        PlayerCommit commit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            CommitPlayer?.Invoke(commit) ??
            throw new InvalidOperationException("CommitPlayer was not configured."));
    }

    public Task<PersistenceCommitResult> TryCommitQueueAsync(
        QueueCommit commit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            CommitQueue?.Invoke(commit) ??
            throw new InvalidOperationException("CommitQueue was not configured."));
    }

    public Task<PersistenceCommitResult> TryCommitPresenceAsync(
        PresenceCommit commit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommitPresenceCalls++;
        return Task.FromResult(
            CommitPresence?.Invoke(commit) ??
            throw new InvalidOperationException("CommitPresence was not configured."));
    }

    public Task<PersistenceCommitResult> TryCommitPlayerAndQueueAsync(
        PlayerQueueCommit commit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            CommitPlayerQueue?.Invoke(commit) ??
            throw new InvalidOperationException("CommitPlayerQueue was not configured."));
    }

    public Task<PersistenceCommitResult> TryCommitPlayerAndPresenceAsync(
        PlayerPresenceCommit commit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommitPlayerPresenceCalls++;
        return Task.FromResult(
            CommitPlayerPresence?.Invoke(commit) ??
            throw new InvalidOperationException(
                "CommitPlayerPresence was not configured."));
    }

    public Task<PersistenceStatus> RefreshConnectionLeaseAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RefreshLeaseCalls++;
        return Task.FromResult(
            RefreshLease?.Invoke(userId, connectionId) ??
            throw new InvalidOperationException("RefreshLease was not configured."));
    }

    public Task<ExpiredConnectionsReadResult> ReadExpiredConnectionsAsync(
        Guid userId,
        PresenceState presence,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadExpiredConnectionsCalls++;
        return Task.FromResult(
            ReadExpiredConnections?.Invoke(userId, presence) ??
            throw new InvalidOperationException(
                "ReadExpiredConnections was not configured."));
    }
}
