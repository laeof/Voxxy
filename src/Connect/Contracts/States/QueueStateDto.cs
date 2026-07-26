namespace Connect.Contracts.States;

public enum RepeatModeDto
{
    None,
    Queue,
    Track
}

public enum PlaybackSourceTypeDto
{
    Playlist,
    Album,
    Release,
    LikedSongs,
    Search,
    Manual
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
    long Version,
    Guid? SourceId = null,
    PlaybackSourceTypeDto? SourceType = null);
