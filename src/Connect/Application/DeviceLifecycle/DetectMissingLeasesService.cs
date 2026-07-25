using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;
using Connect.Domain.Presence;

namespace Connect.Application.DeviceLifecycle;

public sealed class DetectMissingLeasesService(
    IConnectStateStore store,
    IExpireConnectionsHandler expireConnectionsHandler)
    : IDetectMissingLeasesService
{
    public async Task<ConnectApplicationResult> ExecuteAsync(
        DetectMissingLeasesCommand command,
        CancellationToken cancellationToken = default)
    {
        string? validationError = CommandValidation.Validate(command);
        if (validationError is not null)
        {
            return HandlerResultFactory.ValidationFailed(validationError);
        }

        cancellationToken.ThrowIfCancellationRequested();
        PersistenceReadResult<PresenceState> read =
            await store.ReadPresenceAsync(command.UserId, cancellationToken);
        if (read.Status != PersistenceStatus.Success || read.State is null)
        {
            return PersistenceStatusMapper.FromReadFailure(read.Status, read.Error);
        }

        cancellationToken.ThrowIfCancellationRequested();
        ExpiredConnectionsReadResult expired =
            await store.ReadExpiredConnectionsAsync(
                command.UserId,
                read.State,
                cancellationToken);
        if (expired.Status != PersistenceStatus.Success)
        {
            return PersistenceStatusMapper.FromReadFailure(expired.Status, expired.Error);
        }

        if (expired.ConnectionIds.Count == 0)
        {
            return new ConnectApplicationResult(ConnectCommandStatus.NoChanges);
        }

        return await expireConnectionsHandler.HandleAsync(
            new ExpireConnectionsCommand(
                command.UserId,
                command.CommandId,
                expired.ConnectionIds,
                command.ServerTime),
            cancellationToken);
    }
}
