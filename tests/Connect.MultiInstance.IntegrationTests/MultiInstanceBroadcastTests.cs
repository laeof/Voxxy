using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Presentation.Transport;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;

namespace Connect.MultiInstance.IntegrationTests;

[Collection(MultiInstanceCollection.Name)]
public sealed class MultiInstanceBroadcastTests(MultiInstanceFixture fixture)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BroadcastsFlowInBothDirectionsAcrossInstances()
    {
        var userId = Guid.NewGuid();
        await using HubConnection client1 = CreateClient(fixture.Instance1, userId);
        await using HubConnection client2 = CreateClient(fixture.Instance2, userId);
        TaskCompletionSource<PlayerStateChangedEvent> from1 =
            Completion<PlayerStateChangedEvent>();
        TaskCompletionSource<PresenceStateChangedEvent> from2 =
            Completion<PresenceStateChangedEvent>();
        client2.On<PlayerStateChangedEvent>("PlayerStateChanged", from1.SetResult);
        client1.On<PresenceStateChangedEvent>("PresenceStateChanged", from2.SetResult);
        await client1.StartAsync();
        await client2.StartAsync();

        await PublishUntilReceivedAsync(
            () => fixture.Instance1.Broadcaster.BroadcastAsync(
                userId,
                Guid.NewGuid(),
                Applied(player: Player(1)),
                default),
            from1.Task);
        await PublishUntilReceivedAsync(
            () => fixture.Instance2.Broadcaster.BroadcastAsync(
                userId,
                Guid.NewGuid(),
                Applied(presence: Presence(1)),
                default),
            from2.Task);

        (await from1.Task.WaitAsync(Timeout)).Player.Version.ShouldBe(1);
        (await from2.Task.WaitAsync(Timeout)).Presence.Version.ShouldBe(1);
    }

    [Fact]
    public async Task BroadcastForUserA_DoesNotReachUserB_OnOtherInstance()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await using HubConnection clientB = CreateClient(fixture.Instance2, userB);
        TaskCompletionSource<PlayerStateChangedEvent> received =
            Completion<PlayerStateChangedEvent>();
        clientB.On<PlayerStateChangedEvent>("PlayerStateChanged", received.SetResult);
        await clientB.StartAsync();

        await fixture.Instance1.Broadcaster.BroadcastAsync(
            userA,
            Guid.NewGuid(),
            Applied(player: Player(1)),
            default);

        await Should.ThrowAsync<TimeoutException>(() =>
            received.Task.WaitAsync(TimeSpan.FromMilliseconds(300)));
    }

    [Fact]
    public async Task VolumeCommand_OnInstance1_IsReceivedByClientOnInstance2()
    {
        var userId = Guid.NewGuid();
        await using HubConnection caller = CreateClient(fixture.Instance1, userId);
        await using HubConnection observer = CreateClient(fixture.Instance2, userId);
        TaskCompletionSource<PlayerStateChangedEvent> received =
            Completion<PlayerStateChangedEvent>();
        observer.On<PlayerStateChangedEvent>("PlayerStateChanged", received.SetResult);
        fixture.Instance1.Facade.Result = Applied(player: Player(4));
        await caller.StartAsync();
        await observer.StartAsync();

        ConnectCommandAck ack = await caller.InvokeAsync<ConnectCommandAck>(
            "ChangeVolume",
            new ChangeVolumeRequest(Guid.NewGuid(), 75));

        ack.Status.ShouldBe(ConnectCommandAckStatus.Applied);
        (await received.Task.WaitAsync(Timeout)).Player.Version.ShouldBe(4);
    }

    [Fact]
    public async Task CombinedAndCleanupEventsCrossInstances()
    {
        var userId = Guid.NewGuid();
        await using HubConnection client1 = CreateClient(fixture.Instance1, userId);
        await using HubConnection client2 = CreateClient(fixture.Instance2, userId);
        TaskCompletionSource<PlayerQueueStateChangedEvent> playerQueue =
            Completion<PlayerQueueStateChangedEvent>();
        TaskCompletionSource<PlayerPresenceStateChangedEvent> playerPresence =
            Completion<PlayerPresenceStateChangedEvent>();
        client2.On<PlayerQueueStateChangedEvent>(
            "PlayerQueueStateChanged",
            playerQueue.SetResult);
        client1.On<PlayerPresenceStateChangedEvent>(
            "PlayerPresenceStateChanged",
            playerPresence.SetResult);
        await client1.StartAsync();
        await client2.StartAsync();

        await fixture.Instance1.Broadcaster.BroadcastAsync(
            userId,
            Guid.NewGuid(),
            Applied(player: Player(2), queue: Queue(3)),
            default);
        await fixture.Instance2.Broadcaster.BroadcastAsync(
            userId,
            Guid.NewGuid(),
            Applied(player: Player(4), presence: Presence(5)),
            default);

        (await playerQueue.Task.WaitAsync(Timeout)).Queue.Version.ShouldBe(3);
        (await playerPresence.Task.WaitAsync(Timeout)).Presence.Version.ShouldBe(5);
    }

    [Fact]
    public async Task DuplicateCommand_DoesNotProduceSecondCrossInstanceBroadcast()
    {
        var userId = Guid.NewGuid();
        await using HubConnection caller = CreateClient(fixture.Instance1, userId);
        await using HubConnection observer = CreateClient(fixture.Instance2, userId);
        int events = 0;
        TaskCompletionSource<PlayerStateChangedEvent> first =
            Completion<PlayerStateChangedEvent>();
        TaskCompletionSource<PlayerStateChangedEvent> second =
            Completion<PlayerStateChangedEvent>();
        observer.On<PlayerStateChangedEvent>("PlayerStateChanged", message =>
        {
            if (Interlocked.Increment(ref events) == 1)
            {
                first.SetResult(message);
            }
            else
            {
                second.TrySetResult(message);
            }
        });
        fixture.Instance1.Facade.Result = Applied(player: Player(1));
        await caller.StartAsync();
        await observer.StartAsync();
        var request = new CommandRequest(Guid.NewGuid());
        await caller.InvokeAsync<ConnectCommandAck>("Play", request);
        await first.Task.WaitAsync(Timeout);

        fixture.Instance1.Facade.Result =
            new ConnectApplicationResult(ConnectCommandStatus.Duplicate);
        await caller.InvokeAsync<ConnectCommandAck>("Play", request);
        await Should.ThrowAsync<TimeoutException>(() =>
            second.Task.WaitAsync(TimeSpan.FromMilliseconds(300)));

        events.ShouldBe(1);
    }

    [Fact]
    public async Task ClientReconnectsToDifferentInstance_AndSnapshotRestoresState()
    {
        var userId = Guid.NewGuid();
        await using HubConnection first = CreateClient(fixture.Instance1, userId);
        await first.StartAsync();
        await first.StopAsync();
        fixture.Instance2.Facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Snapshot: new ConnectSnapshot(
                userId,
                Player(7),
                Queue(8),
                Presence(9),
                Now));
        await using HubConnection reconnected = CreateClient(fixture.Instance2, userId);
        await reconnected.StartAsync();

        ConnectSnapshotResponse snapshot =
            await reconnected.InvokeAsync<ConnectSnapshotResponse>("GetSnapshot");

        snapshot.Player!.Version.ShouldBe(7);
        snapshot.Queue!.Version.ShouldBe(8);
        snapshot.Presence!.Version.ShouldBe(9);
    }

    private static HubConnection CreateClient(TestConnectHost host, Guid userId) =>
        new HubConnectionBuilder()
            .WithUrl(
                new Uri(host.BaseAddress, "/api/hubs/connect"),
                options => options.AccessTokenProvider =
                    () => Task.FromResult<string?>(userId.ToString("D")))
            .Build();

    private static TaskCompletionSource<T> Completion<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task PublishUntilReceivedAsync<T>(
        Func<Task> publish,
        Task<T> received)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(25));
        while (!received.IsCompleted)
        {
            await publish();
            if (received.IsCompleted)
            {
                break;
            }
            await timer.WaitForNextTickAsync(timeout.Token);
        }
        await received.WaitAsync(timeout.Token);
    }

    private static ConnectApplicationResult Applied(
        PlayerStateDto? player = null,
        QueueStateDto? queue = null,
        PresenceStateDto? presence = null) =>
        new(
            ConnectCommandStatus.Applied,
            Player: player,
            Queue: queue,
            Presence: presence,
            Outcome: new ConnectCommandOutcome(
                PlayerVersion: player?.Version,
                QueueVersion: queue?.Version,
                PresenceVersion: presence?.Version));

    private static PlayerStateDto Player(long version) =>
        new(false, 0, Now, 50, version);

    private static QueueStateDto Queue(long version) =>
        new([], null, RepeatModeDto.None, false, version);

    private static PresenceStateDto Presence(long version) =>
        new([], null, null, version);
}
