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
}
