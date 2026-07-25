using Connect.Contracts.States;

namespace Connect.ValidationTests;

internal static class ConnectStateInvariantValidator
{
    public static IReadOnlyList<string> Validate(ConnectSnapshot snapshot)
    {
        var failures = new List<string>();
        if (snapshot.Player.Version < 0 ||
            snapshot.Queue.Version < 0 ||
            snapshot.Presence.Version < 0)
        {
            failures.Add("Versions must be non-negative.");
        }
        if (snapshot.Queue.Items.Select(item => item.QueueItemId).Distinct().Count() !=
            snapshot.Queue.Items.Count)
        {
            failures.Add("Queue item IDs must be unique.");
        }
        if (snapshot.Queue.CurrentQueueItemId is Guid current &&
            snapshot.Queue.Items.All(item => item.QueueItemId != current))
        {
            failures.Add("Current queue item must exist.");
        }
        if (snapshot.Presence.ActiveDeviceId is Guid active &&
            snapshot.Presence.Devices.All(device => device.DeviceId != active))
        {
            failures.Add("Active device must exist.");
        }
        if (snapshot.Presence.AudioOwnerConnectionId is string owner &&
            snapshot.Presence.Devices
                .SelectMany(device => device.Connections)
                .All(connection => connection.ConnectionId != owner))
        {
            failures.Add("Audio owner connection must be online.");
        }
        return failures;
    }
}
