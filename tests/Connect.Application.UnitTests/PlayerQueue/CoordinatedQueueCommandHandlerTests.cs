using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.PlayerQueue;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Player;
using Connect.Domain.Queue;
using Connect.Domain.Synchronization;

namespace Connect.Application.UnitTests.PlayerQueue;

public sealed class CoordinatedQueueCommandHandlerTests
{
    [Fact]
    public async Task RemoveQueueItem_RemovesOnlyMatchingIdentity()
    {
        var trackId = Guid.NewGuid();
        var queue = new QueueState();
        QueueItem removed = queue.Add(trackId, Guid.NewGuid());
        QueueItem remaining = queue.Add(trackId, Guid.NewGuid());
        FakeConnectStateStore store = SnapshotStore(new PlayerState(TestStates.Time), queue);
        store.CommitQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await RemoveHandler(store).HandleAsync(
            new RemoveQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                removed.QueueItemId,
                TestStates.Time));

        result.Queue!.Items.Single().QueueItemId.ShouldBe(remaining.QueueItemId);
        store.CommitQueueCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveQueueItem_UnknownItem_RecordsNoOp()
    {
        FakeConnectStateStore store = SnapshotStore(
            new PlayerState(TestStates.Time),
            new QueueState());
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await RemoveHandler(store).HandleAsync(
            new RemoveQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
    }

    [Fact]
    public async Task SelectQueueItem_CommitsPlayerAndQueueAtomically()
    {
        QueueState queue = QueueWithItems(2);
        queue.Select(queue.Items[0].QueueItemId);
        PlayerState player = TestStates.PlayingPlayer();
        FakeConnectStateStore store = SnapshotStore(player, queue);
        store.CommitPlayerQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await SelectHandler(store).HandleAsync(
            new SelectQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                queue.Items[1].QueueItemId,
                TestStates.Time.AddSeconds(2)));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.CommitPlayerQueueCalls.ShouldBe(1);
        store.CommitPlayerCalls.ShouldBe(0);
        store.CommitQueueCalls.ShouldBe(0);
    }

    [Fact]
    public async Task SelectQueueItem_MissingItem_ReturnsQueueItemNotFound()
    {
        FakeConnectStateStore store = SnapshotStore(
            new PlayerState(TestStates.Time),
            new QueueState());

        ConnectApplicationResult result = await SelectHandler(store).HandleAsync(
            new SelectQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.QueueItemNotFound);
    }

    [Fact]
    public async Task SelectQueueItem_OnConflict_RereadsFreshSnapshot()
    {
        QueueState firstQueue = QueueWithItems(2);
        Guid selectedId = firstQueue.Items[1].QueueItemId;
        var secondQueue = QueueState.Restore(
            firstQueue.Items,
            firstQueue.Items[0].QueueItemId,
            RepeatMode.None,
            false,
            8);
        ConnectSnapshotState[] snapshots =
        [
            new(TestStates.PlayingPlayer(), firstQueue, new()),
            new(TestStates.PlayingPlayer(), secondQueue, new())
        ];
        var store = new FakeConnectStateStore();
        store.ReadSnapshot = (_, _) => new(
            PersistenceStatus.Success,
            snapshots[store.ReadSnapshotCalls - 1],
            null);
        store.CommitPlayerQueue = _ => TestResults.Commit(
            store.CommitPlayerQueueCalls == 1
                ? PersistenceStatus.VersionConflict
                : PersistenceStatus.Applied);

        ConnectApplicationResult result = await SelectHandler(store).HandleAsync(
            new SelectQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                selectedId,
                TestStates.Time.AddSeconds(1)));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.ReadSnapshotCalls.ShouldBe(2);
        result.Queue!.Version.ShouldBe(9);
    }

    [Fact]
    public async Task NextQueueItem_UsesDomainCoordinatorAndCommitsAtomically()
    {
        QueueState queue = QueueWithItems(2);
        queue.Select(queue.Items[0].QueueItemId);
        FakeConnectStateStore store = SnapshotStore(TestStates.PlayingPlayer(), queue);
        store.CommitPlayerQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new NextQueueItemHandler(
            store,
            new ConnectStateCoordinator()).HandleAsync(
                new NextQueueItemCommand(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    TestStates.Time.AddSeconds(2)));

        result.Queue!.CurrentQueueItemId.ShouldBe(queue.Items[1].QueueItemId);
        result.Player!.PositionMs.ShouldBe(0);
        store.CommitPlayerQueueCalls.ShouldBe(1);
    }

    [Fact]
    public async Task PreviousQueueItem_UsesDomainCoordinator()
    {
        QueueState queue = QueueWithItems(2);
        Guid firstId = queue.Items[0].QueueItemId;
        queue.Select(queue.Items[1].QueueItemId);
        FakeConnectStateStore store = SnapshotStore(new PlayerState(TestStates.Time), queue);
        store.CommitQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new PreviousQueueItemHandler(
            store,
            new ConnectStateCoordinator()).HandleAsync(
                new PreviousQueueItemCommand(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    TestStates.Time));

        result.Queue!.CurrentQueueItemId.ShouldBe(firstId);
        store.CommitQueueCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RetryExhaustion_ReturnsConflict()
    {
        var store = new FakeConnectStateStore
        {
            ReadPlayer = (_, _) => TestResults.Player(new PlayerState(TestStates.Time)),
            CommitPlayer = _ => TestResults.Commit(PersistenceStatus.VersionConflict)
        };

        ConnectApplicationResult result = await new PlayHandler(store).HandleAsync(
            new PlayCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Conflict);
        store.ReadPlayerCalls.ShouldBe(3);
    }

    [Fact]
    public async Task CommandCollision_IsMappedCorrectly()
    {
        var store = new FakeConnectStateStore
        {
            ReadPlayer = (_, _) => TestResults.Player(new PlayerState(TestStates.Time)),
            CommitPlayer = _ => TestResults.Commit(PersistenceStatus.CommandCollision)
        };

        ConnectApplicationResult result = await new PlayHandler(store).HandleAsync(
            new PlayCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.CommandCollision);
    }

    [Fact]
    public async Task Cancellation_IsPropagated()
    {
        var store = new FakeConnectStateStore();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            new PlayHandler(store).HandleAsync(
                new PlayCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time),
                source.Token));
    }

    private static FakeConnectStateStore SnapshotStore(
        PlayerState player,
        QueueState queue) =>
        new()
        {
            ReadSnapshot = (_, _) => TestResults.Snapshot(player, queue)
        };

    private static RemoveQueueItemHandler RemoveHandler(FakeConnectStateStore store) =>
        new(store, new ConnectStateCoordinator());

    private static SelectQueueItemHandler SelectHandler(FakeConnectStateStore store) =>
        new(store, new ConnectStateCoordinator());

    private static QueueState QueueWithItems(int count)
    {
        var queue = new QueueState();
        for (int index = 0; index < count; index++)
        {
            queue.Add(Guid.NewGuid(), Guid.NewGuid());
        }

        return queue;
    }
}
