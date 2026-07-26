using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.PlayerQueue;
using Connect.Application.Results;
using Connect.Application.UnitTests.Fakes;
using Connect.Domain.Player;

namespace Connect.Application.UnitTests.PlayerQueue;

public sealed class PlayerCommandHandlerTests
{
    [Fact]
    public async Task Play_CommitsPlayer()
    {
        FakeConnectStateStore store = PlayerStore(new PlayerState(TestStates.Time));
        store.CommitPlayer = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new PlayHandler(store).HandleAsync(
            new PlayCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Player!.IsPlaying.ShouldBeTrue();
        store.CommitPlayerCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Play_WhenAlreadyPlaying_RecordsNoOp()
    {
        PlayerState player = TestStates.PlayingPlayer();
        FakeConnectStateStore store = PlayerStore(player);
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new PlayHandler(store).HandleAsync(
            new PlayCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
        store.CommitPlayerCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Play_OnConflict_RereadsFreshPlayer()
    {
        PlayerState[] states =
        [
            new(TestStates.Time),
            new(TestStates.Time)
        ];
        var store = new FakeConnectStateStore();
        store.ReadPlayer = (_, _) => TestResults.Player(states[store.ReadPlayerCalls - 1]);
        store.CommitPlayer = _ => TestResults.Commit(
            store.CommitPlayerCalls == 1
                ? PersistenceStatus.VersionConflict
                : PersistenceStatus.Applied);

        ConnectApplicationResult result = await new PlayHandler(store).HandleAsync(
            new PlayCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        store.ReadPlayerCalls.ShouldBe(2);
        states.All(state => state.Version == 1).ShouldBeTrue();
    }

    [Fact]
    public async Task Pause_CommitsAnchoredPosition()
    {
        PlayerState player = TestStates.PlayingPlayer();
        FakeConnectStateStore store = PlayerStore(player);
        store.CommitPlayer = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new PauseHandler(store).HandleAsync(
            new PauseCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                TestStates.Time.AddSeconds(4)));

        result.Player!.IsPlaying.ShouldBeFalse();
        result.Player.PositionMs.ShouldBe(4_000);
        result.Player.PositionUpdatedAt.ShouldBe(TestStates.Time.AddSeconds(4));
    }

    [Fact]
    public async Task Pause_OnConflict_ReappliesUsingSameServerTime()
    {
        PlayerState[] states =
        [
            TestStates.PlayingPlayer(),
            TestStates.PlayingPlayer()
        ];
        var store = new FakeConnectStateStore();
        store.ReadPlayer = (_, _) => TestResults.Player(states[store.ReadPlayerCalls - 1]);
        store.CommitPlayer = _ => TestResults.Commit(
            store.CommitPlayerCalls == 1
                ? PersistenceStatus.VersionConflict
                : PersistenceStatus.Applied);
        DateTimeOffset serverTime = TestStates.Time.AddSeconds(5);

        ConnectApplicationResult result = await new PauseHandler(store).HandleAsync(
            new PauseCommand(Guid.NewGuid(), Guid.NewGuid(), serverTime));

        result.Player!.PositionMs.ShouldBe(5_000);
        states.Select(state => state.PositionUpdatedAt).ShouldAllBe(value => value == serverTime);
    }

    [Fact]
    public async Task ChangePosition_CommitsPlayer()
    {
        FakeConnectStateStore store = PlayerStore(new PlayerState(TestStates.Time));
        store.CommitPlayer = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new ChangePositionHandler(store).HandleAsync(
            new ChangePositionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                12_345,
                TestStates.Time.AddSeconds(1)));

        result.Player!.PositionMs.ShouldBe(12_345);
        store.CommitPlayerCalls.ShouldBe(1);
    }

    [Fact]
    public async Task ChangePosition_WithNegativeValue_FailsWithoutStoreCall()
    {
        var store = new FakeConnectStateStore();

        ConnectApplicationResult result = await new ChangePositionHandler(store).HandleAsync(
            new ChangePositionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                -1,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.ValidationFailed);
        store.ReadPlayerCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ChangeVolume_CommitsPlayer()
    {
        DateTimeOffset anchorTime = TestStates.Time.AddSeconds(-10);
        var player = PlayerState.Restore(true, 12_000, anchorTime, 50, 4);
        FakeConnectStateStore store = PlayerStore(player);
        store.CommitPlayer = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new ChangeVolumeHandler(store).HandleAsync(
            new ChangeVolumeCommand(Guid.NewGuid(), Guid.NewGuid(), 80, TestStates.Time));

        result.Player!.VolumePercent.ShouldBe(80);
        result.Player.PositionMs.ShouldBe(12_000);
        result.Player.PositionUpdatedAt.ShouldBe(anchorTime);
        result.Player.IsPlaying.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeVolume_WithSameValue_RecordsNoOp()
    {
        FakeConnectStateStore store = PlayerStore(new PlayerState(TestStates.Time));
        store.RecordCommand = _ => TestResults.Commit(PersistenceStatus.Applied);

        ConnectApplicationResult result = await new ChangeVolumeHandler(store).HandleAsync(
            new ChangeVolumeCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                PlayerState.DefaultVolumePercent,
                TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        store.RecordCommandCalls.ShouldBe(1);
    }

    [Fact]
    public async Task ChangeVolume_OnConflict_UsesFreshState()
    {
        PlayerState first = new(TestStates.Time);
        DateTimeOffset freshAnchorTime = TestStates.Time.AddSeconds(2);
        var second = PlayerState.Restore(true, 42_000, freshAnchorTime, 30, 4);
        PlayerState[] states = [first, second];
        var store = new FakeConnectStateStore();
        store.ReadPlayer = (_, _) => TestResults.Player(states[store.ReadPlayerCalls - 1]);
        store.CommitPlayer = _ => TestResults.Commit(
            store.CommitPlayerCalls == 1
                ? PersistenceStatus.VersionConflict
                : PersistenceStatus.Applied);

        ConnectApplicationResult result = await new ChangeVolumeHandler(store).HandleAsync(
            new ChangeVolumeCommand(Guid.NewGuid(), Guid.NewGuid(), 80, TestStates.Time));

        result.Player!.Version.ShouldBe(5);
        result.Player.PositionMs.ShouldBe(42_000);
        result.Player.PositionUpdatedAt.ShouldBe(freshAnchorTime);
        result.Player.IsPlaying.ShouldBeTrue();
        store.ReadPlayerCalls.ShouldBe(2);
    }

    [Fact]
    public async Task DuplicateCommand_DoesNotExposeSpeculativeDomainMutation()
    {
        FakeConnectStateStore store = PlayerStore(new PlayerState(TestStates.Time));
        store.CommitPlayer = _ => TestResults.Commit(
            PersistenceStatus.Duplicate,
            """{"playerVersion":1}""");

        ConnectApplicationResult result = await new PlayHandler(store).HandleAsync(
            new PlayCommand(Guid.NewGuid(), Guid.NewGuid(), TestStates.Time));

        result.Status.ShouldBe(ConnectCommandStatus.Duplicate);
        result.Player!.IsPlaying.ShouldBeFalse();
        result.Outcome!.PlayerVersion.ShouldBe(1);
    }

    private static FakeConnectStateStore PlayerStore(PlayerState player) =>
        new()
        {
            ReadPlayer = (_, _) => TestResults.Player(player)
        };
}
