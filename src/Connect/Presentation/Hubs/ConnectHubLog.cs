using Microsoft.Extensions.Logging;
using Connect.Application.Results;

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
}
