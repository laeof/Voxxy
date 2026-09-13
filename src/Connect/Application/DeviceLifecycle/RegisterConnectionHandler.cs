using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Domain.Presence;

namespace Connect.Application.DeviceLifecycle;

public sealed class RegisterConnectionHandler(IConnectStateStore store)
    : IRegisterConnectionHandler
{
    private const string CommandType = "RegisterConnection";

    public async Task<ConnectApplicationResult> HandleAsync(
        RegisterConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        string? validationError = CommandValidation.Validate(command);
        if (validationError is not null)
        {
            return HandlerResultFactory.ValidationFailed(validationError);
        }

        string fingerprint = CommandFingerprint.Create(
            CommandType,
            CommandFingerprint.GuidValue(command.DeviceId),
            CommandFingerprint.GuidValue(command.RuntimeSessionId),
            command.DeviceName,
            command.ConnectionId);

        for (int attempt = 0; attempt < CommandHandlerPolicy.MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PersistenceReadResult<PresenceState> read =
                await store.ReadPresenceAsync(command.UserId, cancellationToken);
            if (read.Status != PersistenceStatus.Success || read.State is null)
            {
                return PersistenceStatusMapper.FromReadFailure(read.Status, read.Error);
            }

            PresenceState presence = read.State;
            long expectedVersion = presence.Version;
            bool changed;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                changed = presence.RegisterConnection(
                    command.DeviceId,
                    command.DeviceName,
                    command.ConnectionId,
                    command.ServerTime,
                    command.RuntimeSessionId);
            }
            catch (Exception exception) when (
                exception is ArgumentException or InvalidOperationException)
            {
                return HandlerResultFactory.ValidationFailed(exception.Message);
            }

            var outcome = new ConnectCommandOutcome(
                command.DeviceId,
                command.ConnectionId,
                presence.Version);
            var persistenceCommand = new PersistenceCommand(
                command.CommandId,
                CommandType,
                fingerprint,
                CommandOutcomeSerializer.Serialize(outcome));

            cancellationToken.ThrowIfCancellationRequested();
            PersistenceCommitResult commit = changed
                ? await store.TryCommitPresenceAsync(
                    new PresenceCommit(
                        command.UserId,
                        persistenceCommand,
                        expectedVersion,
                        presence,
                        command.ConnectionId),
                    cancellationToken)
                : await store.TryRecordCommandAsync(
                    new CommandRecordCommit(
                        command.UserId,
                        persistenceCommand,
                        command.ConnectionId),
                    cancellationToken);

            if (commit.Status == PersistenceStatus.VersionConflict)
            {
                continue;
            }

            PresenceStateDto presenceDto = ConnectDtoMapper.ToDto(presence);
            return HandlerResultFactory.FromCommit(
                commit,
                changed
                    ? ConnectCommandStatus.Applied
                    : ConnectCommandStatus.NoChanges,
                presence: presenceDto,
                outcome: outcome);
        }

        return new ConnectApplicationResult(ConnectCommandStatus.Conflict);
    }
}
