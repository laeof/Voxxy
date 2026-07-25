using Connect.Presentation.Transport;

namespace Connect.Presentation.Hubs;

public interface IConnectHubClient
{
    Task PlayerStateChanged(PlayerStateChangedEvent message);
    Task QueueStateChanged(QueueStateChangedEvent message);
    Task PresenceStateChanged(PresenceStateChangedEvent message);
    Task PlayerQueueStateChanged(PlayerQueueStateChangedEvent message);
    Task PlayerPresenceStateChanged(PlayerPresenceStateChangedEvent message);
}
