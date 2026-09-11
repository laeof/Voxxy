using Connect.Application.Abstractions.Persistence;
using Connect.Contracts.States;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;

namespace Connect.Application.Common;

internal static class ConnectDtoMapper
{
    public static PlayerStateDto ToDto(PlayerState state) =>
        new(
            state.IsPlaying,
            state.PositionMs,
            state.PositionUpdatedAt,
            state.VolumePercent,
            state.Version);

    public static QueueStateDto ToDto(QueueState state) =>
        new(
            state.Items.Select(item => new QueueItemDto(
                    item.QueueItemId,
                    item.TrackId,
                    item.CanonicalOrder))
                .ToArray(),
            state.CurrentQueueItemId,
            (RepeatModeDto)state.RepeatMode,
            state.IsShuffled,
            state.Version,
            state.SourceId,
            state.SourceType is null
                ? null
                : (PlaybackSourceTypeDto)state.SourceType);

    public static PresenceStateDto ToDto(PresenceState state) =>
        new(
            state.Devices.Select(device => new DevicePresenceDto(
                    device.DeviceId,
                    device.Name,
                    device.Connections.Select(connection =>
                            new ConnectionPresenceDto(
                                connection.ConnectionId,
                                connection.ConnectedAt))
                        .ToArray(),
                    device.IsOnline))
                .ToArray(),
            state.ActiveDeviceId,
            state.AudioOwnerConnectionId,
            state.Version);

    public static ConnectSnapshot ToDto(
        Guid userId,
        ConnectSnapshotState snapshot,
        DateTimeOffset serverTime) =>
        new(
            userId,
            ToDto(snapshot.Player),
            ToDto(snapshot.Queue),
            ToDto(snapshot.Presence),
            serverTime);
}
