using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Domain.Presence;

namespace Connect.Application.DeviceLifecycle;

public sealed class SelectActiveDeviceHandler(IConnectStateStore store)
    : ISelectActiveDeviceHandler
{
    private const string CommandType = "SelectActiveDevice";

    public async Task<ConnectApplicationResult> HandleAsync(
        SelectActiveDeviceCommand command,
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
            command.ConnectionId);
        for (int attempt = 0; attempt < CommandHandlerPolicy.MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectSnapshotReadResult read = await store.ReadSnapshotAsync(
                command.UserId,
                command.ServerTime,
                cancellationToken);
            if (read.Status != PersistenceStatus.Success || read.Snapshot is null)
            {
                return PersistenceStatusMapper.FromReadFailure(read.Status, read.Error);
            }

            PresenceState presence = read.Snapshot.Presence;
            long expectedVersion = presence.Version;

            cancellationToken.ThrowIfCancellationRequested();
            SelectActiveDeviceResult domainResult =
                presence.SelectActiveDevice(command.DeviceId);
            var outcome = new ConnectCommandOutcome(
                command.DeviceId,
                command.ConnectionId,
                presence.Version);
            var persistenceCommand = new PersistenceCommand(
                command.CommandId,
                CommandType,
                fingerprint,
                CommandOutcomeSerializer.Serialize(outcome));

            ConnectCommandStatus appliedStatus = domainResult.Status switch
            {
                SelectActiveDeviceStatus.Selected => ConnectCommandStatus.Applied,
                SelectActiveDeviceStatus.NoOp => ConnectCommandStatus.NoChanges,
                SelectActiveDeviceStatus.DeviceNotFound => ConnectCommandStatus.DeviceNotFound,
                SelectActiveDeviceStatus.DeviceOffline => ConnectCommandStatus.DeviceOffline,
                _ => throw new InvalidOperationException(
                    $"Unsupported device selection status '{domainResult.Status}'.")
            };

            cancellationToken.ThrowIfCancellationRequested();
            PersistenceCommitResult commit =
                domainResult.Status == SelectActiveDeviceStatus.Selected
                    ? await store.TryCommitPresenceAsync(
                        new PresenceCommit(
                            command.UserId,
                            persistenceCommand,
                            expectedVersion,
                            presence),
                        cancellationToken)
                    : await store.TryRecordCommandAsync(
                        new CommandRecordCommit(command.UserId, persistenceCommand),
                        cancellationToken);

            if (commit.Status == PersistenceStatus.VersionConflict)
            {
                continue;
            }

            PresenceStateDto presenceDto = ConnectDtoMapper.ToDto(presence);
            return HandlerResultFactory.FromCommit(
                commit,
                appliedStatus,
                presence: presenceDto,
                outcome: outcome);
        }

        return new ConnectApplicationResult(ConnectCommandStatus.Conflict);
    }
}
