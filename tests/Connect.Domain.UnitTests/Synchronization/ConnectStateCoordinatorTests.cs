using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;
using Connect.Domain.Synchronization;
using Shouldly;

namespace Connect.Domain.UnitTests.Synchronization;

public sealed class ConnectStateCoordinatorTests
{
    private static readonly DateTimeOffset Start =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ConnectStateCoordinator _coordinator = new();

    [Fact]
    public void SelectTrack_ChangesQueueAndResetsPlayerPosition()
    {
        var queue = new QueueState();
        QueueItem first = queue.Add(Guid.NewGuid());
        QueueItem second = queue.Add(Guid.NewGuid());
        queue.Select(first.QueueItemId);
        var player = PlayerState.Restore(false, 5_000, Start, 50, 1);

        _coordinator.SelectTrack(queue, player, second.QueueItemId, Start.AddSeconds(1));

        queue.CurrentItem.ShouldBe(second);
        player.PositionMs.ShouldBe(0);
    }

    [Fact]
    public void RemoveCurrentItem_SelectsReplacementAndResetsPosition()
    {
        var queue = new QueueState();
        QueueItem first = queue.Add(Guid.NewGuid());
        QueueItem second = queue.Add(Guid.NewGuid());
        queue.Select(first.QueueItemId);
        var player = PlayerState.Restore(true, 5_000, Start, 50, 1);

        RemoveQueueItemResult result = _coordinator.RemoveQueueItem(
            queue,
            player,
            first.QueueItemId,
            Start.AddSeconds(1));

        result.CurrentItem?.QueueItemId.ShouldBe(second.QueueItemId);
        player.PositionMs.ShouldBe(0);
        player.IsPlaying.ShouldBeTrue();
    }

    [Fact]
    public void RemoveFinalItem_ClearsAndPausesPlayer()
    {
        var queue = new QueueState();
        QueueItem item = queue.Add(Guid.NewGuid());
        queue.Select(item.QueueItemId);
        var player = PlayerState.Restore(true, 5_000, Start, 50, 1);

        _coordinator.RemoveQueueItem(
            queue,
            player,
            item.QueueItemId,
            Start.AddSeconds(1));

        queue.CurrentItem.ShouldBeNull();
        player.IsPlaying.ShouldBeFalse();
        player.PositionMs.ShouldBe(0);
    }

    [Fact]
    public void NextAtEnd_WithRepeatNone_PausesPlayer()
    {
        var queue = new QueueState();
        QueueItem item = queue.Add(Guid.NewGuid());
        queue.Select(item.QueueItemId);
        var player = PlayerState.Restore(true, 1_000, Start, 50, 1);

        QueueNavigationResult result = _coordinator.Next(
            queue,
            player,
            Start.AddSeconds(1));

        result.ShouldPause.ShouldBeTrue();
        player.IsPlaying.ShouldBeFalse();
        queue.CurrentItem.ShouldBe(item);
    }

    [Fact]
    public void CompleteCurrentTrack_WithNextItem_AdvancesAndResetsPlayer()
    {
        QueueState queue = QueueWithTwoSelectedAtFirst();
        Guid expected = queue.Items[1].QueueItemId;
        var player = PlayerState.Restore(true, 1_000, Start, 50, 4);

        bool handled = _coordinator.CompleteCurrentTrack(
            queue,
            player,
            queue.Items[0].QueueItemId,
            180_000,
            Start.AddSeconds(2));

        handled.ShouldBeTrue();
        queue.CurrentQueueItemId.ShouldBe(expected);
        player.IsPlaying.ShouldBeTrue();
        player.PositionMs.ShouldBe(0);
        player.Version.ShouldBe(5);
    }

    [Fact]
    public void CompleteCurrentTrack_FinalItem_PausesAtCompletedPosition()
    {
        var queue = new QueueState();
        QueueItem item = queue.Add(Guid.NewGuid());
        queue.Select(item.QueueItemId);
        var player = PlayerState.Restore(true, 0, Start, 50, 7);

        _coordinator.CompleteCurrentTrack(
            queue,
            player,
            item.QueueItemId,
            180_000,
            Start.AddMinutes(3));

        player.IsPlaying.ShouldBeFalse();
        player.PositionMs.ShouldBe(180_000);
        player.PositionUpdatedAt.ShouldBe(Start.AddMinutes(3));
        player.Version.ShouldBe(8);
    }

    [Theory]
    [InlineData(RepeatMode.Track, 1)]
    [InlineData(RepeatMode.Queue, 0)]
    public void CompleteCurrentTrack_RespectsRepeatMode(
        RepeatMode repeatMode,
        int expectedIndex)
    {
        QueueState queue = QueueWithTwoSelectedAtSecond();
        queue.SetRepeatMode(repeatMode);
        var player = PlayerState.Restore(true, 10_000, Start, 50, 2);

        _coordinator.CompleteCurrentTrack(
            queue,
            player,
            queue.Items[1].QueueItemId,
            180_000,
            Start.AddSeconds(1));

        int actualIndex = queue.Items
            .Select((item, index) => (item, index))
            .Single(value => value.item.QueueItemId == queue.CurrentQueueItemId)
            .index;
        actualIndex.ShouldBe(expectedIndex);
        player.IsPlaying.ShouldBeTrue();
        player.PositionMs.ShouldBe(0);
    }

    [Fact]
    public void StartPlaybackContext_ReplacingPausedContext_IncrementsPlayerVersionOnce()
    {
        QueueState queue = ContextQueue(Guid.NewGuid());
        var player = PlayerState.Restore(false, 9_000, Start, 50, 3);
        QueueItem[] replacement =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), 0),
            new(Guid.NewGuid(), Guid.NewGuid(), 1)
        ];

        _coordinator.StartPlaybackContext(
            queue,
            player,
            PlaybackSourceType.Album,
            Guid.NewGuid(),
            replacement,
            1,
            Start.AddSeconds(1));

        player.Version.ShouldBe(4);
        player.IsPlaying.ShouldBeTrue();
        player.PositionMs.ShouldBe(0);
        queue.CurrentQueueItemId.ShouldBe(replacement[1].QueueItemId);
    }

    [Fact]
    public void CompleteCurrentTrack_StaleQueueItem_DoesNothing()
    {
        QueueState queue = QueueWithTwoSelectedAtSecond();
        long queueVersion = queue.Version;
        var player = PlayerState.Restore(true, 1_000, Start, 50, 3);

        bool handled = _coordinator.CompleteCurrentTrack(
            queue,
            player,
            queue.Items[0].QueueItemId,
            180_000,
            Start.AddSeconds(2));

        handled.ShouldBeFalse();
        queue.Version.ShouldBe(queueVersion);
        player.Version.ShouldBe(3);
    }

    [Fact]
    public void StartPlaybackContext_ReplacesDifferentContextAndStartsFirstTrack()
    {
        var queue = new QueueState();
        QueueItem old = queue.Add(Guid.NewGuid());
        queue.ReplaceContext(
            PlaybackSourceType.Playlist,
            Guid.NewGuid(),
            [old],
            0);
        queue.SetRepeatMode(RepeatMode.Queue);
        queue.Shuffle([old.QueueItemId]);
        var player = PlayerState.Restore(false, 9_000, Start, 50, 2);
        var sourceId = Guid.NewGuid();
        QueueItem[] replacement =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), 0),
            new(Guid.NewGuid(), Guid.NewGuid(), 1)
        ];

        _coordinator.StartPlaybackContext(
            queue,
            player,
            PlaybackSourceType.Album,
            sourceId,
            replacement,
            null,
            Start.AddSeconds(1));

        queue.SourceId.ShouldBe(sourceId);
        queue.SourceType.ShouldBe(PlaybackSourceType.Album);
        queue.Items.ShouldBe(replacement);
        queue.CurrentQueueItemId.ShouldBe(replacement[0].QueueItemId);
        queue.IsShuffled.ShouldBeFalse();
        queue.RepeatMode.ShouldBe(RepeatMode.Queue);
        player.IsPlaying.ShouldBeTrue();
        player.PositionMs.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void StartPlaybackContext_UsesZeroBasedStartIndex(int startIndex)
    {
        var queue = new QueueState();
        QueueItem[] replacement =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), 42),
            new(Guid.NewGuid(), Guid.NewGuid(), 7)
        ];

        _coordinator.StartPlaybackContext(
            queue,
            new PlayerState(Start),
            PlaybackSourceType.Album,
            Guid.NewGuid(),
            replacement,
            startIndex,
            Start);

        queue.CurrentQueueItemId.ShouldBe(replacement[startIndex].QueueItemId);
    }

    [Fact]
    public void StartPlaybackContext_SamePlayingContext_IsNoOp()
    {
        var sourceId = Guid.NewGuid();
        QueueState queue = ContextQueue(sourceId);
        QueueItem[] originalItems = [.. queue.Items];
        long queueVersion = queue.Version;
        var player = PlayerState.Restore(true, 5_000, Start, 50, 3);

        _coordinator.StartPlaybackContext(
            queue,
            player,
            PlaybackSourceType.Playlist,
            sourceId,
            [new(Guid.NewGuid(), Guid.NewGuid(), 0)],
            null,
            Start.AddSeconds(1));

        queue.Items.ShouldBe(originalItems);
        queue.Version.ShouldBe(queueVersion);
        player.Version.ShouldBe(3);
    }

    [Fact]
    public void StartPlaybackContext_SamePausedContext_ResumesWithoutReset()
    {
        var sourceId = Guid.NewGuid();
        QueueState queue = ContextQueue(sourceId);
        Guid currentId = queue.CurrentQueueItemId!.Value;
        var player = PlayerState.Restore(false, 5_000, Start, 50, 3);

        _coordinator.StartPlaybackContext(
            queue,
            player,
            PlaybackSourceType.Playlist,
            sourceId,
            queue.Items,
            null,
            Start.AddSeconds(1));

        queue.CurrentQueueItemId.ShouldBe(currentId);
        player.IsPlaying.ShouldBeTrue();
        player.PositionMs.ShouldBe(5_000);
    }

    [Fact]
    public void StartPlaybackContext_ExplicitTrackInSameContext_SelectsAndStarts()
    {
        var sourceId = Guid.NewGuid();
        QueueState queue = ContextQueue(sourceId);
        QueueItem[] originalItems = [.. queue.Items];
        var player = PlayerState.Restore(false, 5_000, Start, 50, 3);

        _coordinator.StartPlaybackContext(
            queue,
            player,
            PlaybackSourceType.Playlist,
            sourceId,
            queue.Items,
            1,
            Start.AddSeconds(1));

        queue.Items.ShouldBe(originalItems);
        queue.CurrentQueueItemId.ShouldBe(originalItems[1].QueueItemId);
        player.IsPlaying.ShouldBeTrue();
        player.PositionMs.ShouldBe(0);
        player.Version.ShouldBe(4);
    }

    [Fact]
    public void Previous_UsesDerivedPlayingPositionAboveThreshold()
    {
        QueueState queue = QueueWithTwoSelectedAtSecond();
        Guid currentId = queue.CurrentQueueItemId!.Value;
        var player = PlayerState.Restore(true, 0, Start, 50, 1);

        QueueNavigationResult result = _coordinator.Previous(
            queue,
            player,
            Start.AddMilliseconds(
                ConnectStateCoordinator.PreviousRestartThresholdMs + 1));

        result.CurrentItemChanged.ShouldBeFalse();
        queue.CurrentQueueItemId.ShouldBe(currentId);
        player.PositionMs.ShouldBe(0);
    }

    [Fact]
    public void Previous_AtExactlyThreshold_ChangesItem()
    {
        QueueState queue = QueueWithTwoSelectedAtSecond();
        Guid expectedId = queue.Items[0].QueueItemId;
        var player = PlayerState.Restore(true, 0, Start, 50, 1);

        QueueNavigationResult result = _coordinator.Previous(
            queue,
            player,
            Start.AddMilliseconds(ConnectStateCoordinator.PreviousRestartThresholdMs));

        result.CurrentItemChanged.ShouldBeTrue();
        queue.CurrentQueueItemId.ShouldBe(expectedId);
        player.PositionMs.ShouldBe(0);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(ConnectStateCoordinator.PreviousRestartThresholdMs, true)]
    [InlineData(ConnectStateCoordinator.PreviousRestartThresholdMs + 1, false)]
    public void Previous_PausedPlayerUsesStoredPosition(
        long positionMs,
        bool expectedItemChange)
    {
        QueueState queue = QueueWithTwoSelectedAtSecond();
        Guid originalId = queue.CurrentQueueItemId!.Value;
        var player = PlayerState.Restore(false, positionMs, Start, 50, 1);

        QueueNavigationResult result = _coordinator.Previous(
            queue,
            player,
            Start.AddMinutes(1));

        result.CurrentItemChanged.ShouldBe(expectedItemChange);
        if (expectedItemChange)
        {
            queue.CurrentQueueItemId.ShouldBe(queue.Items[0].QueueItemId);
        }
        else
        {
            queue.CurrentQueueItemId.ShouldBe(originalId);
        }
    }

    [Fact]
    public void FinalActiveConnectionDisconnect_PausesPlayer()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();
        presence.RegisterConnection(deviceId, "Browser", "connection-1", Start);
        presence.SelectActiveDevice(deviceId);
        var player = PlayerState.Restore(true, 5_000, Start, 50, 1);

        DisconnectConnectionResult result = _coordinator.DisconnectConnection(
            presence,
            player,
            "connection-1",
            Start.AddSeconds(1));

        result.ActiveDeviceLost.ShouldBeTrue();
        presence.ActiveDeviceId.ShouldBeNull();
        player.IsPlaying.ShouldBeFalse();
        player.PositionMs.ShouldBe(6_000);
        player.PositionUpdatedAt.ShouldBe(Start.AddSeconds(1));
    }

    [Fact]
    public void FinalActiveConnectionExpiry_PausesPlayer()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();
        presence.RegisterConnection(deviceId, "Browser", "connection-1", Start);
        presence.SelectActiveDevice(deviceId);
        var player = PlayerState.Restore(true, 5_000, Start, 50, 1);

        ExpireConnectionsResult result = _coordinator.ExpireConnections(
            presence,
            player,
            ["connection-1"],
            Start.AddSeconds(1));

        result.ActiveDeviceLost.ShouldBeTrue();
        presence.ActiveDeviceId.ShouldBeNull();
        player.IsPlaying.ShouldBeFalse();
        player.PositionMs.ShouldBe(6_000);
    }

    private static QueueState QueueWithTwoSelectedAtSecond()
    {
        var queue = new QueueState();
        queue.Add(Guid.NewGuid());
        QueueItem second = queue.Add(Guid.NewGuid());
        queue.Select(second.QueueItemId);
        return queue;
    }

    private static QueueState QueueWithTwoSelectedAtFirst()
    {
        var queue = new QueueState();
        QueueItem first = queue.Add(Guid.NewGuid());
        queue.Add(Guid.NewGuid());
        queue.Select(first.QueueItemId);
        return queue;
    }

    private static QueueState ContextQueue(Guid sourceId)
    {
        QueueItem[] items =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), 0),
            new(Guid.NewGuid(), Guid.NewGuid(), 1)
        ];
        return QueueState.Restore(
            items,
            items[0].QueueItemId,
            RepeatMode.None,
            false,
            4,
            sourceId,
            PlaybackSourceType.Playlist);
    }
}
