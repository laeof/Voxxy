using Connect.Application.Results;

namespace Connect.Application.Abstractions.Handlers;

public interface IConnectSnapshotReader
{
    Task<ConnectApplicationResult> GetAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default);
}
