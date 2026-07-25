using System.Buffers.Binary;
using Connect.Application.Commands;
using Connect.Application.Results;
using Connect.Domain.Queue;
using Connect.Presentation.Application;
using Connect.Presentation.Broadcasting;
using Connect.Presentation.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Connect.Presentation.Hubs;

[Authorize]
public sealed class PlayerHub(
    IConnectCommandFacade facade,
    IConnectBroadcaster broadcaster,
    TimeProvider timeProvider,
    ILogger<PlayerHub> logger)
    : Hub<IConnectHubClient>
{
    public override async Task OnConnectedAsync()
    {
        Guid userId = GetUserId();
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ConnectGroupNames.User(userId),
            Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public Task<ConnectCommandAck> RegisterConnection(
        RegisterConnectionRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(RegisterConnection),
            (userId, serverTime, cancellationToken) => facade.RegisterAsync(
                new RegisterConnectionCommand(
                    userId,
                    request!.CommandId,
                    request.DeviceId,
                    request.DeviceName,
                    Context.ConnectionId,
                    serverTime),
                cancellationToken));

    public async Task<ConnectCommandAck> RefreshConnectionLease()
    {
        Guid userId = GetUserId();
        try
        {
            ConnectApplicationResult result = await facade.HeartbeatAsync(
                new RefreshConnectionLeaseCommand(userId, Context.ConnectionId),
                Context.ConnectionAborted);
            if (result.Status != ConnectCommandStatus.Applied)
            {
                ConnectHubLog.CommandCompleted(
                    logger,
                    userId,
                    Context.ConnectionId,
                    null,
                    nameof(RefreshConnectionLease),
                    result.Status,
                    result.Outcome?.PlayerVersion,
                    result.Outcome?.QueueVersion,
                    result.Outcome?.PresenceVersion);
            }

            return ConnectTransportMapper.ToAck(null, result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw HandleUnexpected(exception, userId, null, nameof(RefreshConnectionLease));
        }
    }

    public Task<ConnectCommandAck> DisconnectConnection(CommandRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(DisconnectConnection),
            (userId, serverTime, cancellationToken) => facade.DisconnectAsync(
                new DisconnectConnectionCommand(
                    userId,
                    request!.CommandId,
                    Context.ConnectionId,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> SelectActiveDevice(
        SelectActiveDeviceRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(SelectActiveDevice),
            (userId, serverTime, cancellationToken) => facade.SelectDeviceAsync(
                new SelectActiveDeviceCommand(
                    userId,
                    request!.CommandId,
                    request.DeviceId,
                    Context.ConnectionId,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> Play(CommandRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(Play),
            (userId, serverTime, cancellationToken) => facade.PlayAsync(
                new PlayCommand(userId, request!.CommandId, serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> Pause(CommandRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(Pause),
            (userId, serverTime, cancellationToken) => facade.PauseAsync(
                new PauseCommand(userId, request!.CommandId, serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> ChangePosition(ChangePositionRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(ChangePosition),
            (userId, serverTime, cancellationToken) => facade.ChangePositionAsync(
                new ChangePositionCommand(
                    userId,
                    request!.CommandId,
                    request.PositionMs,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> ChangeVolume(ChangeVolumeRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(ChangeVolume),
            (userId, serverTime, cancellationToken) => facade.ChangeVolumeAsync(
                new ChangeVolumeCommand(
                    userId,
                    request!.CommandId,
                    request.VolumePercent,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> AddQueueItem(AddQueueItemRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(AddQueueItem),
            (userId, serverTime, cancellationToken) => facade.AddQueueItemAsync(
                new AddQueueItemCommand(
                    userId,
                    request!.CommandId,
                    request.QueueItemId,
                    request.TrackId,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> RemoveQueueItem(QueueItemRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(RemoveQueueItem),
            (userId, serverTime, cancellationToken) => facade.RemoveQueueItemAsync(
                new RemoveQueueItemCommand(
                    userId,
                    request!.CommandId,
                    request.QueueItemId,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> MoveQueueItem(MoveQueueItemRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(MoveQueueItem),
            (userId, serverTime, cancellationToken) => facade.MoveQueueItemAsync(
                new MoveQueueItemCommand(
                    userId,
                    request!.CommandId,
                    request.QueueItemId,
                    request.TargetIndex,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> SelectQueueItem(QueueItemRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(SelectQueueItem),
            (userId, serverTime, cancellationToken) => facade.SelectQueueItemAsync(
                new SelectQueueItemCommand(
                    userId,
                    request!.CommandId,
                    request.QueueItemId,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> ShuffleQueue(CommandRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(ShuffleQueue),
            (userId, serverTime, cancellationToken) => facade.ShuffleQueueAsync(
                new ShuffleQueueCommand(
                    userId,
                    request!.CommandId,
                    DeriveShuffleSeed(request.CommandId),
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> UnshuffleQueue(CommandRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(UnshuffleQueue),
            (userId, serverTime, cancellationToken) => facade.UnshuffleQueueAsync(
                new UnshuffleQueueCommand(userId, request!.CommandId, serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> ChangeRepeatMode(
        ChangeRepeatModeRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(ChangeRepeatMode),
            (userId, serverTime, cancellationToken) => facade.ChangeRepeatModeAsync(
                new ChangeRepeatModeCommand(
                    userId,
                    request!.CommandId,
                    (RepeatMode)request.RepeatMode,
                    serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> NextQueueItem(CommandRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(NextQueueItem),
            (userId, serverTime, cancellationToken) => facade.NextQueueItemAsync(
                new NextQueueItemCommand(userId, request!.CommandId, serverTime),
                cancellationToken));

    public Task<ConnectCommandAck> PreviousQueueItem(CommandRequest? request) =>
        ExecuteMutationAsync(
            request,
            request?.CommandId,
            nameof(PreviousQueueItem),
            (userId, serverTime, cancellationToken) => facade.PreviousQueueItemAsync(
                new PreviousQueueItemCommand(userId, request!.CommandId, serverTime),
                cancellationToken));

    public async Task<ConnectSnapshotResponse> GetSnapshot()
    {
        Guid userId = GetUserId();
        DateTimeOffset serverTime = timeProvider.GetUtcNow();
        try
        {
            ConnectApplicationResult result = await facade.GetSnapshotAsync(
                userId,
                serverTime,
                Context.ConnectionAborted);
            return ConnectTransportMapper.ToSnapshot(result, serverTime);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw HandleUnexpected(exception, userId, null, nameof(GetSnapshot));
        }
    }

    private async Task<ConnectCommandAck> ExecuteMutationAsync<TRequest>(
        TRequest? request,
        Guid? commandId,
        string commandType,
        Func<Guid, DateTimeOffset, CancellationToken, Task<ConnectApplicationResult>> execute)
        where TRequest : class
    {
        Guid userId = GetUserId();
        if (request is null || commandId is null || commandId == Guid.Empty)
        {
            return ConnectTransportMapper.ValidationFailed(commandId);
        }

        DateTimeOffset serverTime = timeProvider.GetUtcNow();
        try
        {
            ConnectApplicationResult result = await execute(
                userId,
                serverTime,
                Context.ConnectionAborted);
            ConnectHubLog.CommandCompleted(
                logger,
                userId,
                Context.ConnectionId,
                commandId,
                commandType,
                result.Status,
                result.Outcome?.PlayerVersion,
                result.Outcome?.QueueVersion,
                result.Outcome?.PresenceVersion);

            if (result.Status == ConnectCommandStatus.Applied)
            {
                await broadcaster.BroadcastAsync(
                    userId,
                    commandId.Value,
                    result,
                    Context.ConnectionAborted);
            }

            return ConnectTransportMapper.ToAck(commandId, result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw HandleUnexpected(exception, userId, commandId, commandType);
        }
    }

    private Guid GetUserId()
    {
        if (Guid.TryParse(Context.UserIdentifier, out Guid userId) &&
            userId != Guid.Empty)
        {
            return userId;
        }

        throw new HubException("connect_identity_invalid");
    }

    private HubException HandleUnexpected(
        Exception exception,
        Guid userId,
        Guid? commandId,
        string commandType)
    {
        ConnectHubLog.UnexpectedFailure(
            logger,
            exception,
            userId,
            Context.ConnectionId,
            commandId,
            commandType);
        return new HubException("connect_internal_error");
    }

    private static int DeriveShuffleSeed(Guid commandId)
    {
        Span<byte> bytes = stackalloc byte[16];
        commandId.TryWriteBytes(bytes);
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }
}
