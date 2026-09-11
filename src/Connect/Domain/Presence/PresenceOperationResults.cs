namespace Connect.Domain.Presence;

public enum SelectActiveDeviceStatus
{
    Selected,
    NoOp,
    DeviceNotFound,
    DeviceOffline
}

public sealed record SelectActiveDeviceResult(
    SelectActiveDeviceStatus Status,
    Guid? ActiveDeviceId,
    string? AudioOwnerConnectionId);

public sealed record DisconnectConnectionResult(
    bool Removed,
    bool ActiveDeviceLost,
    bool AudioOwnerChanged,
    Guid? DeviceId,
    string? AudioOwnerConnectionId);

public sealed record ExpireConnectionsResult(
    int RemovedCount,
    bool ActiveDeviceLost,
    bool AudioOwnerChanged,
    string? AudioOwnerConnectionId);
