using Connect.Application.Results;

namespace Connect.Presentation.Transport;

internal static class ConnectTransportMapper
{
    public static ConnectCommandAck ToAck(
        Guid? commandId,
        ConnectApplicationResult result) =>
        new(
            commandId,
            ToStatus(result.Status),
            ToErrorCode(result.Status),
            result.Outcome is null
                ? null
                : new ConnectCommandOutcomeContract(
                    result.Outcome.DeviceId,
                    result.Outcome.ConnectionId,
                    result.Outcome.QueueItemId,
                    result.Outcome.PlayerVersion,
                    result.Outcome.QueueVersion,
                    result.Outcome.PresenceVersion,
                    result.Outcome.RemovedConnectionCount));

    public static ConnectCommandAck ValidationFailed(Guid? commandId) =>
        new(
            commandId,
            ConnectCommandAckStatus.ValidationFailed,
            "connect_validation_failed",
            null);

    public static ConnectSnapshotResponse ToSnapshot(
        ConnectApplicationResult result,
        DateTimeOffset serverTime) =>
        result.Snapshot is not null
            ? new ConnectSnapshotResponse(
                ConnectCommandAckStatus.Applied,
                result.Snapshot.Player,
                result.Snapshot.Queue,
                result.Snapshot.Presence,
                result.Snapshot.ServerTime,
                null)
            : new ConnectSnapshotResponse(
                ToStatus(result.Status),
                null,
                null,
                null,
                serverTime,
                ToErrorCode(result.Status));

    private static ConnectCommandAckStatus ToStatus(ConnectCommandStatus status) =>
        status switch
        {
            ConnectCommandStatus.Applied => ConnectCommandAckStatus.Applied,
            ConnectCommandStatus.Duplicate => ConnectCommandAckStatus.Duplicate,
            ConnectCommandStatus.NoChanges => ConnectCommandAckStatus.NoChanges,
            ConnectCommandStatus.Conflict => ConnectCommandAckStatus.Conflict,
            ConnectCommandStatus.CommandCollision =>
                ConnectCommandAckStatus.CommandCollision,
            ConnectCommandStatus.ConnectionNotFound =>
                ConnectCommandAckStatus.ConnectionNotFound,
            ConnectCommandStatus.DeviceNotFound => ConnectCommandAckStatus.DeviceNotFound,
            ConnectCommandStatus.DeviceOffline => ConnectCommandAckStatus.DeviceOffline,
            ConnectCommandStatus.QueueItemNotFound =>
                ConnectCommandAckStatus.QueueItemNotFound,
            ConnectCommandStatus.InvalidQueueIndex =>
                ConnectCommandAckStatus.InvalidQueueIndex,
            ConnectCommandStatus.CorruptState => ConnectCommandAckStatus.CorruptState,
            ConnectCommandStatus.Unavailable => ConnectCommandAckStatus.Unavailable,
            ConnectCommandStatus.ValidationFailed =>
                ConnectCommandAckStatus.ValidationFailed,
            _ => throw new InvalidOperationException(
                $"Unsupported command status '{status}'.")
        };

    private static string? ToErrorCode(ConnectCommandStatus status) =>
        status switch
        {
            ConnectCommandStatus.ValidationFailed => "connect_validation_failed",
            ConnectCommandStatus.Conflict => "connect_conflict",
            ConnectCommandStatus.CommandCollision => "connect_command_collision",
            ConnectCommandStatus.ConnectionNotFound => "connect_connection_not_found",
            ConnectCommandStatus.DeviceNotFound => "connect_device_not_found",
            ConnectCommandStatus.DeviceOffline => "connect_device_offline",
            ConnectCommandStatus.QueueItemNotFound => "connect_queue_item_not_found",
            ConnectCommandStatus.InvalidQueueIndex => "connect_invalid_queue_index",
            ConnectCommandStatus.CorruptState => "connect_state_corrupt",
            ConnectCommandStatus.Unavailable => "connect_unavailable",
            _ => null
        };
}
