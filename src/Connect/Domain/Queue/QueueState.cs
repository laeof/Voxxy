namespace Connect.Domain.Queue;

public sealed class QueueState
{
    private readonly List<QueueItem> _items;

    public IReadOnlyList<QueueItem> Items => _items;
    public Guid? CurrentQueueItemId { get; private set; }
    public RepeatMode RepeatMode { get; private set; }
    public bool IsShuffled { get; private set; }
    public long Version { get; private set; }
    public QueueItem? CurrentItem =>
        CurrentQueueItemId is Guid currentId
            ? _items.FirstOrDefault(item => item.QueueItemId == currentId)
            : null;

    public QueueState()
        : this([], null, RepeatMode.None, false, 0)
    {
    }

    private QueueState(
        IEnumerable<QueueItem> items,
        Guid? currentQueueItemId,
        RepeatMode repeatMode,
        bool isShuffled,
        long version)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        ValidateRepeatMode(repeatMode);

        _items = [.. items];
        ValidateItems(_items);

        if (currentQueueItemId is not null &&
            _items.All(item => item.QueueItemId != currentQueueItemId))
        {
            throw new ArgumentException(
                "Current queue item must belong to the queue.",
                nameof(currentQueueItemId));
        }

        CurrentQueueItemId = currentQueueItemId;
        RepeatMode = repeatMode;
        IsShuffled = isShuffled;
        Version = version;
    }

    public static QueueState Restore(
        IEnumerable<QueueItem> items,
        Guid? currentQueueItemId,
        RepeatMode repeatMode,
        bool isShuffled,
        long version) =>
        new(items, currentQueueItemId, repeatMode, isShuffled, version);

    public QueueItem Add(Guid trackId) => Add(trackId, Guid.NewGuid());

    public QueueItem Add(Guid trackId, Guid queueItemId)
    {
        if (trackId == Guid.Empty)
        {
            throw new ArgumentException("Track ID is required.", nameof(trackId));
        }

        if (queueItemId == Guid.Empty)
        {
            throw new ArgumentException("Queue item ID is required.", nameof(queueItemId));
        }

        QueueItem? existing = _items.FirstOrDefault(
            item => item.QueueItemId == queueItemId);
        if (existing is not null && existing.TrackId == trackId)
        {
            return existing;
        }

        if (existing is not null)
        {
            throw new InvalidOperationException("Queue item ID must be unique.");
        }

        long canonicalOrder = _items.Count == 0
            ? 0
            : checked(_items.Max(item => item.CanonicalOrder) + 1);
        var item = new QueueItem(queueItemId, trackId, canonicalOrder);

        _items.Add(item);
        IncrementVersion();
        return item;
    }

    public bool Select(Guid queueItemId)
    {
        EnsureItemExists(queueItemId);

        if (CurrentQueueItemId == queueItemId)
        {
            return false;
        }

        CurrentQueueItemId = queueItemId;
        IncrementVersion();
        return true;
    }

    public RemoveQueueItemResult Remove(Guid queueItemId)
    {
        int index = _items.FindIndex(item => item.QueueItemId == queueItemId);
        if (index < 0)
        {
            return new RemoveQueueItemResult(
                false,
                false,
                CurrentItem,
                _items.Count == 0);
        }

        bool removedCurrentItem = CurrentQueueItemId == queueItemId;

        _items.RemoveAt(index);

        if (removedCurrentItem)
        {
            CurrentQueueItemId = _items.Count == 0
                ? null
                : _items[Math.Min(index, _items.Count - 1)].QueueItemId;
        }

        if (!IsShuffled)
        {
            NormalizeCanonicalOrder();
        }

        IncrementVersion();
        return new RemoveQueueItemResult(true, removedCurrentItem, CurrentItem, _items.Count == 0);
    }

    public bool Reorder(Guid queueItemId, int newIndex)
    {
        if (newIndex < 0 || newIndex >= _items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        }

        int oldIndex = IndexOf(queueItemId);
        if (oldIndex == newIndex)
        {
            return false;
        }

        QueueItem item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);

        if (!IsShuffled)
        {
            NormalizeCanonicalOrder();
        }

        IncrementVersion();
        return true;
    }

    public QueueNavigationResult Next()
    {
        if (_items.Count == 0)
        {
            return new QueueNavigationResult(null, false, false, true);
        }

        if (CurrentItem is null)
        {
            CurrentQueueItemId = _items[0].QueueItemId;
            IncrementVersion();
            return new QueueNavigationResult(CurrentItem, true, true, false);
        }

        if (RepeatMode == RepeatMode.Track)
        {
            return new QueueNavigationResult(CurrentItem, false, true, false);
        }

        Guid currentId = CurrentQueueItemId
            ?? throw new InvalidOperationException("Current queue item is missing.");
        int currentIndex = IndexOf(currentId);
        if (currentIndex < _items.Count - 1)
        {
            CurrentQueueItemId = _items[currentIndex + 1].QueueItemId;
            IncrementVersion();
            return new QueueNavigationResult(CurrentItem, true, true, false);
        }

        if (RepeatMode == RepeatMode.Queue)
        {
            CurrentQueueItemId = _items[0].QueueItemId;
            IncrementVersion();
            return new QueueNavigationResult(CurrentItem, true, true, false);
        }

        return new QueueNavigationResult(CurrentItem, false, false, true);
    }

    public QueueNavigationResult Previous()
    {
        if (_items.Count == 0)
        {
            return new QueueNavigationResult(null, false, false, false);
        }

        if (CurrentItem is null)
        {
            CurrentQueueItemId = _items[0].QueueItemId;
            IncrementVersion();
            return new QueueNavigationResult(CurrentItem, true, true, false);
        }

        Guid currentId = CurrentQueueItemId
            ?? throw new InvalidOperationException("Current queue item is missing.");
        int currentIndex = IndexOf(currentId);
        if (currentIndex > 0)
        {
            CurrentQueueItemId = _items[currentIndex - 1].QueueItemId;
            IncrementVersion();
            return new QueueNavigationResult(CurrentItem, true, true, false);
        }

        if (RepeatMode == RepeatMode.Queue)
        {
            CurrentQueueItemId = _items[^1].QueueItemId;
            IncrementVersion();
            return new QueueNavigationResult(CurrentItem, true, true, false);
        }

        return new QueueNavigationResult(CurrentItem, false, true, false);
    }

    public bool Shuffle(IReadOnlyCollection<Guid> orderedQueueItemIds)
    {
        ArgumentNullException.ThrowIfNull(orderedQueueItemIds);
        ValidatePermutation(orderedQueueItemIds);

        if (_items.Count == 0)
        {
            return false;
        }

        var reordered = orderedQueueItemIds
            .Select(id => _items.Single(item => item.QueueItemId == id))
            .ToList();

        bool orderChanged = !_items.Select(item => item.QueueItemId)
            .SequenceEqual(reordered.Select(item => item.QueueItemId));
        if (IsShuffled && !orderChanged)
        {
            return false;
        }

        _items.Clear();
        _items.AddRange(reordered);
        IsShuffled = true;
        IncrementVersion();
        return true;
    }

    public bool Unshuffle()
    {
        if (!IsShuffled)
        {
            return false;
        }

        _items.Sort((left, right) => left.CanonicalOrder.CompareTo(right.CanonicalOrder));
        IsShuffled = false;
        IncrementVersion();
        return true;
    }

    public bool SetRepeatMode(RepeatMode repeatMode)
    {
        ValidateRepeatMode(repeatMode);

        if (RepeatMode == repeatMode)
        {
            return false;
        }

        RepeatMode = repeatMode;
        IncrementVersion();
        return true;
    }

    private static void ValidateItems(List<QueueItem> items)
    {
        if (items.Any(item => item.QueueItemId == Guid.Empty || item.TrackId == Guid.Empty))
        {
            throw new ArgumentException("Queue and track IDs are required.", nameof(items));
        }

        if (items.Select(item => item.QueueItemId).Distinct().Count() != items.Count)
        {
            throw new ArgumentException("Queue item IDs must be unique.", nameof(items));
        }

        if (items.Select(item => item.CanonicalOrder).Distinct().Count() != items.Count)
        {
            throw new ArgumentException("Canonical order values must be unique.", nameof(items));
        }
    }

    private static void ValidateRepeatMode(RepeatMode repeatMode)
    {
        if (!Enum.IsDefined(repeatMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(repeatMode),
                repeatMode,
                "Repeat mode is not defined.");
        }
    }

    private void ValidatePermutation(IReadOnlyCollection<Guid> orderedQueueItemIds)
    {
        if (orderedQueueItemIds.Count != _items.Count ||
            orderedQueueItemIds.Distinct().Count() != _items.Count ||
            orderedQueueItemIds.Any(id => _items.All(item => item.QueueItemId != id)))
        {
            throw new ArgumentException(
                "Shuffled order must contain every queue item exactly once.",
                nameof(orderedQueueItemIds));
        }
    }

    private void EnsureItemExists(Guid queueItemId) => _ = IndexOf(queueItemId);

    private int IndexOf(Guid queueItemId)
    {
        int index = _items.FindIndex(item => item.QueueItemId == queueItemId);
        return index >= 0
            ? index
            : throw new KeyNotFoundException($"Queue item '{queueItemId}' was not found.");
    }

    private void NormalizeCanonicalOrder()
    {
        for (int index = 0; index < _items.Count; index++)
        {
            QueueItem item = _items[index];
            _items[index] = item with { CanonicalOrder = index };
        }
    }

    private void IncrementVersion() => Version++;
}
