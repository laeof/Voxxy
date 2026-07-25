namespace Connect.Contracts.States;

public enum RepeatModeDto
{
    None,
    Queue,
    Track
}

public sealed record QueueItemDto(
    Guid QueueItemId,
    Guid TrackId,
    long CanonicalOrder);

public sealed record QueueStateDto(
    IReadOnlyList<QueueItemDto> Items,
    Guid? CurrentQueueItemId,
    RepeatModeDto RepeatMode,
    bool IsShuffled,
    long Version);
