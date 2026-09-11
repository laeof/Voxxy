using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.DeviceLifecycle;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;

namespace Connect.Application.UnitTests.DeviceLifecycle;

public sealed class RefreshConnectionLeaseHandlerTests
{
    [Fact]
    public async Task RefreshLease_DelegatesDirectlyToStore()
    {
        var userId = Guid.NewGuid();
        Guid? capturedUserId = null;
        string? capturedConnectionId = null;
        var store = new FakeConnectStateStore
        {
            RefreshLease = (actualUserId, connectionId) =>
            {
                capturedUserId = actualUserId;
                capturedConnectionId = connectionId;
                return PersistenceStatus.Applied;
            }
        };

        ConnectApplicationResult result = await new RefreshConnectionLeaseHandler(store)
            .HandleAsync(new RefreshConnectionLeaseCommand(userId, "connection-a"));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        capturedUserId.ShouldBe(userId);
        capturedConnectionId.ShouldBe("connection-a");
        store.RefreshLeaseCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RefreshLease_DoesNotReadSnapshot()
    {
        var store = new FakeConnectStateStore
        {
            RefreshLease = (_, _) => PersistenceStatus.Applied
        };

        await new RefreshConnectionLeaseHandler(store).HandleAsync(
            new RefreshConnectionLeaseCommand(Guid.NewGuid(), "connection-a"));

        store.ReadSnapshotCalls.ShouldBe(0);
        store.ReadPresenceCalls.ShouldBe(0);
    }

    [Fact]
    public async Task RefreshLease_MapsConnectionNotFound()
    {
        var store = new FakeConnectStateStore
        {
            RefreshLease = (_, _) => PersistenceStatus.ConnectionNotFound
        };

        ConnectApplicationResult result = await new RefreshConnectionLeaseHandler(store)
            .HandleAsync(new RefreshConnectionLeaseCommand(Guid.NewGuid(), "missing"));

        result.Status.ShouldBe(ConnectCommandStatus.ConnectionNotFound);
    }
}
