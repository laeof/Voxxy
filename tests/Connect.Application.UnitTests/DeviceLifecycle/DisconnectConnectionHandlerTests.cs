using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.DeviceLifecycle;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Synchronization;

namespace Connect.Application.UnitTests.DeviceLifecycle;

public sealed class DisconnectConnectionHandlerTests
{
    [Fact]
    public async Task Disconnect_InactiveConnection_CommitsOnlyPresence()
    {
        var activeDevice = Guid.NewGuid();
        PresenceState presence = TestStates.Presence(activeDevice, "active", active: true);
        presence.RegisterConnection(Guid.NewGuid(), "Other", "inactive", TestStates.Time);
        FakeConnectStateStore store = SnapshotStore(new PlayerState(TestStates.Time), presence);
        store.CommitPresence = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command("inactive"));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.CommitPresenceCalls.ShouldBe(1);
        store.CommitPlayerPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Disconnect_ActiveOwner_CommitsPlayerAndPresenceAtomically()
    {
        PresenceState presence = TestStates.Presence(Guid.NewGuid(), "owner", active: true);
        PlayerState player = TestStates.PlayingPlayer();
        PlayerPresenceCommit? captured = null;
        FakeConnectStateStore store = SnapshotStore(player, presence);
        store.CommitPlayerPresence = commit =>
        {
            captured = commit;
            return TestResults.Commit(PersistenceStatus.Applied);
        };

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command("owner"));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        captured.ShouldNotBeNull();
        captured.Player.IsPlaying.ShouldBeFalse();
        captured.Presence.ActiveDeviceId.ShouldBeNull();
        store.CommitPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Disconnect_UnknownConnection_ReturnsNoChanges()
    {
        FakeConnectStateStore store = SnapshotStore(
            new PlayerState(TestStates.Time),
            TestStates.Presence(Guid.NewGuid(), "known"));
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command("unknown"));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
        store.CommitPresenceCalls.ShouldBe(0);
        store.CommitPlayerPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Disconnect_OnConflict_RetriesWithFreshSnapshot()
    {
        ConnectSnapshotState[] snapshots =
        [
            new(
                new PlayerState(TestStates.Time),
                new(),
                TestStates.Presence(Guid.NewGuid(), "connection-a")),
            new(
                new PlayerState(TestStates.Time),
                new(),
                TestStates.Presence(Guid.NewGuid(), "connection-a"))
        ];
        var committed = new List<PresenceState>();
        var store = new FakeConnectStateStore();
        store.ReadSnapshot = (_, _) => new(
            PersistenceStatus.Success,
            snapshots[store.ReadSnapshotCalls - 1],
            null);
        store.CommitPresence = commit =>
        {
            committed.Add(commit.State);
            return TestResults.Commit(
                committed.Count == 1
                    ? PersistenceStatus.VersionConflict
                    : PersistenceStatus.Applied);
        };

        ConnectApplicationResult result = await Handler(store).HandleAsync(
            Command("connection-a"));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.ReadSnapshotCalls.ShouldBe(2);
        ReferenceEquals(committed[0], committed[1]).ShouldBeFalse();
    }

    private static FakeConnectStateStore SnapshotStore(
        PlayerState player,
        PresenceState presence) =>
        new()
        {
            ReadSnapshot = (_, _) => TestResults.Snapshot(player, presence)
        };

    private static DisconnectConnectionHandler Handler(FakeConnectStateStore store) =>
        new(store, new ConnectStateCoordinator());

    private static DisconnectConnectionCommand Command(string connectionId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), connectionId, TestStates.Time.AddSeconds(5));
}
