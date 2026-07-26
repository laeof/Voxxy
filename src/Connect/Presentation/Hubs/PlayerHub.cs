using System.Buffers.Binary;
using System.Diagnostics;
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
    ConnectTransportOptions transportOptions,
    ConnectInvocationRateLimiter rateLimiter,
    ConnectTransportMetrics metrics,
    ILogger<PlayerHub> logger)
    : Hub<IConnectHubClient>
{
    private static readonly object ConnectionMetricsRecordedKey = new();

    public override async Task OnConnectedAsync()
    {
        ValidateOrigin();
        Guid userId = GetUserId();
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ConnectGroupNames.User(userId),
            Context.ConnectionAborted);
        await base.OnConnectedAsync();
        metrics.CurrentConnections.Add(1);
        metrics.ConnectionsStarted.Add(1);
        Context.Items[ConnectionMetricsRecordedKey] = true;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        rateLimiter.RemoveConnection(Context.ConnectionId);
        try
        {
            Guid userId = GetUserId();
            var commandId = Guid.NewGuid();
            ConnectApplicationResult result = await facade.DisconnectAsync(
                new DisconnectConnectionCommand(
                    userId,
                    commandId,
                    Context.ConnectionId,
                    timeProvider.GetUtcNow()),
                CancellationToken.None);
            if (result.Status == ConnectCommandStatus.Applied)
            {
                await broadcaster.BroadcastAsync(
                    userId,
                    commandId,
                    result,
                    CancellationToken.None);
            }
        }
        catch (Exception disconnectException)
        {
            logger.LogWarning(
                disconnectException,
                "Connect disconnect cleanup failed for connection {ConnectionId}. Lease cleanup will retry.",
                Context.ConnectionId);
        }
        if (Context.Items.Remove(ConnectionMetricsRecordedKey))
        {
            metrics.CurrentConnections.Add(-1);
            metrics.ConnectionsClosed.Add(1);
        }
        await base.OnDisconnectedAsync(exception);
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
                    request.DeviceName.Trim(),
                    Context.ConnectionId,
                    serverTime),
                cancellationToken));

    public async Task<ConnectCommandAck> RefreshConnectionLease()
    {
        Guid userId = GetUserId();
        EnsureRateLimit(ConnectRateLimitBucket.Heartbeat, nameof(RefreshConnectionLease));
        long started = Stopwatch.GetTimestamp();
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

            RecordCommandMetrics(nameof(RefreshConnectionLease), result.Status, started);
            return ConnectTransportMapper.ToAck(null, result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            metrics.Failures.Add(1, new KeyValuePair<string, object?>(
                "command.type",
                nameof(RefreshConnectionLease)));
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
        long started = Stopwatch.GetTimestamp();
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
        finally
        {
            metrics.SnapshotDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
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

        EnsureRateLimit(BucketFor(commandType), commandType);
        DateTimeOffset serverTime = timeProvider.GetUtcNow();
        long started = Stopwatch.GetTimestamp();
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
                long broadcastStarted = Stopwatch.GetTimestamp();
                try
                {
                    await broadcaster.BroadcastAsync(
                        userId,
                        commandId.Value,
                        result,
                        Context.ConnectionAborted);
                    metrics.BroadcastDuration.Record(
                        Stopwatch.GetElapsedTime(broadcastStarted).TotalMilliseconds,
                        new KeyValuePair<string, object?>(
                            "event.type",
                            GetEventType(result)));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    metrics.BroadcastFailures.Add(
                        1,
                        new KeyValuePair<string, object?>(
                            "event.type",
                            GetEventType(result)));
                    ConnectHubLog.DeliveryUnconfirmed(
                        logger,
                        exception,
                        userId,
                        commandId.Value,
                        GetEventType(result),
                        result.Outcome?.PlayerVersion,
                        result.Outcome?.QueueVersion,
                        result.Outcome?.PresenceVersion);
                    throw new HubException("connect_delivery_unconfirmed");
                }
            }

            RecordCommandMetrics(commandType, result.Status, started);
            return ConnectTransportMapper.ToAck(commandId, result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HubException exception)
            when (exception.Message is
                "connect_delivery_unconfirmed" or
                "connect_rate_limited")
        {
            throw;
        }
        catch (Exception exception)
        {
            metrics.Failures.Add(
                1,
                new KeyValuePair<string, object?>("command.type", commandType));
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

    private void ValidateOrigin()
    {
        string origin = Context.GetHttpContext()?.Request.Headers.Origin.ToString() ?? string.Empty;
        if (origin.Length > 0 && !transportOptions.AllowedOrigins.Contains(origin))
        {
            throw new HubException("connect_origin_not_allowed");
        }
    }

    private void EnsureRateLimit(ConnectRateLimitBucket bucket, string commandType)
    {
        if (commandType is nameof(RegisterConnection) or nameof(DisconnectConnection))
        {
            return;
        }

        if (rateLimiter.TryAcquire(Context.ConnectionId, bucket))
        {
            return;
        }

        metrics.RateLimited.Add(
            1,
            new KeyValuePair<string, object?>("command.type", commandType));
        throw new HubException("connect_rate_limited");
    }

    private void RecordCommandMetrics(
        string commandType,
        ConnectCommandStatus status,
        long started)
    {
        var commandTag = new KeyValuePair<string, object?>("command.type", commandType);
        var statusTag = new KeyValuePair<string, object?>("status", status.ToString());
        metrics.Commands.Add(1, commandTag, statusTag);
        metrics.CommandDuration.Record(
            Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            commandTag,
            statusTag);
        if (status == ConnectCommandStatus.Conflict)
        {
            metrics.Conflicts.Add(1, commandTag);
        }
        if (status == ConnectCommandStatus.Duplicate)
        {
            metrics.Duplicates.Add(1, commandTag);
        }
        if (commandType == nameof(RegisterConnection) &&
            status is ConnectCommandStatus.Applied or ConnectCommandStatus.Duplicate)
        {
            metrics.ReconnectRegistrations.Add(1);
        }
    }

    private static ConnectRateLimitBucket BucketFor(string commandType) =>
        commandType switch
        {
            nameof(ChangePosition) => ConnectRateLimitBucket.Position,
            nameof(ChangeVolume) => ConnectRateLimitBucket.Volume,
            _ => ConnectRateLimitBucket.General
        };

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

    private static string GetEventType(ConnectApplicationResult result) =>
        (result.Player, result.Queue, result.Presence) switch
        {
            (not null, not null, _) => "PlayerQueueStateChanged",
            (not null, _, not null) => "PlayerPresenceStateChanged",
            (not null, _, _) => "PlayerStateChanged",
            (_, not null, _) => "QueueStateChanged",
            (_, _, not null) => "PresenceStateChanged",
            _ => "None"
        };
}
