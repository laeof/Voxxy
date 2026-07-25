using Connect.Application.Results;

namespace Connect.Presentation.Broadcasting;

public interface IConnectBroadcaster
{
    Task BroadcastAsync(
        Guid userId,
        Guid commandId,
        ConnectApplicationResult result,
        CancellationToken cancellationToken);
}
