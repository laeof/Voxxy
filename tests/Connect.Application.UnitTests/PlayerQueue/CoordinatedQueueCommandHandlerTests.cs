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
    public async Task CompleteCurrentTrack_CommitsPlayerAndQueueAtomically()
    {
        QueueState queue = QueueWithItems(2);
        queue.Select(queue.Items[0].QueueItemId);
        FakeConnectStateStore store = SnapshotStore(TestStates.PlayingPlayer(), queue);
        store.CommitPlayerQueue = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await CompleteHandler(store).HandleAsync(
            new CompleteCurrentTrackCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                queue.Items[0].QueueItemId,
                180_000,
                TestStates.Time.AddMinutes(3)));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Queue!.CurrentQueueItemId.ShouldBe(queue.Items[1].QueueItemId);
        result.Player!.PositionMs.ShouldBe(0);
        store.CommitPlayerQueueCalls.ShouldBe(1);
    }

    [Fact]
    public async Task CompleteCurrentTrack_RetriesCasConflict()
    {
        QueueState first = QueueWithItems(2);
        first.Select(first.Items[0].QueueItemId);
        Guid expectedId = first.Items[0].QueueItemId;
        var second = QueueState.Restore(
            first.Items,
            expectedId,
            RepeatMode.None,
            false,
            first.Version + 1);
        var store = new FakeConnectStateStore();
        store.ReadSnapshot = (_, _) => TestResults.Snapshot(
            TestStates.PlayingPlayer(),
            store.ReadSnapshotCalls == 1 ? first : second);
        store.CommitPlayerQueue = _ => TestResults.Commit(
            store.CommitPlayerQueueCalls == 1
                ? PersistenceStatus.VersionConflict
                : PersistenceStatus.Applied);

        ConnectApplicationResult result = await CompleteHandler(store).HandleAsync(
            new CompleteCurrentTrackCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                expectedId,
                180_000,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.ReadSnapshotCalls.ShouldBe(2);
        store.CommitPlayerQueueCalls.ShouldBe(2);
    }

    [Fact]
    public async Task CompleteCurrentTrack_DuplicateCommandIsReturned()
    {
        QueueState queue = QueueWithItems(2);
        queue.Select(queue.Items[0].QueueItemId);
        FakeConnectStateStore store = SnapshotStore(TestStates.PlayingPlayer(), queue);
        store.CommitPlayerQueue = _ => TestResults.Commit(PersistenceStatus.Duplicate);

        ConnectApplicationResult result = await CompleteHandler(store).HandleAsync(
            new CompleteCurrentTrackCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                queue.Items[0].QueueItemId,
                180_000,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Duplicate);
        store.CommitPlayerQueueCalls.ShouldBe(1);
    }

    [Fact]
    public async Task StartPlaybackContext_CommitsReplacementAtomically()
    {
        FakeConnectStateStore store = SnapshotStore(
            new PlayerState(TestStates.Time),
            new QueueState());
        store.CommitPlayerQueue = _ => TestResults.Commit(PersistenceStatus.Applied);
        var sourceId = Guid.NewGuid();
        PlaybackContextItem[] items =
        [
            new(Guid.NewGuid(), Guid.NewGuid()),
            new(Guid.NewGuid(), Guid.NewGuid())
        ];

        ConnectApplicationResult result = await StartContextHandler(store).HandleAsync(
            new StartPlaybackContextCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                sourceId,
                PlaybackSourceType.Album,
                items,
                1,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Queue!.SourceId.ShouldBe(sourceId);
        result.Queue.SourceType.ShouldBe(Connect.Contracts.States.PlaybackSourceTypeDto.Album);
        result.Queue.CurrentQueueItemId.ShouldBe(items[1].QueueItemId);
        result.Player!.IsPlaying.ShouldBeTrue();
        store.CommitPlayerQueueCalls.ShouldBe(1);
    }

    [Fact]
    public async Task StartPlaybackContext_SamePlayingContext_RecordsNoChanges()
    {
        var sourceId = Guid.NewGuid();
        QueueState queue = QueueWithItems(2);
        queue = QueueState.Restore(
            queue.Items,
            queue.Items[0].QueueItemId,
            RepeatMode.None,
            false,
            queue.Version,
            sourceId,
            PlaybackSourceType.Playlist);
        FakeConnectStateStore store = SnapshotStore(TestStates.PlayingPlayer(), queue);
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await StartContextHandler(store).HandleAsync(
            ContextCommand(sourceId, queue));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
        store.CommitPlayerQueueCalls.ShouldBe(0);
    }

    [Fact]
    public async Task StartPlaybackContext_OnConflict_RereadsFreshSnapshot()
    {
        var sourceId = Guid.NewGuid();
        QueueState first = QueueWithItems(1);
        QueueState second = QueueWithItems(1);
        var store = new FakeConnectStateStore();
        store.ReadSnapshot = (_, _) => TestResults.Snapshot(
            new PlayerState(TestStates.Time),
            store.ReadSnapshotCalls == 1 ? first : second);
        store.CommitPlayerQueue = _ => TestResults.Commit(
            store.CommitPlayerQueueCalls == 1
                ? PersistenceStatus.VersionConflict
                : PersistenceStatus.Applied);
        StartPlaybackContextCommand command = ContextCommand(sourceId, first);

        ConnectApplicationResult result = await StartContextHandler(store).HandleAsync(command);

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.ReadSnapshotCalls.ShouldBe(2);
        store.CommitPlayerQueueCalls.ShouldBe(2);
    }

    [Fact]
    public async Task StartPlaybackContext_EmptySource_ReturnsValidationFailure()
    {
        FakeConnectStateStore store = SnapshotStore(
            new PlayerState(TestStates.Time),
            new QueueState());

        ConnectApplicationResult result = await StartContextHandler(store).HandleAsync(
            new StartPlaybackContextCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                PlaybackSourceType.Playlist,
                [],
                null,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.ValidationFailed);
        store.ReadSnapshotCalls.ShouldBe(0);
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

    private static CompleteCurrentTrackHandler CompleteHandler(FakeConnectStateStore store) =>
        new(store, new ConnectStateCoordinator());

    private static StartPlaybackContextHandler StartContextHandler(
        FakeConnectStateStore store) =>
        new(store, new ConnectStateCoordinator());

    private static StartPlaybackContextCommand ContextCommand(
        Guid sourceId,
        QueueState queue) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            sourceId,
            PlaybackSourceType.Playlist,
            queue.Items.Select(item => new PlaybackContextItem(
                    item.QueueItemId,
                    item.TrackId))
                .ToArray(),
            null,
            TestStates.Time);

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
