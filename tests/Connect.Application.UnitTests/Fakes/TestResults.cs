using Connect.Application.Abstractions.Persistence;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;

namespace Connect.Application.UnitTests.Fakes;

internal static class TestResults
{
    public static PersistenceCommitResult Commit(PersistenceStatus status, string? outcome = null) =>
        new(status, outcome, new ConnectVersions(null, null, null));

    public static PersistenceReadResult<PresenceState> Presence(PresenceState state) =>
        new(PersistenceStatus.Success, state, null);

    public static PersistenceReadResult<PlayerState> Player(PlayerState state) =>
        new(PersistenceStatus.Success, state, null);

    public static PersistenceReadResult<QueueState> Queue(QueueState state) =>
        new(PersistenceStatus.Success, state, null);

    public static ConnectSnapshotReadResult Snapshot(
        PlayerState player,
        PresenceState presence) =>
        new(
            PersistenceStatus.Success,
            new ConnectSnapshotState(player, new QueueState(), presence),
            null);

    public static ConnectSnapshotReadResult Snapshot(
        PlayerState player,
        QueueState queue) =>
        new(
            PersistenceStatus.Success,
            new ConnectSnapshotState(player, queue, new PresenceState()),
            null);
}
