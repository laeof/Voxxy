using Connect.Domain.Devices;

namespace Connect.Application.Abstractions.Services;

public interface IDeviceService
{
    Task<Device> GetDevicesAsync(Guid userId);
    Task<Device> GetOnlineDevicesAsync(Guid userId);
    Task<DeviceItem> AddDeviceAsync(Guid userId, DeviceItem deviceItem);
    Task RemoveDeviceAsync(Guid userId, Guid deviceId);
    Task<DeviceItem?> GetDeviceItemAsync(Guid userId, Guid deviceId);
    Task ConnectDeviceAsync(Guid userId, Guid deviceId, string connectionId);
    Task DisconnectDeviceAsync(Guid userId, string connectionId);
}