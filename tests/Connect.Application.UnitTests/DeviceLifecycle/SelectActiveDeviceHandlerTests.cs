using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.DeviceLifecycle;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Player;
using Connect.Domain.Presence;

namespace Connect.Application.UnitTests.DeviceLifecycle;

public sealed class SelectActiveDeviceHandlerTests
{
    [Fact]
    public async Task SelectDevice_ExistingDevice_CommitsPresence()
    {
        var deviceId = Guid.NewGuid();
        FakeConnectStateStore store = Store(deviceId);
        store.CommitPresence = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(Command(deviceId));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Presence.ShouldNotBeNull();
        result.Presence.ActiveDeviceId.ShouldBe(deviceId);
        store.CommitPresenceCalls.ShouldBe(1);
        store.CommitPlayerPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task SelectDevice_MissingDevice_ReturnsDeviceNotFound()
    {
        FakeConnectStateStore store = Store(Guid.NewGuid());
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await Handler(store).HandleAsync(
            Command(Guid.NewGuid()));

        result.Status.ShouldBe(ConnectCommandStatus.DeviceNotFound);
        store.RecordCommandCalls.ShouldBe(1);
        store.CommitPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task SelectDevice_DoesNotChangePlayerUnderCurrentDomainContract()
    {
        var deviceId = Guid.NewGuid();
        FakeConnectStateStore store = Store(deviceId);
        store.CommitPresence = _ => TestResults.Commit(PersistenceStatus.Applied);

        await Handler(store).HandleAsync(Command(deviceId));

        store.CommitPresenceCalls.ShouldBe(1);
        store.CommitPlayerPresenceCalls.ShouldBe(0);
    }

    private static FakeConnectStateStore Store(Guid deviceId)
    {
        var presence = new PresenceState();
        presence.RegisterConnection(
            Guid.NewGuid(),
            "Initially active",
            "connection-other",
            TestStates.Time);
        presence.RegisterConnection(
            deviceId,
            "Selected device",
            "connection-a",
            TestStates.Time.AddSeconds(1));

        return new FakeConnectStateStore
        {
            ReadSnapshot = (_, _) => TestResults.Snapshot(
                new PlayerState(TestStates.Time),
                presence)
        };
    }

    private static SelectActiveDeviceHandler Handler(FakeConnectStateStore store) => new(store);

    private static SelectActiveDeviceCommand Command(Guid deviceId) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            deviceId,
            "connection-a",
            TestStates.Time);
}
