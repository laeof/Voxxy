using Connect.Application.Results;
using Connect.Presentation.Transport;
using Microsoft.Extensions.Logging;

namespace Connect.Presentation.Hubs;

internal static partial class ConnectHubLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Connect command completed for user {UserId}, connection {ConnectionId}, " +
            "command {CommandId}, type {CommandType}, status {ApplicationStatus}, " +
            "versions player={PlayerVersion}, queue={QueueVersion}, presence={PresenceVersion}")]
    public static partial void CommandCompleted(
        ILogger logger,
        Guid userId,
        string connectionId,
        Guid? commandId,
        string commandType,
        ConnectCommandStatus applicationStatus,
        long? playerVersion,
        long? queueVersion,
        long? presenceVersion);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Unexpected Connect Hub failure for user {UserId}, connection " +
            "{ConnectionId}, command {CommandId}, type {CommandType}")]
    public static partial void UnexpectedFailure(
        ILogger logger,
        Exception exception,
        Guid userId,
        string connectionId,
        Guid? commandId,
        string commandType);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Authoritative Connect event delivery was not confirmed for user {UserId}, " +
            "command {CommandId}, event {EventType}, versions player={PlayerVersion}, " +
            "queue={QueueVersion}, presence={PresenceVersion}")]
    public static partial void DeliveryUnconfirmed(
        ILogger logger,
        Exception exception,
        Guid userId,
        Guid commandId,
        string eventType,
        long? playerVersion,
        long? queueVersion,
        long? presenceVersion);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "StartPlaybackContext received for user {UserId}, connection {ConnectionId}, " +
            "command {CommandId}, source {SourceId} ({SourceType}), startIndex={StartIndex}, " +
            "startTrack={StartTrackId}, startQueueItem={StartQueueItemId}, itemCount={ItemCount}")]
    public static partial void StartPlaybackContextReceived(
        ILogger logger,
        Guid userId,
        string connectionId,
        Guid? commandId,
        Guid? sourceId,
        int? sourceType,
        int? startIndex,
        Guid? startTrackId,
        Guid? startQueueItemId,
        int itemCount);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "StartPlaybackContext response ready for user {UserId}, connection " +
            "{ConnectionId}, command {CommandId}, status {ApplicationStatus}, versions " +
            "player={PlayerVersion}, queue={QueueVersion}, presence={PresenceVersion}")]
    public static partial void StartPlaybackContextResponseReady(
        ILogger logger,
        Guid userId,
        string connectionId,
        Guid? commandId,
        ConnectCommandAckStatus applicationStatus,
        long? playerVersion,
        long? queueVersion,
        long? presenceVersion);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "StartPlaybackContext failed for user {UserId}, connection {ConnectionId}, " +
            "command {CommandId}, source {SourceId} ({SourceType}), startIndex={StartIndex}, " +
            "itemCount={ItemCount}")]
    public static partial void StartPlaybackContextFailed(
        ILogger logger,
        Exception exception,
        Guid userId,
        string connectionId,
        Guid? commandId,
        Guid? sourceId,
        int? sourceType,
        int? startIndex,
        int itemCount);
}
