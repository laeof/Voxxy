using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Common;
using Connect.Application.Results;

namespace Connect.Application.Snapshots;

public sealed class ConnectSnapshotReader(IConnectStateStore store) : IConnectSnapshotReader
{
    public async Task<ConnectApplicationResult> GetAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return HandlerResultFactory.ValidationFailed("User ID is required.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        ConnectSnapshotReadResult read =
            await store.ReadSnapshotAsync(userId, serverTime, cancellationToken);
        if (read.Status != PersistenceStatus.Success || read.Snapshot is null)
        {
            return PersistenceStatusMapper.FromReadFailure(read.Status, read.Error);
        }

        return new ConnectApplicationResult(
            ConnectCommandStatus.Applied,
            Snapshot: ConnectDtoMapper.ToDto(userId, read.Snapshot, serverTime));
    }
}
