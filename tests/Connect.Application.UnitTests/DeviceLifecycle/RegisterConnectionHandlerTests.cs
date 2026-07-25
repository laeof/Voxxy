using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.DeviceLifecycle;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Presence;

namespace Connect.Application.UnitTests.DeviceLifecycle;

public sealed class RegisterConnectionHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CommandId = Guid.NewGuid();
    private static readonly Guid DeviceId = Guid.NewGuid();

    [Fact]
    public async Task RegisterConnection_NewConnection_CommitsPresenceAndLease()
    {
        PresenceCommit? captured = null;
        var store = new FakeConnectStateStore
        {
            ReadPresence = _ => TestResults.Presence(new PresenceState()),
            CommitPresence = commit =>
            {
                captured = commit;
                return TestResults.Commit(PersistenceStatus.Applied);
            }
        };

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command());

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        captured.ShouldNotBeNull();
        captured.RegisteredConnectionId.ShouldBe("connection-a");
        captured.State.Devices.Single().DeviceId.ShouldBe(DeviceId);
        store.CommitPresenceCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RegisterConnection_DuplicateCommand_ReturnsDuplicateOutcome()
    {
        var original = new ConnectCommandOutcome(DeviceId, "connection-a", 12);
        var store = new FakeConnectStateStore
        {
            ReadPresence = _ => TestResults.Presence(new PresenceState()),
            CommitPresence = _ => TestResults.Commit(
                PersistenceStatus.Duplicate,
                """{"deviceId":"00000000-0000-0000-0000-000000000001","connectionId":"original","presenceVersion":12}""")
        };

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command());

        result.Status.ShouldBe(ConnectCommandStatus.Duplicate);
        result.Outcome.ShouldNotBeNull();
        result.Outcome.ConnectionId.ShouldBe("original");
        result.Outcome.PresenceVersion.ShouldBe(original.PresenceVersion);
    }

    [Fact]
    public async Task RegisterConnection_CommandIdWithDifferentPayload_ReturnsCollision()
    {
        var store = new FakeConnectStateStore
        {
            ReadPresence = _ => TestResults.Presence(new PresenceState()),
            CommitPresence = _ => TestResults.Commit(PersistenceStatus.CommandCollision)
        };

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command());

        result.Status.ShouldBe(ConnectCommandStatus.CommandCollision);
    }

    [Fact]
    public async Task RegisterConnection_OnConflict_RereadsAndRetries()
    {
        var store = new FakeConnectStateStore
        {
            ReadPresence = _ => TestResults.Presence(new PresenceState())
        };
        int commits = 0;
        store.CommitPresence = _ => TestResults.Commit(
            ++commits == 1 ? PersistenceStatus.VersionConflict : PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command());

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.ReadPresenceCalls.ShouldBe(2);
        store.CommitPresenceCalls.ShouldBe(2);
    }

    [Fact]
    public async Task RegisterConnection_DoesNotReuseMutatedStateBetweenRetries()
    {
        PresenceState[] states = new[] { new PresenceState(), new PresenceState() };
        var committedStates = new List<PresenceState>();
        var store = new FakeConnectStateStore();
        store.ReadPresence = _ => TestResults.Presence(states[store.ReadPresenceCalls - 1]);
        store.CommitPresence = commit =>
        {
            committedStates.Add(commit.State);
            return TestResults.Commit(
                committedStates.Count == 1
                    ? PersistenceStatus.VersionConflict
                    : PersistenceStatus.Applied);
        };

        await Handler(store).HandleAsync(Command());

        committedStates.Count.ShouldBe(2);
        ReferenceEquals(committedStates[0], committedStates[1]).ShouldBeFalse();
        committedStates[0].Version.ShouldBe(1);
        committedStates[1].Version.ShouldBe(1);
    }

    [Fact]
    public async Task RegisterConnection_InvalidCommand_DoesNotReadStore()
    {
        var store = new FakeConnectStateStore();

        ConnectApplicationResult result = await Handler(store).HandleAsync(
            Command() with { UserId = Guid.Empty });

        result.Status.ShouldBe(ConnectCommandStatus.ValidationFailed);
        store.ReadPresenceCalls.ShouldBe(0);
    }

    private static RegisterConnectionHandler Handler(FakeConnectStateStore store) => new(store);

    private static RegisterConnectionCommand Command() =>
        new(UserId, CommandId, DeviceId, "Browser", "connection-a", TestStates.Time);
}
