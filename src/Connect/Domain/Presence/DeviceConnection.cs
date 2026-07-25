namespace Connect.Domain.Presence;

public sealed record DeviceConnection(
    string ConnectionId,
    DateTimeOffset ConnectedAt);
