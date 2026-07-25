namespace Connect.Domain.Presence;

public sealed class Device
{
    private readonly List<DeviceConnection> _connections;

    public Guid DeviceId { get; }
    public string Name { get; private set; }
    public IReadOnlyList<DeviceConnection> Connections { get; }
    public bool IsOnline => _connections.Count > 0;

    public Device(Guid deviceId, string name)
        : this(deviceId, name, [])
    {
    }

    private Device(
        Guid deviceId,
        string name,
        IEnumerable<DeviceConnection> connections)
    {
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("Device ID is required.", nameof(deviceId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Device name is required.", nameof(name));
        }

        _connections =
        [
            .. connections.Select(connection => new DeviceConnection(
                connection.ConnectionId,
                connection.ConnectedAt))
        ];
        Connections = _connections.AsReadOnly();
        if (_connections.Any(connection => string.IsNullOrWhiteSpace(connection.ConnectionId)))
        {
            throw new ArgumentException("Connection ID is required.", nameof(connections));
        }

        if (_connections.Select(connection => connection.ConnectionId)
                .Distinct(StringComparer.Ordinal).Count() !=
            _connections.Count)
        {
            throw new ArgumentException("Connection IDs must be unique.", nameof(connections));
        }

        DeviceId = deviceId;
        Name = name;
    }

    public static Device Restore(
        Guid deviceId,
        string name,
        IEnumerable<DeviceConnection> connections) =>
        new(deviceId, name, connections);

    internal Device Copy() => new(DeviceId, Name, _connections);

    internal bool Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Device name is required.", nameof(name));
        }

        if (Name == name)
        {
            return false;
        }

        Name = name;
        return true;
    }

    internal bool RegisterConnection(
        string connectionId,
        DateTimeOffset connectedAt)
    {
        ValidateConnectionId(connectionId);

        if (_connections.Any(connection =>
                string.Equals(
                    connection.ConnectionId,
                    connectionId,
                    StringComparison.Ordinal)))
        {
            return false;
        }

        _connections.Add(new DeviceConnection(connectionId, connectedAt));
        return true;
    }

    internal bool RemoveConnection(string connectionId) =>
        _connections.RemoveAll(connection =>
            string.Equals(
                connection.ConnectionId,
                connectionId,
                StringComparison.Ordinal)) > 0;

    internal int RemoveConnections(IReadOnlySet<string> connectionIds) =>
        _connections.RemoveAll(connection => connectionIds.Contains(connection.ConnectionId));

    internal bool HasConnection(string connectionId) =>
        _connections.Any(connection =>
            string.Equals(
                connection.ConnectionId,
                connectionId,
                StringComparison.Ordinal));

    private static void ValidateConnectionId(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("Connection ID is required.", nameof(connectionId));
        }
    }
}
