namespace Connect.Domain.Queue;

public sealed record QueueItem(Guid QueueItemId, Guid TrackId, long CanonicalOrder);
