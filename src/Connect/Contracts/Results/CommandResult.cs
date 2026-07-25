namespace Connect.Contracts.Results;

public enum CommandStatus
{
    Applied,
    NoOp,
    Duplicate,
    Rejected,
    Conflict
}

public sealed record CommandResult(
    Guid CommandId,
    CommandStatus Status,
    ConnectError? Error);

public sealed record ConnectError(
    string Code,
    string Message,
    long? CurrentPlayerVersion,
    long? CurrentQueueVersion,
    long? CurrentPresenceVersion,
    bool SnapshotRequired);
