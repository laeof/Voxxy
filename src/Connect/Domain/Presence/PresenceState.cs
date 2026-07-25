namespace Connect.Domain.Presence;

public sealed class PresenceState
{
    private readonly List<Device> _devices;

    public IReadOnlyList<Device> Devices { get; }
    public Guid? ActiveDeviceId { get; private set; }
    public string? AudioOwnerConnectionId { get; private set; }
    public long Version { get; private set; }

    public PresenceState()
        : this([], null, null, 0)
    {
    }

    private PresenceState(
        IEnumerable<Device> devices,
        Guid? activeDeviceId,
        string? audioOwnerConnectionId,
        long version)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        _devices = [.. devices.Select(device => device.Copy())];
        Devices = _devices.AsReadOnly();
        if (_devices.Select(device => device.DeviceId).Distinct().Count() != _devices.Count)
        {
            throw new ArgumentException("Device IDs must be unique.", nameof(devices));
        }

        ValidateUniqueConnectionIds(_devices);

        if (activeDeviceId is not null &&
            _devices.All(device => device.DeviceId != activeDeviceId))
        {
            throw new ArgumentException("Active device must belong to this presence state.");
        }

        if (audioOwnerConnectionId is not null &&
            (activeDeviceId is null ||
             FindDevice(activeDeviceId.Value)?.HasConnection(audioOwnerConnectionId) != true))
        {
            throw new ArgumentException(
                "Audio owner must be a connection of the active device.",
                nameof(audioOwnerConnectionId));
        }

        ActiveDeviceId = activeDeviceId;
        AudioOwnerConnectionId = audioOwnerConnectionId;
        Version = version;
    }

    public static PresenceState Restore(
        IEnumerable<Device> devices,
        Guid? activeDeviceId,
        string? audioOwnerConnectionId,
        long version) =>
        new(devices, activeDeviceId, audioOwnerConnectionId, version);

    public bool RegisterConnection(
        Guid deviceId,
        string deviceName,
        string connectionId,
        DateTimeOffset connectedAt)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("Connection ID is required.", nameof(connectionId));
        }

        Device? connectionOwner = FindDeviceByConnection(connectionId);
        if (connectionOwner is not null && connectionOwner.DeviceId != deviceId)
        {
            throw new InvalidOperationException(
                $"Connection '{connectionId}' already belongs to another device.");
        }

        Device? device = FindDevice(deviceId);
        bool changed;

        if (device is null)
        {
            device = new Device(deviceId, deviceName);
            _devices.Add(device);
            changed = true;
        }
        else
        {
            changed = device.Rename(deviceName);
        }

        changed |= device.RegisterConnection(connectionId, connectedAt);

        if (ActiveDeviceId == deviceId)
        {
            changed |= EnsureAudioOwner();
        }

        if (changed)
        {
            IncrementVersion();
        }

        return changed;
    }

    public SelectActiveDeviceResult SelectActiveDevice(Guid deviceId)
    {
        Device? device = FindDevice(deviceId);
        if (device is null)
        {
            return new SelectActiveDeviceResult(
                SelectActiveDeviceStatus.DeviceNotFound,
                ActiveDeviceId,
                AudioOwnerConnectionId);
        }

        if (!device.IsOnline)
        {
            return new SelectActiveDeviceResult(
                SelectActiveDeviceStatus.DeviceOffline,
                ActiveDeviceId,
                AudioOwnerConnectionId);
        }

        string owner = SelectAudioOwner(device);
        if (ActiveDeviceId == deviceId && AudioOwnerConnectionId == owner)
        {
            return new SelectActiveDeviceResult(
                SelectActiveDeviceStatus.NoOp,
                ActiveDeviceId,
                AudioOwnerConnectionId);
        }

        ActiveDeviceId = deviceId;
        AudioOwnerConnectionId = owner;
        IncrementVersion();
        return new SelectActiveDeviceResult(
            SelectActiveDeviceStatus.Selected,
            ActiveDeviceId,
            AudioOwnerConnectionId);
    }

    public DisconnectConnectionResult DisconnectConnection(string connectionId)
    {
        Device? device = FindDeviceByConnection(connectionId);
        if (device is null)
        {
            return new DisconnectConnectionResult(false, false, false, null, AudioOwnerConnectionId);
        }

        bool wasAudioOwner = AudioOwnerConnectionId == connectionId;
        device.RemoveConnection(connectionId);

        bool activeDeviceLost = ActiveDeviceId == device.DeviceId && !device.IsOnline;
        if (activeDeviceLost)
        {
            ActiveDeviceId = null;
            AudioOwnerConnectionId = null;
        }
        else if (ActiveDeviceId == device.DeviceId && wasAudioOwner)
        {
            AudioOwnerConnectionId = SelectAudioOwner(device);
        }

        IncrementVersion();
        return new DisconnectConnectionResult(
            true,
            activeDeviceLost,
            wasAudioOwner,
            device.DeviceId,
            AudioOwnerConnectionId);
    }

    public ExpireConnectionsResult ExpireConnections(
        IReadOnlyCollection<string> expiredConnectionIds)
    {
        ArgumentNullException.ThrowIfNull(expiredConnectionIds);

        var uniqueExpiredIds = new HashSet<string>(
            expiredConnectionIds.Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.Ordinal);
        bool ownerWasRemoved =
            AudioOwnerConnectionId is not null &&
            uniqueExpiredIds.Contains(AudioOwnerConnectionId) &&
            FindDeviceByConnection(AudioOwnerConnectionId) is not null;
        int removedCount = _devices.Sum(device =>
            device.RemoveConnections(uniqueExpiredIds));

        if (removedCount == 0)
        {
            return new ExpireConnectionsResult(0, false, false, AudioOwnerConnectionId);
        }

        Device? activeDevice = ActiveDeviceId is Guid activeId ? FindDevice(activeId) : null;
        bool activeDeviceLost = activeDevice is not null && !activeDevice.IsOnline;
        if (activeDeviceLost)
        {
            ActiveDeviceId = null;
            AudioOwnerConnectionId = null;
        }
        else if (activeDevice is not null && ownerWasRemoved)
        {
            AudioOwnerConnectionId = SelectAudioOwner(activeDevice);
        }

        IncrementVersion();
        return new ExpireConnectionsResult(
            removedCount,
            activeDeviceLost,
            ownerWasRemoved,
            AudioOwnerConnectionId);
    }

    private bool EnsureAudioOwner()
    {
        Device activeDevice = FindDevice(ActiveDeviceId!.Value)!;
        if (AudioOwnerConnectionId is not null &&
            activeDevice.HasConnection(AudioOwnerConnectionId))
        {
            return false;
        }

        string? nextOwner = activeDevice.IsOnline ? SelectAudioOwner(activeDevice) : null;
        if (AudioOwnerConnectionId == nextOwner)
        {
            return false;
        }

        AudioOwnerConnectionId = nextOwner;
        return true;
    }

    private static string SelectAudioOwner(Device device) =>
        device.Connections
            .OrderBy(connection => connection.ConnectedAt)
            .ThenBy(connection => connection.ConnectionId, StringComparer.Ordinal)
            .First()
            .ConnectionId;

    private static void ValidateUniqueConnectionIds(IReadOnlyCollection<Device> devices)
    {
        string[] connectionIds =
        [
            .. devices.SelectMany(device => device.Connections)
                .Select(connection => connection.ConnectionId)
        ];

        if (connectionIds.Distinct(StringComparer.Ordinal).Count() != connectionIds.Length)
        {
            throw new ArgumentException(
                "Connection IDs must be unique across all devices.",
                nameof(devices));
        }
    }

    private Device? FindDevice(Guid deviceId) =>
        _devices.FirstOrDefault(device => device.DeviceId == deviceId);

    private Device? FindDeviceByConnection(string connectionId) =>
        _devices.FirstOrDefault(device => device.HasConnection(connectionId));

    private void IncrementVersion() => Version++;
}
