namespace Connect.Contracts.Commands;

public sealed record ConnectCommand<TPayload>(
    Guid CommandId,
    Guid DeviceId,
    TPayload Payload);

public sealed record RegisterConnectionRequest(
    Guid DeviceId,
    string DeviceName,
    int ProtocolVersion);

public sealed record PlayPayload(long ExpectedPlayerVersion);

public sealed record PausePayload(long ExpectedPlayerVersion);

public sealed record SeekPayload(
    long PositionMs,
    long ExpectedPlayerVersion);

public sealed record ChangeVolumePayload(
    int VolumePercent,
    long ExpectedPlayerVersion);

public sealed record SelectActiveDevicePayload(
    Guid TargetDeviceId,
    long ExpectedPresenceVersion);

public sealed record SelectTrackPayload(
    Guid QueueItemId,
    long ExpectedQueueVersion,
    long ExpectedPlayerVersion);

public sealed record AddQueueItemPayload(
    Guid TrackId,
    long ExpectedQueueVersion);

public sealed record RemoveQueueItemPayload(
    Guid QueueItemId,
    long ExpectedQueueVersion,
    long ExpectedPlayerVersion);

public sealed record ReorderQueueItemPayload(
    Guid QueueItemId,
    int NewIndex,
    long ExpectedQueueVersion);

public sealed record NextPayload(
    long ExpectedQueueVersion,
    long ExpectedPlayerVersion);

public sealed record PreviousPayload(
    long ExpectedQueueVersion,
    long ExpectedPlayerVersion);

public sealed record ShuffleQueuePayload(
    IReadOnlyList<Guid> OrderedQueueItemIds,
    long ExpectedQueueVersion);

public sealed record UnshuffleQueuePayload(long ExpectedQueueVersion);

public sealed record SetRepeatModePayload(
    RepeatModeContract RepeatMode,
    long ExpectedQueueVersion);

public enum RepeatModeContract
{
    None,
    Queue,
    Track
}
