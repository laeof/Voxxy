using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;

namespace Connect.Application.DeviceLifecycle;

public sealed class RefreshConnectionLeaseHandler(IConnectStateStore store)
    : IRefreshConnectionLeaseHandler
{
    public async Task<ConnectApplicationResult> HandleAsync(
        RefreshConnectionLeaseCommand command,
        CancellationToken cancellationToken = default)
    {
        string? validationError = CommandValidation.Validate(command);
        if (validationError is not null)
        {
            return HandlerResultFactory.ValidationFailed(validationError);
        }

        cancellationToken.ThrowIfCancellationRequested();
        PersistenceStatus status = await store.RefreshConnectionLeaseAsync(
            command.UserId,
            command.ConnectionId,
            cancellationToken);

        return new ConnectApplicationResult(
            PersistenceStatusMapper.ToCommandStatus(status));
    }
}
