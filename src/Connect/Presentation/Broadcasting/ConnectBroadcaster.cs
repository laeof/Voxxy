using Connect.Application.Results;
using Connect.Presentation.Hubs;
using Connect.Presentation.Transport;
using Microsoft.AspNetCore.SignalR;

namespace Connect.Presentation.Broadcasting;

public sealed class ConnectBroadcaster(
    IHubContext<PlayerHub, IConnectHubClient> hubContext)
    : IConnectBroadcaster
{
    public async Task BroadcastAsync(
        Guid userId,
        Guid commandId,
        ConnectApplicationResult result,
        CancellationToken cancellationToken)
    {
        if (result.Status != ConnectCommandStatus.Applied)
        {
            return;
        }

        IConnectHubClient client =
            hubContext.Clients.Group(ConnectGroupNames.User(userId));
        if (result.Player is not null && result.Queue is not null)
        {
            await client.PlayerQueueStateChanged(
                new PlayerQueueStateChangedEvent(commandId, result.Player, result.Queue));
            return;
        }

        if (result.Player is not null && result.Presence is not null)
        {
            await client.PlayerPresenceStateChanged(
                new PlayerPresenceStateChangedEvent(
                    commandId,
                    result.Player,
                    result.Presence));
            return;
        }

        if (result.Player is not null)
        {
            await client.PlayerStateChanged(
                new PlayerStateChangedEvent(commandId, result.Player));
            return;
        }

        if (result.Queue is not null)
        {
            await client.QueueStateChanged(
                new QueueStateChangedEvent(commandId, result.Queue));
            return;
        }

        if (result.Presence is not null)
        {
            await client.PresenceStateChanged(
                new PresenceStateChangedEvent(commandId, result.Presence));
        }
    }
}
