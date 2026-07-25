using Connect.Application.Abstractions.Persistence;
using Connect.Application.Results;
using Connect.Application.Snapshots;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Player;
using Connect.Domain.Queue;

namespace Connect.Application.UnitTests.Snapshots;

public sealed class ConnectSnapshotReaderTests
{
    [Fact]
    public async Task GetAsync_ReturnsCoherentSnapshotWithServerTime()
    {
        var store = new FakeConnectStateStore
        {
            ReadSnapshot = (_, serverTime) => TestResults.Snapshot(
                new PlayerState(serverTime),
                new QueueState())
        };
        DateTimeOffset serverTime = TestStates.Time;

        ConnectApplicationResult result = await new ConnectSnapshotReader(store).GetAsync(
            Guid.NewGuid(),
            serverTime);

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Snapshot!.ServerTime.ShouldBe(serverTime);
        store.ReadSnapshotCalls.ShouldBe(1);
    }

    [Fact]
    public async Task GetAsync_Unavailable_DoesNotExposeState()
    {
        var store = new FakeConnectStateStore
        {
            ReadSnapshot = (_, _) => new(
                PersistenceStatus.Unavailable,
                null,
                "redis details")
        };

        ConnectApplicationResult result = await new ConnectSnapshotReader(store).GetAsync(
            Guid.NewGuid(),
            TestStates.Time);

        result.Status.ShouldBe(ConnectCommandStatus.Unavailable);
        result.Snapshot.ShouldBeNull();
    }
}
