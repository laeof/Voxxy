using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.PlayerQueue;
using Connect.Application.Results;
using Connect.Domain.Synchronization;
using Connect.Infrastructure.Redis;
using Shouldly;

namespace Connect.Infrastructure.IntegrationTests;

[Collection(RedisCollection.Name)]
public sealed class ApplicationPlayerQueueTests(RedisFixture fixture)
{
    private static readonly DateTimeOffset Start =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddQueueItem_DuplicateReturnsSameQueueItemId()
    {
        var handler = new AddQueueItemHandler(fixture.CreateStore());
        var command = new AddQueueItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Start);

        ConnectApplicationResult first = await handler.HandleAsync(command);
        ConnectApplicationResult duplicate = await handler.HandleAsync(command);

        first.Status.ShouldBe(ConnectCommandStatus.Applied);
        duplicate.Status.ShouldBe(ConnectCommandStatus.Duplicate);
        duplicate.Outcome!.QueueItemId.ShouldBe(first.Outcome!.QueueItemId);
    }

    [Fact]
    public async Task ConcurrentVolumeCommands_RetryToSuccessOrConflict()
    {
        var userId = Guid.NewGuid();
        RedisConnectStateStore store = fixture.CreateStore();
        var firstHandler = new ChangeVolumeHandler(store);
        var secondHandler = new ChangeVolumeHandler(store);

        ConnectApplicationResult[] results = await Task.WhenAll(
            firstHandler.HandleAsync(
                new ChangeVolumeCommand(userId, Guid.NewGuid(), 20, Start)),
            secondHandler.HandleAsync(
                new ChangeVolumeCommand(userId, Guid.NewGuid(), 80, Start)));

        results.Count(result => result.Status == ConnectCommandStatus.Applied)
            .ShouldBeGreaterThanOrEqualTo(1);
        results.All(result =>
                result.Status is
                    ConnectCommandStatus.Applied or ConnectCommandStatus.Conflict)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task SelectQueueItem_WritesPlayerAndQueueAtomically()
    {
        var userId = Guid.NewGuid();
        RedisConnectStateStore store = fixture.CreateStore();
        var add = new AddQueueItemHandler(store);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await add.HandleAsync(
            new AddQueueItemCommand(
                userId,
                Guid.NewGuid(),
                firstId,
                Guid.NewGuid(),
                Start));
        await add.HandleAsync(
            new AddQueueItemCommand(
                userId,
                Guid.NewGuid(),
                secondId,
                Guid.NewGuid(),
                Start));
        await new SelectQueueItemHandler(store, new ConnectStateCoordinator()).HandleAsync(
            new SelectQueueItemCommand(userId, Guid.NewGuid(), firstId, Start));
        await new ChangePositionHandler(store).HandleAsync(
            new ChangePositionCommand(userId, Guid.NewGuid(), 5_000, Start));

        ConnectApplicationResult result = await new SelectQueueItemHandler(
            store,
            new ConnectStateCoordinator()).HandleAsync(
                new SelectQueueItemCommand(
                    userId,
                    Guid.NewGuid(),
                    secondId,
                    Start.AddSeconds(1)));
        ConnectSnapshotReadResult persisted = await store.ReadSnapshotAsync(userId, Start);

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        persisted.Snapshot!.Queue.CurrentQueueItemId.ShouldBe(secondId);
        persisted.Snapshot.Player.PositionMs.ShouldBe(0);
        persisted.Snapshot.Player.Version.ShouldBe(result.Player!.Version);
        persisted.Snapshot.Queue.Version.ShouldBe(result.Queue!.Version);
    }

    [Fact]
    public async Task ShuffleDuplicate_DoesNotProduceDifferentOrder()
    {
        var userId = Guid.NewGuid();
        RedisConnectStateStore store = fixture.CreateStore();
        var add = new AddQueueItemHandler(store);
        for (int index = 0; index < 5; index++)
        {
            await add.HandleAsync(
                new AddQueueItemCommand(
                    userId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Start));
        }

        var handler = new ShuffleQueueHandler(store);
        var command = new ShuffleQueueCommand(userId, Guid.NewGuid(), 1234, Start);
        ConnectApplicationResult first = await handler.HandleAsync(command);
        Guid[] firstOrder =
        [
            .. first.Queue!.Items.Select(item => item.QueueItemId)
        ];
        ConnectApplicationResult duplicate = await handler.HandleAsync(command);
        PersistenceReadResult<Connect.Domain.Queue.QueueState> persisted =
            await store.ReadQueueAsync(userId);

        duplicate.Status.ShouldBe(ConnectCommandStatus.Duplicate);
        persisted.State!.Items.Select(item => item.QueueItemId).ShouldBe(firstOrder);
    }

    [Fact]
    public async Task NoOpCommand_IsDeduplicated()
    {
        var handler = new ChangeVolumeHandler(fixture.CreateStore());
        var command = new ChangeVolumeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Connect.Domain.Player.PlayerState.DefaultVolumePercent,
            Start);

        ConnectApplicationResult first = await handler.HandleAsync(command);
        ConnectApplicationResult duplicate = await handler.HandleAsync(command);

        first.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        duplicate.Status.ShouldBe(ConnectCommandStatus.Duplicate);
    }
}
