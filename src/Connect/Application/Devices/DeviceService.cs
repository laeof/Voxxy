using Connect.Application.Abstractions.Repositories;
using Connect.Application.Abstractions.Services;
using Connect.Domain.Devices;

namespace Connect.Application.Devices;

public sealed class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<Device> GetDevicesAsync(Guid userId)
    {
        Device device = await _deviceRepository.GetValueAsync(userId);

        return device;
    }

    public async Task<DeviceItem?> GetDeviceItemAsync(Guid userId, Guid deviceId)
    {
        Device device = await _deviceRepository.GetValueAsync(userId);

        return device.Items.FirstOrDefault(d => d.Id == deviceId);
    }

    public async Task<DeviceItem> AddDeviceAsync(Guid userId, DeviceItem deviceItem)
    {
        Device device = await _deviceRepository.GetValueAsync(userId);

        device ??= new Device
        {
            UserId = userId
        };

        device.AddDevice(deviceItem);

        await _deviceRepository.SetValueAsync(userId, device);

        return deviceItem;
    }

    public async Task RemoveDeviceAsync(Guid userId, Guid deviceId)
    {
        Device device = await _deviceRepository.GetValueAsync(userId);

        if (device is null)
        {
            return;
        }

        device.RemoveDevice(deviceId);

        await _deviceRepository.SetValueAsync(userId, device);
    }

    public async Task ConnectDeviceAsync(Guid userId, Guid deviceId, string connectionId)
    {
        Device device = await _deviceRepository.GetValueAsync(userId);

        if (device is null)
        {
            return;
        }

        device.ConnectDevice(deviceId, connectionId);

        await _deviceRepository.SetValueAsync(userId, device);
    }

    public async Task DisconnectDeviceAsync(Guid userId, string connectionId)
    {
        Device device = await _deviceRepository.GetValueAsync(userId);

        if (device is null)
        {
            return;
        }

        device.DisconnectDevice(connectionId);

        await _deviceRepository.SetValueAsync(userId, device);
    }

    public async Task<Device> GetOnlineDevicesAsync(Guid userId)
    {
        Device device = await _deviceRepository.GetValueAsync(userId);

        if (device is null)
        {
            return default;
        }

        device.Items = [.. device.Items.Where(x => x.ConnectionId != null)];

        return device;
    }
}