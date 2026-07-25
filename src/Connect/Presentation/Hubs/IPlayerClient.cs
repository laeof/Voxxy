using Connect.Domain.Devices;
using Connect.Domain.Player;
using Connect.Domain.Queue;
using Connect.Shared;

namespace Connect.Presentation.Hubs;

public interface IPlayerClient
{
    Task PlayerStateChanged(PlayerState state);
    Task VolumeChanged(int volumePercent);
    Task PositionChanged(int positionMs, DateTimeOffset updatedAt);
    Task QueuePlaybackChanged(QueuePlayback queue);
    Task ActiveDeviceChanged(string connectionId);
    Task DeviceListChanged(Device device);
}