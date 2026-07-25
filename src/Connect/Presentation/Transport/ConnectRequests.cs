namespace Connect.Presentation.Transport;

public enum RepeatModeContract
{
    None,
    Queue,
    Track
}

public sealed record RegisterConnectionRequest(
    Guid CommandId,
    Guid DeviceId,
    string DeviceName);

public sealed record CommandRequest(Guid CommandId);

public sealed record SelectActiveDeviceRequest(Guid CommandId, Guid DeviceId);

public sealed record ChangePositionRequest(Guid CommandId, long PositionMs);

public sealed record ChangeVolumeRequest(Guid CommandId, int VolumePercent);

public sealed record AddQueueItemRequest(
    Guid CommandId,
    Guid QueueItemId,
    Guid TrackId);

public sealed record QueueItemRequest(Guid CommandId, Guid QueueItemId);

public sealed record MoveQueueItemRequest(
    Guid CommandId,
    Guid QueueItemId,
    int TargetIndex);

public sealed record ChangeRepeatModeRequest(
    Guid CommandId,
    RepeatModeContract RepeatMode);
