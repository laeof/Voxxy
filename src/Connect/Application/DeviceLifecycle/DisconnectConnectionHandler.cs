using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Synchronization;

namespace Connect.Application.DeviceLifecycle;

public sealed class DisconnectConnectionHandler(
    IConnectStateStore store,
    ConnectStateCoordinator coordinator)
    : IDisconnectConnectionHandler
{
    private const string CommandType = "DisconnectConnection";

    public async Task<ConnectApplicationResult> HandleAsync(
        DisconnectConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        string? validationError = CommandValidation.Validate(command);
        if (validationError is not null)
        {
            return HandlerResultFactory.ValidationFailed(validationError);
        }

        string fingerprint = CommandFingerprint.Create(CommandType, command.ConnectionId);
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

            PlayerState player = read.Snapshot.Player;
            PresenceState presence = read.Snapshot.Presence;
            long expectedPlayerVersion = player.Version;
            long expectedPresenceVersion = presence.Version;

            cancellationToken.ThrowIfCancellationRequested();
            DisconnectConnectionResult domainResult = coordinator.DisconnectConnection(
                presence,
                player,
                command.ConnectionId,
                command.ServerTime);
            bool playerChanged = player.Version != expectedPlayerVersion;
            var outcome = new ConnectCommandOutcome(
                ConnectionId: command.ConnectionId,
                PresenceVersion: presence.Version,
                RemovedConnectionCount: domainResult.Removed ? 1 : 0,
                PlayerVersion: playerChanged ? player.Version : null);
            var persistenceCommand = new PersistenceCommand(
                command.CommandId,
                CommandType,
                fingerprint,
                CommandOutcomeSerializer.Serialize(outcome));

            cancellationToken.ThrowIfCancellationRequested();
            PersistenceCommitResult commit = (domainResult.Removed, playerChanged) switch
            {
                (false, _) => await store.TryRecordCommandAsync(
                    new CommandRecordCommit(command.UserId, persistenceCommand),
                    cancellationToken),
                (true, true) => await store.TryCommitPlayerAndPresenceAsync(
                    new PlayerPresenceCommit(
                        command.UserId,
                        persistenceCommand,
                        expectedPlayerVersion,
                        player,
                        expectedPresenceVersion,
                        presence),
                    cancellationToken),
                _ => await store.TryCommitPresenceAsync(
                    new PresenceCommit(
                        command.UserId,
                        persistenceCommand,
                        expectedPresenceVersion,
                        presence),
                    cancellationToken)
            };

            if (commit.Status == PersistenceStatus.VersionConflict)
            {
                continue;
            }

            PlayerStateDto playerDto = ConnectDtoMapper.ToDto(player);
            PresenceStateDto presenceDto = ConnectDtoMapper.ToDto(presence);
            return HandlerResultFactory.FromCommit(
                commit,
                domainResult.Removed
                    ? ConnectCommandStatus.Applied
                    : ConnectCommandStatus.NoChanges,
                player: playerChanged ? playerDto : null,
                presence: presenceDto,
                outcome: outcome);
        }

        return new ConnectApplicationResult(ConnectCommandStatus.Conflict);
    }
}
