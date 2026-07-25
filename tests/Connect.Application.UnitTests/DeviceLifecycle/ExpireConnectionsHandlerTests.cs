using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.DeviceLifecycle;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Synchronization;

namespace Connect.Application.UnitTests.DeviceLifecycle;

public sealed class ExpireConnectionsHandlerTests
{
    [Fact]
    public async Task ExpireConnections_InactiveConnections_CommitsOnlyPresence()
    {
        var activeDevice = Guid.NewGuid();
        PresenceState presence = TestStates.Presence(activeDevice, "active", active: true);
        presence.RegisterConnection(Guid.NewGuid(), "Other", "expired", TestStates.Time);
        FakeConnectStateStore store = SnapshotStore(new PlayerState(TestStates.Time), presence);
        store.CommitPresence = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command(["expired"]));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.CommitPresenceCalls.ShouldBe(1);
        store.CommitPlayerPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ExpireConnections_ActiveConnection_CommitsPlayerAndPresence()
    {
        PresenceState presence = TestStates.Presence(Guid.NewGuid(), "expired", active: true);
        FakeConnectStateStore store = SnapshotStore(TestStates.PlayingPlayer(), presence);
        store.CommitPlayerPresence = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command(["expired"]));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.CommitPlayerPresenceCalls.ShouldBe(1);
        store.CommitPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ExpireConnections_UnknownIds_ReturnsNoChanges()
    {
        FakeConnectStateStore store = SnapshotStore(
            new PlayerState(TestStates.Time),
            TestStates.Presence(Guid.NewGuid(), "known"));
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command(["missing"]));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
    }

    [Fact]
    public async Task DetectExpiredConnections_UsesExplicitIdsFromStore()
    {
        PresenceState presence = TestStates.Presence(Guid.NewGuid(), "expired");
        IReadOnlyCollection<string>? committedIds = null;
        var store = new FakeConnectStateStore
        {
            ReadPresence = _ => TestResults.Presence(presence),
            ReadExpiredConnections = (_, _) => new(
                PersistenceStatus.Success,
                ["expired"],
                null),
            ReadSnapshot = (_, _) => TestResults.Snapshot(
                new PlayerState(TestStates.Time),
                TestStates.Presence(presence.Devices.Single().DeviceId, "expired")),
            CommitPresence = commit =>
            {
                committedIds = commit.State.Devices
                    .SelectMany(device => device.Connections)
                    .Select(connection => connection.ConnectionId)
                    .ToArray();
                return TestResults.Commit(PersistenceStatus.Applied);
            }
        };
        ExpireConnectionsHandler expiryHandler = Handler(store);
        var service = new DetectMissingLeasesService(store, expiryHandler);

        ConnectApplicationResult result = await service.ExecuteAsync(
            new DetectMissingLeasesCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        committedIds.ShouldBeEmpty();
        store.ReadExpiredConnectionsCalls.ShouldBe(1);
    }

    [Fact]
    public async Task DetectExpiredConnections_WithNoMissingLeases_DoesNotCommit()
    {
        var store = new FakeConnectStateStore
        {
            ReadPresence = _ => TestResults.Presence(new PresenceState()),
            ReadExpiredConnections = (_, _) => new(
                PersistenceStatus.Success,
                [],
                null)
        };
        var service = new DetectMissingLeasesService(store, Handler(store));

        ConnectApplicationResult result = await service.ExecuteAsync(
            new DetectMissingLeasesCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.ReadSnapshotCalls.ShouldBe(0);
        store.RecordCommandCalls.ShouldBe(0);
        store.CommitPresenceCalls.ShouldBe(0);
        store.CommitPlayerPresenceCalls.ShouldBe(0);
    }

    private static FakeConnectStateStore SnapshotStore(
        PlayerState player,
        PresenceState presence) =>
        new()
        {
            ReadSnapshot = (_, _) => TestResults.Snapshot(player, presence)
        };

    private static ExpireConnectionsHandler Handler(FakeConnectStateStore store) =>
        new(store, new ConnectStateCoordinator());

    private static ExpireConnectionsCommand Command(IReadOnlyCollection<string> ids) =>
        new(Guid.NewGuid(), Guid.NewGuid(), ids, TestStates.Time.AddSeconds(5));
}
