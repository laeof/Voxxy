using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;
using Connect.Infrastructure.Redis.Models;

namespace Connect.Infrastructure.Redis.Serialization;

internal static class ConnectRedisMapper
{
    public static PlayerStateRedisModel ToRedis(PlayerState state) =>
        new(
            state.IsPlaying,
            state.PositionMs,
            state.PositionUpdatedAt,
            state.VolumePercent,
            state.Version);

    public static PlayerState ToDomain(PlayerStateRedisModel model) =>
        PlayerState.Restore(
            model.IsPlaying,
            model.PositionMs,
            model.PositionUpdatedAt,
            model.VolumePercent,
            model.Version);

    public static QueueStateRedisModel ToRedis(QueueState state) =>
        new(
            state.Items.Select(item => new QueueItemRedisModel(
                    item.QueueItemId,
                    item.TrackId,
                    item.CanonicalOrder))
                .ToArray(),
            state.CurrentQueueItemId,
            (RepeatModeRedisModel)state.RepeatMode,
            state.IsShuffled,
            state.Version,
            state.SourceId,
            state.SourceType is null
                ? null
                : (PlaybackSourceTypeRedisModel)state.SourceType);

    public static QueueState ToDomain(QueueStateRedisModel model) =>
        QueueState.Restore(
            model.Items.Select(item => new QueueItem(
                item.QueueItemId,
                item.TrackId,
                item.CanonicalOrder)),
            model.CurrentQueueItemId,
            (RepeatMode)model.RepeatMode,
            model.IsShuffled,
            model.Version,
            model.SourceId,
            model.SourceType is null ? null : (PlaybackSourceType)model.SourceType);

    public static PresenceStateRedisModel ToRedis(PresenceState state) =>
        new(
            state.Devices.Select(device => new DeviceRedisModel(
                    device.DeviceId,
                    device.Name,
                    device.Connections.Select(connection =>
                            new DeviceConnectionRedisModel(
                                connection.ConnectionId,
                                connection.ConnectedAt))
                        .ToArray()))
                .ToArray(),
            state.ActiveDeviceId,
            state.AudioOwnerConnectionId,
            state.Version);

    public static PresenceState ToDomain(PresenceStateRedisModel model) =>
        PresenceState.Restore(
            model.Devices.Select(device => Device.Restore(
                device.DeviceId,
                device.Name,
                device.Connections.Select(connection => new DeviceConnection(
                    connection.ConnectionId,
                    connection.ConnectedAt)))),
            model.ActiveDeviceId,
            model.AudioOwnerConnectionId,
            model.Version);
}
