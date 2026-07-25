using Connect.Domain.Queue;
using Shouldly;

namespace Connect.Domain.UnitTests.Queue;

public sealed class QueueStateTests
{
    [Fact]
    public void Add_CreatesStableQueueItemAndCanonicalOrder()
    {
        var queue = new QueueState();
        var trackId = Guid.NewGuid();

        QueueItem item = queue.Add(trackId);

        item.QueueItemId.ShouldNotBe(Guid.Empty);
        item.TrackId.ShouldBe(trackId);
        item.CanonicalOrder.ShouldBe(0);
        queue.Items.ShouldHaveSingleItem();
        queue.Version.ShouldBe(1);
    }

    [Fact]
    public void Add_DuplicateTrack_CreatesDistinctQueueItems()
    {
        var queue = new QueueState();
        var trackId = Guid.NewGuid();

        QueueItem first = queue.Add(trackId);
        QueueItem second = queue.Add(trackId);

        first.QueueItemId.ShouldNotBe(second.QueueItemId);
        queue.Items.Select(item => item.TrackId).ShouldAllBe(value => value == trackId);
    }

    [Fact]
    public void Select_UsesQueueItemIdentity_AndRepeatedSelectIsNoOp()
    {
        var queue = new QueueState();
        QueueItem item = queue.Add(Guid.NewGuid());
        long versionAfterAdd = queue.Version;

        queue.Select(item.QueueItemId).ShouldBeTrue();
        queue.Select(item.QueueItemId).ShouldBeFalse();

        queue.CurrentItem.ShouldBe(item);
        queue.Version.ShouldBe(versionAfterAdd + 1);
    }

    [Fact]
    public void Remove_NonCurrentItem_PreservesCurrentItem()
    {
        QueueState queue = QueueWithThreeSelectedAt(1);
        QueueItem current = queue.CurrentItem!;
        QueueItem first = queue.Items[0];

        RemoveQueueItemResult result = queue.Remove(first.QueueItemId);

        result.Removed.ShouldBeTrue();
        result.RemovedCurrentItem.ShouldBeFalse();
        result.CurrentItem?.QueueItemId.ShouldBe(current.QueueItemId);
        result.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Remove_CurrentItem_SelectsItemAtSameIndex()
    {
        QueueState queue = QueueWithThreeSelectedAt(1);
        QueueItem removed = queue.CurrentItem!;
        QueueItem expected = queue.Items[2];

        RemoveQueueItemResult result = queue.Remove(removed.QueueItemId);

        result.RemovedCurrentItem.ShouldBeTrue();
        result.CurrentItem?.QueueItemId.ShouldBe(expected.QueueItemId);
        queue.CurrentItem?.QueueItemId.ShouldBe(expected.QueueItemId);
    }

    [Fact]
    public void Remove_FinalCurrentItem_EmptiesQueue()
    {
        var queue = new QueueState();
        QueueItem item = queue.Add(Guid.NewGuid());
        queue.Select(item.QueueItemId);

        RemoveQueueItemResult result = queue.Remove(item.QueueItemId);

        result.RemovedCurrentItem.ShouldBeTrue();
        result.IsEmpty.ShouldBeTrue();
        result.CurrentItem.ShouldBeNull();
        queue.CurrentQueueItemId.ShouldBeNull();
    }

    [Fact]
    public void Reorder_ChangesOrderAndCanonicalOrder()
    {
        QueueState queue = QueueWithThreeSelectedAt(1);
        QueueItem last = queue.Items[2];

        queue.Reorder(last.QueueItemId, 0).ShouldBeTrue();

        queue.Items[0].QueueItemId.ShouldBe(last.QueueItemId);
        queue.Items.Select(item => item.CanonicalOrder).ShouldBe([0L, 1L, 2L]);
    }

    [Fact]
    public void Shuffle_PreservesCurrentAndCanonicalOrder()
    {
        QueueState queue = QueueWithThreeSelectedAt(1);
        Guid currentId = queue.CurrentQueueItemId!.Value;
        QueueItem[] canonical = [.. queue.Items];
        Guid[] shuffledIds =
        [
            canonical[2].QueueItemId,
            canonical[0].QueueItemId,
            canonical[1].QueueItemId
        ];

        queue.Shuffle(shuffledIds).ShouldBeTrue();

        queue.IsShuffled.ShouldBeTrue();
        queue.CurrentQueueItemId.ShouldBe(currentId);
        queue.Items.Select(item => item.QueueItemId).ShouldBe(shuffledIds);
        foreach (QueueItem item in canonical)
        {
            queue.Items.Single(candidate => candidate.QueueItemId == item.QueueItemId)
                .CanonicalOrder.ShouldBe(item.CanonicalOrder);
        }
    }

    [Fact]
    public void Shuffle_EmptyQueue_IsNoOp()
    {
        var queue = new QueueState();

        queue.Shuffle([]).ShouldBeFalse();

        queue.IsShuffled.ShouldBeFalse();
        queue.Version.ShouldBe(0);
    }

    [Fact]
    public void Shuffle_SingleItem_EnablesModeAndRepeatedShuffleIsNoOp()
    {
        var queue = new QueueState();
        QueueItem item = queue.Add(Guid.NewGuid());
        queue.Select(item.QueueItemId);
        long version = queue.Version;

        queue.Shuffle([item.QueueItemId]).ShouldBeTrue();
        queue.Shuffle([item.QueueItemId]).ShouldBeFalse();

        queue.IsShuffled.ShouldBeTrue();
        queue.CurrentQueueItemId.ShouldBe(item.QueueItemId);
        queue.Items.ShouldHaveSingleItem().CanonicalOrder.ShouldBe(item.CanonicalOrder);
        queue.Version.ShouldBe(version + 1);

        queue.Unshuffle().ShouldBeTrue();
        queue.IsShuffled.ShouldBeFalse();
        queue.CurrentQueueItemId.ShouldBe(item.QueueItemId);
        queue.Items.ShouldHaveSingleItem().CanonicalOrder.ShouldBe(item.CanonicalOrder);
        queue.Version.ShouldBe(version + 2);
    }

    [Fact]
    public void Unshuffle_RestoresCanonicalOrderAndPreservesCurrent()
    {
        QueueState queue = QueueWithThreeSelectedAt(1);
        Guid currentId = queue.CurrentQueueItemId!.Value;
        Guid[] canonicalIds = [.. queue.Items.Select(item => item.QueueItemId)];
        queue.Shuffle([canonicalIds[2], canonicalIds[0], canonicalIds[1]]);

        queue.Unshuffle().ShouldBeTrue();

        queue.Items.Select(item => item.QueueItemId).ShouldBe(canonicalIds);
        queue.CurrentQueueItemId.ShouldBe(currentId);
        queue.IsShuffled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(RepeatMode.None, false, true)]
    [InlineData(RepeatMode.Queue, true, false)]
    [InlineData(RepeatMode.Track, false, false)]
    public void Next_AtEnd_FollowsRepeatMode(
        RepeatMode repeatMode,
        bool expectedChange,
        bool expectedPause)
    {
        QueueState queue = QueueWithThreeSelectedAt(2);
        Guid originalId = queue.CurrentQueueItemId!.Value;
        queue.SetRepeatMode(repeatMode);

        QueueNavigationResult result = queue.Next();

        result.CurrentItemChanged.ShouldBe(expectedChange);
        result.ShouldPause.ShouldBe(expectedPause);
        if (repeatMode == RepeatMode.Queue)
        {
            queue.CurrentQueueItemId.ShouldBe(queue.Items[0].QueueItemId);
        }
        else
        {
            queue.CurrentQueueItemId.ShouldBe(originalId);
        }
    }

    [Fact]
    public void Previous_AtStart_WithQueueRepeat_WrapsToEnd()
    {
        QueueState queue = QueueWithThreeSelectedAt(0);
        queue.SetRepeatMode(RepeatMode.Queue);

        QueueNavigationResult result = queue.Previous();

        result.CurrentItemChanged.ShouldBeTrue();
        queue.CurrentQueueItemId.ShouldBe(queue.Items[^1].QueueItemId);
    }

    [Theory]
    [InlineData(RepeatMode.None)]
    [InlineData(RepeatMode.Track)]
    public void Previous_AtStart_WithoutQueueRepeat_StaysAtStart(RepeatMode repeatMode)
    {
        QueueState queue = QueueWithThreeSelectedAt(0);
        queue.SetRepeatMode(repeatMode);
        Guid originalId = queue.CurrentQueueItemId!.Value;

        QueueNavigationResult result = queue.Previous();

        result.CurrentItemChanged.ShouldBeFalse();
        result.ShouldResetPosition.ShouldBeTrue();
        queue.CurrentQueueItemId.ShouldBe(originalId);
    }

    [Fact]
    public void NoOp_DoesNotIncrementVersion()
    {
        var queue = new QueueState();
        QueueItem item = queue.Add(Guid.NewGuid());
        queue.Select(item.QueueItemId);
        long version = queue.Version;

        queue.Select(item.QueueItemId).ShouldBeFalse();
        queue.SetRepeatMode(RepeatMode.None).ShouldBeFalse();
        queue.Unshuffle().ShouldBeFalse();

        queue.Version.ShouldBe(version);
    }

    [Fact]
    public void Restore_RejectsUndefinedRepeatMode()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            QueueState.Restore([], null, (RepeatMode)999, false, 0));
    }

    [Fact]
    public void SetRepeatMode_RejectsUndefinedValueWithoutMutation()
    {
        var queue = new QueueState();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            queue.SetRepeatMode((RepeatMode)999));

        queue.RepeatMode.ShouldBe(RepeatMode.None);
        queue.Version.ShouldBe(0);
    }

    [Fact]
    public void Remove_UnknownItem_IsNoOp()
    {
        var queue = new QueueState();
        queue.Add(Guid.NewGuid());
        long version = queue.Version;

        RemoveQueueItemResult result = queue.Remove(Guid.NewGuid());

        result.Removed.ShouldBeFalse();
        queue.Version.ShouldBe(version);
        queue.Items.Count.ShouldBe(1);
    }

    [Fact]
    public void Add_RepeatedIdentityAndTrack_IsNoOp()
    {
        var queue = new QueueState();
        var queueItemId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        QueueItem original = queue.Add(trackId, queueItemId);
        long version = queue.Version;

        QueueItem repeated = queue.Add(trackId, queueItemId);

        repeated.ShouldBeSameAs(original);
        queue.Version.ShouldBe(version);
        queue.Items.Count.ShouldBe(1);
    }

    [Fact]
    public void Add_RepeatedIdentityWithDifferentTrack_IsRejected()
    {
        var queue = new QueueState();
        var queueItemId = Guid.NewGuid();
        queue.Add(Guid.NewGuid(), queueItemId);

        Should.Throw<InvalidOperationException>(() =>
            queue.Add(Guid.NewGuid(), queueItemId));
    }

    private static QueueState QueueWithThreeSelectedAt(int selectedIndex)
    {
        var queue = new QueueState();
        queue.Add(Guid.NewGuid());
        queue.Add(Guid.NewGuid());
        queue.Add(Guid.NewGuid());
        queue.Select(queue.Items[selectedIndex].QueueItemId);
        return queue;
    }
}
