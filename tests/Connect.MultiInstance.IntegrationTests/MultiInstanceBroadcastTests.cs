using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Presentation.Transport;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;
using Xunit.Abstractions;

namespace Connect.MultiInstance.IntegrationTests;

[Collection(MultiInstanceCollection.Name)]
public sealed class MultiInstanceBroadcastTests(
    MultiInstanceFixture fixture,
    ITestOutputHelper output)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CiLoadProfile_ConnectsRegistersSnapshotsAndHeartbeats()
    {
        const int connectionCount = 20;
        HubConnection[] clients = Enumerable.Range(0, connectionCount)
            .Select(index => CreateClient(
                index % 2 == 0 ? fixture.Instance1 : fixture.Instance2,
                Guid.NewGuid()))
            .ToArray();
        try
        {
            double[] connectLatency = await MeasureAllAsync(
                clients.Select(client => (Func<Task>)(() => client.StartAsync())));
            fixture.Instance1.Facade.Result = new ConnectApplicationResult(
                ConnectCommandStatus.Applied,
                Snapshot: new ConnectSnapshot(
                    Guid.NewGuid(),
                    Player(1),
                    Queue(1),
                    Presence(1),
                    Now));
            fixture.Instance2.Facade.Result = fixture.Instance1.Facade.Result;

            double[] registrationLatency = await MeasureAllAsync(
                clients.Select((client, index) => (Func<Task>)(async () =>
                    await client.InvokeAsync<ConnectCommandAck>(
                    "RegisterConnection",
                    new RegisterConnectionRequest(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        $"CI device {index}")))));
            double[] snapshotLatency = await MeasureAllAsync(
                clients.Select(client => (Func<Task>)(async () =>
                    await client.InvokeAsync<ConnectSnapshotResponse>("GetSnapshot"))));
            double[] heartbeatLatency = await MeasureAllAsync(
                clients.Select(client => (Func<Task>)(async () =>
                    await client.InvokeAsync<ConnectCommandAck>("RefreshConnectionLease"))));

            output.WriteLine(
                "CI_LOAD connect={0} register={1} snapshot={2} heartbeat={3}",
                Summary(connectLatency),
                Summary(registrationLatency),
                Summary(snapshotLatency),
                Summary(heartbeatLatency));
            connectLatency.Length.ShouldBe(connectionCount);
            registrationLatency.Length.ShouldBe(connectionCount);
            snapshotLatency.Length.ShouldBe(connectionCount);
            heartbeatLatency.Length.ShouldBe(connectionCount);
        }
        finally
        {
            await Task.WhenAll(clients.Select(client => client.DisposeAsync().AsTask()));
        }
    }

    private static async Task<double[]> MeasureAllAsync(IEnumerable<Func<Task>> operations) =>
        await Task.WhenAll(operations.Select(async operation =>
        {
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            await operation();
            return System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }));

    private static string Summary(double[] values)
    {
        Array.Sort(values);
        double At(double percentile) =>
            values[Math.Min(values.Length - 1, (int)Math.Ceiling(values.Length * percentile) - 1)];
        return $"p50={At(0.50):F1}ms,p95={At(0.95):F1}ms,p99={At(0.99):F1}ms,max={values[^1]:F1}ms";
    }

    [Fact]
    public async Task UnauthenticatedHubConnection_IsRejected()
    {
        await using HubConnection client = new HubConnectionBuilder()
            .WithUrl(
                new Uri(fixture.Instance1.BaseAddress, "/api/hubs/connect"),
                options =>
                {
                    options.Transports =
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                })
            .Build();

        await Should.ThrowAsync<Exception>(() => client.StartAsync());
    }

    [Fact]
    public async Task UserIdentityForSnapshot_ComesOnlyFromAuthenticatedClaims()
    {
        var userId = Guid.NewGuid();
        fixture.Instance1.Facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Snapshot: new ConnectSnapshot(
                userId,
                Player(1),
                Queue(1),
                Presence(1),
                Now));
        await using HubConnection client = CreateClient(fixture.Instance1, userId);
        await client.StartAsync();

        await client.InvokeAsync<ConnectSnapshotResponse>("GetSnapshot");

        var capturedUserId = (Guid)fixture.Instance1.Facade.LastArguments![0]!;
        capturedUserId.ShouldBe(userId);
    }

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

    [Fact]
    public async Task ReconnectToInstance2_PreservesDeviceId_ChangesConnection_AndReceivesEvents()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        await using HubConnection first = CreateClient(fixture.Instance1, userId);
        await first.StartAsync();
        string firstConnectionId = first.ConnectionId!;
        await first.InvokeAsync<ConnectCommandAck>(
            "RegisterConnection",
            new RegisterConnectionRequest(Guid.NewGuid(), deviceId, "Browser"));
        await first.StopAsync();

        fixture.Instance2.Facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Snapshot: new ConnectSnapshot(
                userId,
                Player(7),
                Queue(8),
                new PresenceStateDto([], deviceId, firstConnectionId, 9),
                Now));
        await using HubConnection second = CreateClient(fixture.Instance2, userId);
        TaskCompletionSource<PlayerStateChangedEvent> futureEvent =
            Completion<PlayerStateChangedEvent>();
        second.On<PlayerStateChangedEvent>("PlayerStateChanged", futureEvent.SetResult);
        await second.StartAsync();
        string secondConnectionId = second.ConnectionId!;
        ConnectSnapshotResponse snapshot =
            await second.InvokeAsync<ConnectSnapshotResponse>("GetSnapshot");
        fixture.Instance2.Facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Presence: new PresenceStateDto([], deviceId, secondConnectionId, 10));
        await second.InvokeAsync<ConnectCommandAck>(
            "RegisterConnection",
            new RegisterConnectionRequest(Guid.NewGuid(), deviceId, "Browser"));

        await PublishUntilReceivedAsync(
            () => fixture.Instance2.Broadcaster.BroadcastAsync(
                userId,
                Guid.NewGuid(),
                Applied(player: Player(8)),
                default),
            futureEvent.Task);

        secondConnectionId.ShouldNotBe(firstConnectionId);
        snapshot.Presence!.AudioOwnerConnectionId.ShouldBe(firstConnectionId);
        snapshot.Presence.AudioOwnerConnectionId.ShouldNotBe(secondConnectionId);
        var registration = (Connect.Application.Commands.RegisterConnectionCommand)
            fixture.Instance2.Facade.LastArguments![0]!;
        registration.DeviceId.ShouldBe(deviceId);
        (await futureEvent.Task.WaitAsync(Timeout)).Player.Version.ShouldBe(8);
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
