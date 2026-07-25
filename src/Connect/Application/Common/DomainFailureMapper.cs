using Connect.Application.Results;

namespace Connect.Application.Common;

internal static class DomainFailureMapper
{
    public static ConnectApplicationResult Validation(Exception exception) =>
        HandlerResultFactory.ValidationFailed(exception.Message);

    public static ConnectApplicationResult QueueItemNotFound(KeyNotFoundException exception) =>
        new(ConnectCommandStatus.QueueItemNotFound, Error: exception.Message);

    public static ConnectApplicationResult InvalidQueueIndex(
        ArgumentOutOfRangeException exception) =>
        new(ConnectCommandStatus.InvalidQueueIndex, Error: exception.Message);
}
