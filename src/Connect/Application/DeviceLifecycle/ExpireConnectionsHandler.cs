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

public sealed class ExpireConnectionsHandler(
    IConnectStateStore store,
    ConnectStateCoordinator coordinator)
    : IExpireConnectionsHandler
{
    private const string CommandType = "ExpireConnections";

    public async Task<ConnectApplicationResult> HandleAsync(
        ExpireConnectionsCommand command,
        CancellationToken cancellationToken = default)
    {
        string? validationError = CommandValidation.Validate(command);
        if (validationError is not null)
        {
            return HandlerResultFactory.ValidationFailed(validationError);
        }

        string[] connectionIds =
        [
            .. command.ConnectionIds.Distinct(StringComparer.Ordinal)
        ];
        string fingerprint = CommandFingerprint.Create(
            CommandType,
            CommandFingerprint.ExpiredConnections(connectionIds));

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
            ExpiredConnectionsReadResult verifiedExpired =
                await store.ReadExpiredConnectionsAsync(
                    command.UserId,
                    presence,
                    cancellationToken);
            if (verifiedExpired.Status != PersistenceStatus.Success)
            {
                return PersistenceStatusMapper.FromReadFailure(
                    verifiedExpired.Status,
                    verifiedExpired.Error);
            }

            HashSet<string> requested = connectionIds.ToHashSet(StringComparer.Ordinal);
            string[] currentlyExpired =
            [
                .. verifiedExpired.ConnectionIds.Where(requested.Contains)
            ];
            ExpireConnectionsResult domainResult = coordinator.ExpireConnections(
                presence,
                player,
                currentlyExpired,
                command.ServerTime);
            bool playerChanged = player.Version != expectedPlayerVersion;
            var outcome = new ConnectCommandOutcome(
                PresenceVersion: presence.Version,
                RemovedConnectionCount: domainResult.RemovedCount,
                PlayerVersion: playerChanged ? player.Version : null);
            var persistenceCommand = new PersistenceCommand(
                command.CommandId,
                CommandType,
                fingerprint,
                CommandOutcomeSerializer.Serialize(outcome));

            cancellationToken.ThrowIfCancellationRequested();
            PersistenceCommitResult commit = (domainResult.RemovedCount, playerChanged) switch
            {
                (0, _) => await store.TryRecordCommandAsync(
                    new CommandRecordCommit(command.UserId, persistenceCommand),
                    cancellationToken),
                (_, true) => await store.TryCommitPlayerAndPresenceAsync(
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
                domainResult.RemovedCount > 0
                    ? ConnectCommandStatus.Applied
                    : ConnectCommandStatus.NoChanges,
                player: playerChanged ? playerDto : null,
                presence: presenceDto,
                outcome: outcome);
        }

        return new ConnectApplicationResult(ConnectCommandStatus.Conflict);
    }
}
