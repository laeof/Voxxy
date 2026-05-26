using Microsoft.Extensions.Logging;

namespace Application.Abstractions.Behaviors;

internal static partial class LoggingMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processing command {Command}")]
    public static partial void ProcessingCommand(
        ILogger logger,
        string command);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Completed command {Command}")]
    public static partial void CompletedCommand(
        ILogger logger,
        string command);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Completed command {Command} with error")]
    public static partial void CommandFailed(
        ILogger logger,
        string command);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Processing query {Query}")]
    public static partial void ProcessingQuery(
        ILogger logger,
        string query);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Completed query {Query}")]
    public static partial void CompletedQuery(
        ILogger logger,
        string query);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Completed query {Query} with error")]
    public static partial void QueryFailed(
        ILogger logger,
        string query);
}