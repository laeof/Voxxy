using Connect.Application.Commands;
using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Presentation.Hubs;
using Connect.Presentation.Transport;
using Connect.Presentation.UnitTests.Fakes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Connect.Presentation.UnitTests.Hubs;

public sealed class PlayerHubTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now =
        new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    public async Task Register_UsesUserIdFromClaimsAndContextConnectionId()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, _, _) = CreateHub();
        var request = new RegisterConnectionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Browser");

        await hub.RegisterConnection(request);

        RegisterConnectionCommand command =
            facade.LastCommand.ShouldBeOfType<RegisterConnectionCommand>();
        command.UserId.ShouldBe(UserId);
        command.ConnectionId.ShouldBe("server-connection");
        command.DeviceId.ShouldBe(request.DeviceId);
    }

    [Fact]
    public async Task MissingUserIdentifier_DoesNotCallHandler()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, _, _) =
            CreateHub(missingUser: true);

        HubException exception = await Should.ThrowAsync<HubException>(() =>
            hub.Play(new CommandRequest(Guid.NewGuid())));

        exception.Message.ShouldBe("connect_identity_invalid");
        facade.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task OnConnected_AddsSocketToUserGroupWithoutRegistration()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, _, _) = CreateHub();
        var groups = new TestGroupManager();
        hub.Groups = groups;

        await hub.OnConnectedAsync();

        groups.AddedConnectionId.ShouldBe("server-connection");
        groups.AddedGroupName.ShouldBe($"connect:user:{UserId:D}");
        facade.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Command_UsesInjectedTimeProviderOncePerInvocation()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, _, TestTimeProvider clock) =
            CreateHub();

        await hub.Pause(new CommandRequest(Guid.NewGuid()));

        PauseCommand command = facade.LastCommand.ShouldBeOfType<PauseCommand>();
        command.ServerTime.ShouldBe(Now);
        clock.GetUtcNowCalls.ShouldBe(1);
    }

    [Fact]
    public async Task EmptyCommandId_ReturnsValidationWithoutHandler()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();

        ConnectCommandAck ack = await hub.Play(new CommandRequest(Guid.Empty));

        ack.Status.ShouldBe(ConnectCommandAckStatus.ValidationFailed);
        facade.Calls.ShouldBe(0);
        broadcaster.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(ConnectCommandStatus.Applied, 1)]
    [InlineData(ConnectCommandStatus.Duplicate, 0)]
    [InlineData(ConnectCommandStatus.ValidationFailed, 0)]
    public async Task Register_BroadcastPolicyFollowsStatus(
        ConnectCommandStatus status,
        int expectedBroadcasts)
    {
        PresenceStateDto presence = Presence(version: 1);
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(status, Presence: presence);

        ConnectCommandAck ack = await hub.RegisterConnection(
            new RegisterConnectionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Browser"));

        ack.Status.ToString().ShouldBe(status.ToString());
        broadcaster.Calls.ShouldBe(expectedBroadcasts);
    }

    [Fact]
    public async Task Heartbeat_UsesContextConnectionIdAndDoesNotBroadcast()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(ConnectCommandStatus.Applied);

        ConnectCommandAck ack = await hub.RefreshConnectionLease();

        RefreshConnectionLeaseCommand command =
            facade.LastCommand.ShouldBeOfType<RefreshConnectionLeaseCommand>();
        command.ConnectionId.ShouldBe("server-connection");
        ack.Status.ShouldBe(ConnectCommandAckStatus.Applied);
        broadcaster.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Play_Applied_BroadcastsPlayer()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Player: Player(version: 2));

        await hub.Play(new CommandRequest(Guid.NewGuid()));

        broadcaster.Calls.ShouldBe(1);
        broadcaster.Result!.Player!.Version.ShouldBe(2);
    }

    [Theory]
    [InlineData(ConnectCommandStatus.NoChanges)]
    [InlineData(ConnectCommandStatus.Conflict)]
    public async Task NonAppliedPlayerCommand_DoesNotBroadcast(
        ConnectCommandStatus status)
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(status);

        await hub.ChangeVolume(new ChangeVolumeRequest(Guid.NewGuid(), 50));

        broadcaster.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AddQueueItem_Applied_BroadcastsQueue()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Queue: Queue(version: 3));

        await hub.AddQueueItem(
            new AddQueueItemRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()));

        broadcaster.Calls.ShouldBe(1);
        broadcaster.Result!.Queue!.Version.ShouldBe(3);
    }

    [Fact]
    public async Task AddQueueItem_Duplicate_ReturnsOriginalQueueItemId()
    {
        var originalId = Guid.NewGuid();
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Duplicate,
            Outcome: new ConnectCommandOutcome(QueueItemId: originalId));

        ConnectCommandAck ack = await hub.AddQueueItem(
            new AddQueueItemRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()));

        ack.Outcome!.QueueItemId.ShouldBe(originalId);
        broadcaster.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task MoveQueueItem_InvalidIndex_DoesNotBroadcast()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.InvalidQueueIndex,
            Error: "internal details");

        ConnectCommandAck ack = await hub.MoveQueueItem(
            new MoveQueueItemRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                100));

        ack.ErrorCode.ShouldBe("connect_invalid_queue_index");
        broadcaster.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task SelectQueueItem_Applied_BroadcastsCoordinatedResult()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = CoordinatedPlayerQueueResult();

        await hub.SelectQueueItem(
            new QueueItemRequest(Guid.NewGuid(), Guid.NewGuid()));

        broadcaster.Calls.ShouldBe(1);
        broadcaster.Result!.Player.ShouldNotBeNull();
        broadcaster.Result.Queue.ShouldNotBeNull();
    }

    [Fact]
    public async Task NextQueueItem_Applied_BroadcastsCoordinatedResult()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = CoordinatedPlayerQueueResult();

        await hub.NextQueueItem(new CommandRequest(Guid.NewGuid()));

        broadcaster.Calls.ShouldBe(1);
        facade.LastCommand.ShouldBeOfType<NextQueueItemCommand>();
    }

    [Fact]
    public async Task DisconnectActiveOwner_BroadcastsPlayerPresenceResult()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Player: Player(version: 2),
            Presence: Presence(version: 3));

        await hub.DisconnectConnection(new CommandRequest(Guid.NewGuid()));

        broadcaster.Calls.ShouldBe(1);
        broadcaster.Result!.Player.ShouldNotBeNull();
        broadcaster.Result.Presence.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetSnapshot_ReturnsCallerOnlySnapshotWithServerTime()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Snapshot: new ConnectSnapshot(
                UserId,
                Player(1),
                Queue(2),
                Presence(3),
                Now));

        ConnectSnapshotResponse response = await hub.GetSnapshot();

        response.ServerTime.ShouldBe(Now);
        response.Player!.Version.ShouldBe(1);
        broadcaster.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(ConnectCommandStatus.Unavailable, "connect_unavailable")]
    [InlineData(ConnectCommandStatus.CorruptState, "connect_state_corrupt")]
    public async Task PersistenceFailure_ReturnsSafeTransportError(
        ConnectCommandStatus status,
        string expectedCode)
    {
        (PlayerHub hub, TestConnectCommandFacade facade, _, _) = CreateHub();
        facade.Result = new ConnectApplicationResult(
            status,
            Error: "redis key connect:v2:secret serializer stack trace");

        ConnectCommandAck ack = await hub.Play(new CommandRequest(Guid.NewGuid()));

        ack.ErrorCode.ShouldBe(expectedCode);
        ack.ErrorCode!.ShouldNotContain("redis");
    }

    [Fact]
    public async Task Cancellation_IsPropagated()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, _, _) = CreateHub();
        facade.Exception = new OperationCanceledException();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            hub.Play(new CommandRequest(Guid.NewGuid())));
    }

    [Fact]
    public async Task UnexpectedException_ReturnsSafeHubError()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, _, _) = CreateHub();
        facade.Exception = new InvalidOperationException("sensitive programming detail");

        HubException exception = await Should.ThrowAsync<HubException>(() =>
            hub.Play(new CommandRequest(Guid.NewGuid())));

        exception.Message.ShouldBe("connect_internal_error");
    }

    [Fact]
    public async Task AppliedCommand_WhenBroadcastFails_ReturnsDeliveryUnconfirmed()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        facade.Result = new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Player: Player(3),
            Outcome: new ConnectCommandOutcome(PlayerVersion: 3));
        broadcaster.Exception = new InvalidOperationException("redis backplane unavailable");

        HubException exception = await Should.ThrowAsync<HubException>(() =>
            hub.Play(new CommandRequest(Guid.NewGuid())));

        exception.Message.ShouldBe("connect_delivery_unconfirmed");
        facade.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task DeliveryUnconfirmed_SameCommandRetryIsDuplicate_AndSnapshotHasCommittedState()
    {
        (PlayerHub hub, TestConnectCommandFacade facade, TestBroadcaster broadcaster, _) =
            CreateHub();
        var commandId = Guid.NewGuid();
        PlayerStateDto committedPlayer = Player(3);
        facade.Results.Enqueue(new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Player: committedPlayer,
            Outcome: new ConnectCommandOutcome(PlayerVersion: 3)));
        facade.Results.Enqueue(new ConnectApplicationResult(ConnectCommandStatus.Duplicate));
        facade.Results.Enqueue(new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Snapshot: new ConnectSnapshot(
                UserId,
                committedPlayer,
                Queue(1),
                Presence(1),
                Now)));
        broadcaster.Exception = new InvalidOperationException("backplane unavailable");

        HubException exception = await Should.ThrowAsync<HubException>(() =>
            hub.Play(new CommandRequest(commandId)));
        broadcaster.Exception = null;
        ConnectCommandAck duplicate = await hub.Play(new CommandRequest(commandId));
        ConnectSnapshotResponse snapshot = await hub.GetSnapshot();

        exception.Message.ShouldBe("connect_delivery_unconfirmed");
        duplicate.Status.ShouldBe(ConnectCommandAckStatus.Duplicate);
        snapshot.Player!.Version.ShouldBe(3);
        facade.Calls.ShouldBe(3);
        broadcaster.Calls.ShouldBe(0);
    }

    private static (
        PlayerHub Hub,
        TestConnectCommandFacade Facade,
        TestBroadcaster Broadcaster,
        TestTimeProvider TimeProvider) CreateHub(bool missingUser = false)
    {
        var facade = new TestConnectCommandFacade();
        var broadcaster = new TestBroadcaster();
        var timeProvider = new TestTimeProvider(Now);
        var hub = new PlayerHub(
            facade,
            broadcaster,
            timeProvider,
            NullLogger<PlayerHub>.Instance)
        {
            Context = new TestHubCallerContext(
                missingUser ? null : UserId.ToString("D"))
        };
        return (hub, facade, broadcaster, timeProvider);
    }

    private static ConnectApplicationResult CoordinatedPlayerQueueResult() =>
        new(
            ConnectCommandStatus.Applied,
            Player: Player(version: 4),
            Queue: Queue(version: 5));

    private static PlayerStateDto Player(long version) =>
        new(false, 0, Now, 50, version);

    private static QueueStateDto Queue(long version) =>
        new([], null, RepeatModeDto.None, false, version);

    private static PresenceStateDto Presence(long version) =>
        new([], null, null, version);
}
