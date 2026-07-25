namespace Connect.Contracts.States;

public sealed record ConnectionPresenceDto(
    string ConnectionId,
    DateTimeOffset ConnectedAt);

public sealed record DevicePresenceDto(
    Guid DeviceId,
    string Name,
    IReadOnlyList<ConnectionPresenceDto> Connections,
    bool IsOnline);

public sealed record PresenceStateDto(
    IReadOnlyList<DevicePresenceDto> Devices,
    Guid? ActiveDeviceId,
    string? AudioOwnerConnectionId,
    long Version);
