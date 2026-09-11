namespace Connect.Application.Commands;

public sealed record RegisterConnectionCommand(
    Guid UserId,
    Guid CommandId,
    Guid DeviceId,
    string DeviceName,
    string ConnectionId,
    DateTimeOffset ServerTime);

public sealed record RefreshConnectionLeaseCommand(
    Guid UserId,
    string ConnectionId);

public sealed record DisconnectConnectionCommand(
    Guid UserId,
    Guid CommandId,
    string ConnectionId,
    DateTimeOffset ServerTime);

public sealed record ExpireConnectionsCommand(
    Guid UserId,
    Guid CommandId,
    IReadOnlyCollection<string> ConnectionIds,
    DateTimeOffset ServerTime);

public sealed record DetectMissingLeasesCommand(
    Guid UserId,
    Guid CommandId,
    DateTimeOffset ServerTime);

public sealed record SelectActiveDeviceCommand(
    Guid UserId,
    Guid CommandId,
    Guid DeviceId,
    string? ConnectionId,
    DateTimeOffset ServerTime);
