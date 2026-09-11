namespace Connect.Domain.Queue;

public sealed record RemoveQueueItemResult(
    bool Removed,
    bool RemovedCurrentItem,
    QueueItem? CurrentItem,
    bool IsEmpty);

public sealed record QueueNavigationResult(
    QueueItem? CurrentItem,
    bool CurrentItemChanged,
    bool ShouldResetPosition,
    bool ShouldPause);
