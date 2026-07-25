using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.PlayerQueue;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Queue;

namespace Connect.Application.UnitTests.PlayerQueue;

public sealed class QueueCommandHandlerTests
{
    [Fact]
    public async Task AddQueueItem_CommitsQueueAndReturnsStableQueueItemId()
    {
        var queueItemId = Guid.NewGuid();
        FakeConnectStateStore store = QueueStore(new QueueState());
        store.CommitQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new AddQueueItemHandler(store).HandleAsync(
            AddCommand(queueItemId));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Outcome!.QueueItemId.ShouldBe(queueItemId);
        result.Queue!.Items.Single().QueueItemId.ShouldBe(queueItemId);
    }

    [Fact]
    public async Task AddQueueItem_Duplicate_ReturnsOriginalQueueItemId()
    {
        var originalId = Guid.NewGuid();
        FakeConnectStateStore store = QueueStore(new QueueState());
        store.CommitQueue = _ => TestResults.Commit(
            PersistenceStatus.Duplicate,
            $$"""{"queueItemId":"{{originalId:D}}","queueVersion":1}""");

        ConnectApplicationResult result = await new AddQueueItemHandler(store).HandleAsync(
            AddCommand(Guid.NewGuid()));

        result.Status.ShouldBe(ConnectCommandStatus.Duplicate);
        result.Outcome!.QueueItemId.ShouldBe(originalId);
    }

    [Fact]
    public async Task AddQueueItem_OnConflict_ReusesSameQueueItemId()
    {
        var queueItemId = Guid.NewGuid();
        QueueState[] queues = [new(), new()];
        var committedIds = new List<Guid>();
        var store = new FakeConnectStateStore();
        store.ReadQueue = _ => TestResults.Queue(queues[store.ReadQueueCalls - 1]);
        store.CommitQueue = commit =>
        {
            committedIds.Add(commit.State.Items.Single().QueueItemId);
            return TestResults.Commit(
                store.CommitQueueCalls == 1
                    ? PersistenceStatus.VersionConflict
                    : PersistenceStatus.Applied);
        };

        await new AddQueueItemHandler(store).HandleAsync(AddCommand(queueItemId));

        committedIds.ShouldBe([queueItemId, queueItemId]);
    }

    [Fact]
    public async Task MoveQueueItem_CommitsQueue()
    {
        QueueState queue = QueueWithItems(3);
        Guid movedId = queue.Items[0].QueueItemId;
        FakeConnectStateStore store = QueueStore(queue);
        store.CommitQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new MoveQueueItemHandler(store).HandleAsync(
            new MoveQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                movedId,
                2,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Queue!.Items[2].QueueItemId.ShouldBe(movedId);
    }

    [Fact]
    public async Task MoveQueueItem_InvalidIndex_ReturnsDomainFailure()
    {
        QueueState queue = QueueWithItems(1);
        FakeConnectStateStore store = QueueStore(queue);

        ConnectApplicationResult result = await new MoveQueueItemHandler(store).HandleAsync(
            new MoveQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                queue.Items[0].QueueItemId,
                4,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.InvalidQueueIndex);
        store.CommitQueueCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MoveQueueItem_NoMovement_RecordsNoOp()
    {
        QueueState queue = QueueWithItems(1);
        FakeConnectStateStore store = QueueStore(queue);
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new MoveQueueItemHandler(store).HandleAsync(
            new MoveQueueItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                queue.Items[0].QueueItemId,
                0,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Shuffle_OnConflict_ReusesSameOrder()
    {
        QueueState[] queues = [QueueWithItems(5), QueueWithItems(5)];
        Guid[] ids = queues[0].Items.Select(item => item.QueueItemId).ToArray();
        queues[1] = QueueState.Restore(
            ids.Select((id, index) => new QueueItem(id, Guid.NewGuid(), index)),
            null,
            RepeatMode.None,
            false,
            0);
        var orders = new List<Guid[]>();
        var store = new FakeConnectStateStore();
        store.ReadQueue = _ => TestResults.Queue(queues[store.ReadQueueCalls - 1]);
        store.CommitQueue = commit =>
        {
            orders.Add(commit.State.Items.Select(item => item.QueueItemId).ToArray());
            return TestResults.Commit(
                store.CommitQueueCalls == 1
                    ? PersistenceStatus.VersionConflict
                    : PersistenceStatus.Applied);
        };

        await new ShuffleQueueHandler(store).HandleAsync(
            new ShuffleQueueCommand(Guid.NewGuid(), Guid.NewGuid(), 42, TestStates.Time));

        orders.Count.ShouldBe(2);
        orders[1].ShouldBe(orders[0]);
    }

    [Fact]
    public async Task Shuffle_PreservesCurrentQueueItemIdentity()
    {
        QueueState queue = QueueWithItems(4);
        Guid currentId = queue.Items[2].QueueItemId;
        queue.Select(currentId);
        FakeConnectStateStore store = QueueStore(queue);
        store.CommitQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new ShuffleQueueHandler(store).HandleAsync(
            new ShuffleQueueCommand(Guid.NewGuid(), Guid.NewGuid(), 77, TestStates.Time));

        result.Queue!.CurrentQueueItemId.ShouldBe(currentId);
    }

    [Fact]
    public async Task Unshuffle_RestoresCanonicalOrder()
    {
        QueueState queue = QueueWithItems(3);
        queue.Shuffle(queue.Items.Reverse().Select(item => item.QueueItemId).ToArray());
        FakeConnectStateStore store = QueueStore(queue);
        store.CommitQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new UnshuffleQueueHandler(store).HandleAsync(
            new UnshuffleQueueCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Queue!.IsShuffled.ShouldBeFalse();
        result.Queue.Items.Select(item => item.CanonicalOrder).ShouldBe([0, 1, 2]);
    }

    [Fact]
    public async Task ChangeRepeatMode_CommitsQueue()
    {
        FakeConnectStateStore store = QueueStore(new QueueState());
        store.CommitQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new ChangeRepeatModeHandler(store).HandleAsync(
            new ChangeRepeatModeCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                RepeatMode.Queue,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Queue!.RepeatMode.ShouldBe(Connect.Contracts.States.RepeatModeDto.Queue);
    }

    [Fact]
    public async Task ChangeRepeatMode_WithSameMode_RecordsNoOp()
    {
        FakeConnectStateStore store = QueueStore(new QueueState());
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new ChangeRepeatModeHandler(store).HandleAsync(
            new ChangeRepeatModeCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                RepeatMode.None,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
    }

    [Fact]
    public async Task ChangeRepeatMode_InvalidValue_FailsWithoutStoreCall()
    {
        var store = new FakeConnectStateStore();

        ConnectApplicationResult result = await new ChangeRepeatModeHandler(store).HandleAsync(
            new ChangeRepeatModeCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                (RepeatMode)999,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.ValidationFailed);
        store.ReadQueueCalls.ShouldBe(0);
    }

    private static AddQueueItemCommand AddCommand(Guid queueItemId) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            queueItemId,
            Guid.NewGuid(),
            TestStates.Time);

    private static FakeConnectStateStore QueueStore(QueueState queue) =>
        new()
        {
            ReadQueue = _ => TestResults.Queue(queue)
        };

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
