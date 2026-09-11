using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;

namespace Connect.Domain.Synchronization;

public sealed class ConnectStateCoordinator
{
    public const long PreviousRestartThresholdMs = 3_000;

    public bool SelectTrack(
        QueueState queue,
        PlayerState player,
        Guid queueItemId,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(player);

        bool selected = queue.Select(queueItemId);
        player.ResetPosition(serverTime);
        player.Play(serverTime);

        return selected;
    }

    public RemoveQueueItemResult RemoveQueueItem(
        QueueState queue,
        PlayerState player,
        Guid queueItemId,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(player);

        RemoveQueueItemResult result = queue.Remove(queueItemId);
        if (!result.RemovedCurrentItem)
        {
            return result;
        }

        if (result.IsEmpty)
        {
            player.ClearAndPause(serverTime);
        }
        else
        {
            player.ResetPosition(serverTime);
        }

        return result;
    }

    public QueueNavigationResult Next(
        QueueState queue,
        PlayerState player,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(player);

        QueueNavigationResult result = queue.Next();
        ApplyNavigationResult(player, result, serverTime);
        return result;
    }

    public bool CompleteCurrentTrack(
        QueueState queue,
        PlayerState player,
        Guid expectedQueueItemId,
        long completedPositionMs,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(player);

        if (queue.CurrentItem?.QueueItemId != expectedQueueItemId)
        {
            return false;
        }

        QueueNavigationResult result = queue.Next();
        if (result.ShouldPause)
        {
            player.Complete(completedPositionMs, serverTime);
        }
        else if (result.ShouldResetPosition)
        {
            player.ResetPosition(serverTime);
        }

        return true;
    }

    public void StartPlaybackContext(
        QueueState queue,
        PlayerState player,
        PlaybackSourceType sourceType,
        Guid sourceId,
        IReadOnlyCollection<QueueItem> items,
        int? startIndex,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(items);

        int selectedIndex = startIndex ?? 0;
        if (!queue.IsContext(sourceType, sourceId))
        {
            queue.ReplaceContext(sourceType, sourceId, items, selectedIndex);
            player.PlayFrom(0, serverTime);
            return;
        }

        if (startIndex is int explicitIndex)
        {
            if (explicitIndex < 0 || explicitIndex >= queue.Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
            queue.Select(queue.Items[explicitIndex].QueueItemId);
            player.PlayFrom(0, serverTime);
            return;
        }

        player.Play(serverTime);
    }

    public QueueNavigationResult Previous(
        QueueState queue,
        PlayerState player,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(player);

        if (player.GetPositionAt(serverTime) > PreviousRestartThresholdMs)
        {
            player.ResetPosition(serverTime);
            return new QueueNavigationResult(queue.CurrentItem, false, true, false);
        }

        QueueNavigationResult result = queue.Previous();
        ApplyNavigationResult(player, result, serverTime);
        return result;
    }

    public DisconnectConnectionResult DisconnectConnection(
        PresenceState presence,
        PlayerState player,
        string connectionId,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(presence);
        ArgumentNullException.ThrowIfNull(player);

        DisconnectConnectionResult result = presence.DisconnectConnection(connectionId);
        if (result.ActiveDeviceLost)
        {
            player.Pause(serverTime);
        }

        return result;
    }

    public ExpireConnectionsResult ExpireConnections(
        PresenceState presence,
        PlayerState player,
        IReadOnlyCollection<string> expiredConnectionIds,
        DateTimeOffset serverTime)
    {
        ArgumentNullException.ThrowIfNull(presence);
        ArgumentNullException.ThrowIfNull(player);

        ExpireConnectionsResult result =
            presence.ExpireConnections(expiredConnectionIds);
        if (result.ActiveDeviceLost)
        {
            player.Pause(serverTime);
        }

        return result;
    }

    private static void ApplyNavigationResult(
        PlayerState player,
        QueueNavigationResult result,
        DateTimeOffset serverTime)
    {
        if (result.ShouldPause)
        {
            player.Pause(serverTime);
        }

        if (result.ShouldResetPosition)
        {
            player.ResetPosition(serverTime);
        }
    }
}
