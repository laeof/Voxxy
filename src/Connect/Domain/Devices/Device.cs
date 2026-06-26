namespace Connect.Domain.Devices;

public sealed class Device
{
    public Guid UserId { get; set; }
    public List<DeviceItem> Items { get; set; } = [];

    public void AddDevice(DeviceItem device)
    {
        Items.Add(device);
    }

    public void RemoveDevice(Guid deviceId)
    {
        Items.RemoveAll(x => x.Id == deviceId);
    }

    public void ConnectDevice(Guid deviceId, string connectionId)
    {
        DeviceItem device = Items.FirstOrDefault(x => x.Id == deviceId);

        if (device is null)
        {
            return;
        }

        device.ConnectionId = connectionId;
    }

    public void DisconnectDevice(string connectionId)
    {
        DeviceItem device = Items.FirstOrDefault(x => x.ConnectionId == connectionId);

        if (device is null)
        {
            return;
        }

        device.ConnectionId = null;
    }
}

public sealed class DeviceItem
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? ConnectionId { get; set; }
}