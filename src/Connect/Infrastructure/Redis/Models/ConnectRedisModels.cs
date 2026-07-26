namespace Connect.Infrastructure.Redis.Models;

public sealed record PlayerStateRedisModel(
    bool IsPlaying,
    long PositionMs,
    DateTimeOffset PositionUpdatedAt,
    int VolumePercent,
    long Version);

public enum RepeatModeRedisModel
{
    None,
    Queue,
    Track
}

public enum PlaybackSourceTypeRedisModel
{
    Playlist,
    Album,
    Release,
    LikedSongs,
    Search,
    Manual
}

public sealed record QueueItemRedisModel(
    Guid QueueItemId,
    Guid TrackId,
    long CanonicalOrder);

public sealed record QueueStateRedisModel(
    IReadOnlyList<QueueItemRedisModel> Items,
    Guid? CurrentQueueItemId,
    RepeatModeRedisModel RepeatMode,
    bool IsShuffled,
    long Version,
    Guid? SourceId = null,
    PlaybackSourceTypeRedisModel? SourceType = null);

public sealed record DeviceConnectionRedisModel(
    string ConnectionId,
    DateTimeOffset ConnectedAt);

public sealed record DeviceRedisModel(
    Guid DeviceId,
    string Name,
    IReadOnlyList<DeviceConnectionRedisModel> Connections);

public sealed record PresenceStateRedisModel(
    IReadOnlyList<DeviceRedisModel> Devices,
    Guid? ActiveDeviceId,
    string? AudioOwnerConnectionId,
    long Version);
