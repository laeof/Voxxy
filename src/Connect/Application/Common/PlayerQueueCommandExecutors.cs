using Connect.Application.Abstractions.Persistence;
using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Domain.Player;
using Connect.Domain.Queue;

namespace Connect.Application.Common;

internal static class PlayerQueueCommandExecutors
{
    public static async Task<ConnectApplicationResult> ExecutePlayerAsync(
        IConnectStateStore store,
        Guid userId,
        Guid commandId,
        DateTimeOffset serverTime,
        string commandType,
        IReadOnlyCollection<string?> fingerprintValues,
        Func<PlayerState, bool> mutate,
        CancellationToken cancellationToken)
    {
        string fingerprint = CommandFingerprint.Create(
            commandType,
            [.. fingerprintValues]);

        for (int attempt = 0; attempt < CommandHandlerPolicy.MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PersistenceReadResult<PlayerState> read =
                await store.ReadPlayerAsync(userId, serverTime, cancellationToken);
            if (read.Status != PersistenceStatus.Success || read.State is null)
            {
                return PersistenceStatusMapper.FromReadFailure(read.Status, read.Error);
            }

            PlayerState player = read.State;
            PlayerStateDto playerBefore = ConnectDtoMapper.ToDto(player);
            long expectedVersion = player.Version;
            bool changed;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                changed = mutate(player);
            }
            catch (ArgumentException exception)
            {
                return DomainFailureMapper.Validation(exception);
            }

            var outcome = new ConnectCommandOutcome(PlayerVersion: player.Version);
            PersistenceCommand persistenceCommand = CreatePersistenceCommand(
                commandId,
                commandType,
                fingerprint,
                outcome);

            cancellationToken.ThrowIfCancellationRequested();
            PersistenceCommitResult commit = changed
                ? await store.TryCommitPlayerAsync(
                    new PlayerCommit(
                        userId,
                        persistenceCommand,
                        expectedVersion,
                        player),
                    cancellationToken)
                : await store.TryRecordCommandAsync(
                    new CommandRecordCommit(userId, persistenceCommand),
                    cancellationToken);

            if (commit.Status == PersistenceStatus.VersionConflict)
            {
                continue;
            }

            PlayerStateDto playerDto = commit.Status == PersistenceStatus.Applied
                ? ConnectDtoMapper.ToDto(player)
                : playerBefore;
            return HandlerResultFactory.FromCommit(
                commit,
                changed ? ConnectCommandStatus.Applied : ConnectCommandStatus.NoChanges,
                player: playerDto,
                outcome: outcome);
        }

        return new ConnectApplicationResult(ConnectCommandStatus.Conflict);
    }

    public static async Task<ConnectApplicationResult> ExecuteQueueAsync(
        IConnectStateStore store,
        Guid userId,
        Guid commandId,
        string commandType,
        IReadOnlyCollection<string?> fingerprintValues,
        Func<QueueState, bool> mutate,
        Func<QueueState, ConnectCommandOutcome>? createOutcome,
        Func<Exception, ConnectApplicationResult?>? mapFailure,
        CancellationToken cancellationToken)
    {
        string fingerprint = CommandFingerprint.Create(
            commandType,
            [.. fingerprintValues]);

        for (int attempt = 0; attempt < CommandHandlerPolicy.MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PersistenceReadResult<QueueState> read =
                await store.ReadQueueAsync(userId, cancellationToken);
            if (read.Status != PersistenceStatus.Success || read.State is null)
            {
                return PersistenceStatusMapper.FromReadFailure(read.Status, read.Error);
            }

            QueueState queue = read.State;
            QueueStateDto queueBefore = ConnectDtoMapper.ToDto(queue);
            long expectedVersion = queue.Version;
            bool changed;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                changed = mutate(queue);
            }
            catch (KeyNotFoundException exception)
            {
                return MapFailure(exception, mapFailure);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return MapFailure(exception, mapFailure);
            }
            catch (ArgumentException exception)
            {
                return MapFailure(exception, mapFailure);
            }
            catch (InvalidOperationException exception)
            {
                return MapFailure(exception, mapFailure);
            }

            ConnectCommandOutcome outcome = createOutcome?.Invoke(queue) ??
                new ConnectCommandOutcome(QueueVersion: queue.Version);
            PersistenceCommand persistenceCommand = CreatePersistenceCommand(
                commandId,
                commandType,
                fingerprint,
                outcome);

            cancellationToken.ThrowIfCancellationRequested();
            PersistenceCommitResult commit = changed
                ? await store.TryCommitQueueAsync(
                    new QueueCommit(
                        userId,
                        persistenceCommand,
                        expectedVersion,
                        queue),
                    cancellationToken)
                : await store.TryRecordCommandAsync(
                    new CommandRecordCommit(userId, persistenceCommand),
                    cancellationToken);

            if (commit.Status == PersistenceStatus.VersionConflict)
            {
                continue;
            }

            QueueStateDto queueDto = commit.Status == PersistenceStatus.Applied
                ? ConnectDtoMapper.ToDto(queue)
                : queueBefore;
            return HandlerResultFactory.FromCommit(
                commit,
                changed ? ConnectCommandStatus.Applied : ConnectCommandStatus.NoChanges,
                queue: queueDto,
                outcome: outcome);
        }

        return new ConnectApplicationResult(ConnectCommandStatus.Conflict);
    }

    public static async Task<ConnectApplicationResult> ExecuteCoordinatedAsync(
        IConnectStateStore store,
        Guid userId,
        Guid commandId,
        DateTimeOffset serverTime,
        string commandType,
        IReadOnlyCollection<string?> fingerprintValues,
        Action<QueueState, PlayerState> mutate,
        Func<Exception, ConnectApplicationResult?>? mapFailure,
        CancellationToken cancellationToken)
    {
        string fingerprint = CommandFingerprint.Create(
            commandType,
            [.. fingerprintValues]);

        for (int attempt = 0; attempt < CommandHandlerPolicy.MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectSnapshotReadResult read =
                await store.ReadSnapshotAsync(userId, serverTime, cancellationToken);
            if (read.Status != PersistenceStatus.Success || read.Snapshot is null)
            {
                return PersistenceStatusMapper.FromReadFailure(read.Status, read.Error);
            }

            PlayerState player = read.Snapshot.Player;
            QueueState queue = read.Snapshot.Queue;
            PlayerStateDto playerBefore = ConnectDtoMapper.ToDto(player);
            QueueStateDto queueBefore = ConnectDtoMapper.ToDto(queue);
            long expectedPlayerVersion = player.Version;
            long expectedQueueVersion = queue.Version;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                mutate(queue, player);
            }
            catch (KeyNotFoundException exception)
            {
                return MapFailure(exception, mapFailure);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return MapFailure(exception, mapFailure);
            }
            catch (ArgumentException exception)
            {
                return MapFailure(exception, mapFailure);
            }
            catch (InvalidOperationException exception)
            {
                return MapFailure(exception, mapFailure);
            }

            bool playerChanged = player.Version != expectedPlayerVersion;
            bool queueChanged = queue.Version != expectedQueueVersion;
            var outcome = new ConnectCommandOutcome(
                PlayerVersion: player.Version,
                QueueVersion: queue.Version);
            PersistenceCommand persistenceCommand = CreatePersistenceCommand(
                commandId,
                commandType,
                fingerprint,
                outcome);

            cancellationToken.ThrowIfCancellationRequested();
            PersistenceCommitResult commit = (playerChanged, queueChanged) switch
            {
                (true, true) => await store.TryCommitPlayerAndQueueAsync(
                    new PlayerQueueCommit(
                        userId,
                        persistenceCommand,
                        expectedPlayerVersion,
                        player,
                        expectedQueueVersion,
                        queue),
                    cancellationToken),
                (true, false) => await store.TryCommitPlayerAsync(
                    new PlayerCommit(
                        userId,
                        persistenceCommand,
                        expectedPlayerVersion,
                        player),
                    cancellationToken),
                (false, true) => await store.TryCommitQueueAsync(
                    new QueueCommit(
                        userId,
                        persistenceCommand,
                        expectedQueueVersion,
                        queue),
                    cancellationToken),
                _ => await store.TryRecordCommandAsync(
                    new CommandRecordCommit(userId, persistenceCommand),
                    cancellationToken)
            };

            if (commit.Status == PersistenceStatus.VersionConflict)
            {
                continue;
            }

            bool exposeMutation = commit.Status == PersistenceStatus.Applied;
            PlayerStateDto? playerDto = (exposeMutation, playerChanged, commit.Status) switch
            {
                (true, true, _) => ConnectDtoMapper.ToDto(player),
                (_, _, PersistenceStatus.Duplicate) => playerBefore,
                _ => null
            };
            QueueStateDto? queueDto = (exposeMutation, queueChanged, commit.Status) switch
            {
                (true, true, _) => ConnectDtoMapper.ToDto(queue),
                (_, _, PersistenceStatus.Duplicate) => queueBefore,
                _ => null
            };
            return HandlerResultFactory.FromCommit(
                commit,
                playerChanged || queueChanged
                    ? ConnectCommandStatus.Applied
                    : ConnectCommandStatus.NoChanges,
                player: playerDto,
                queue: queueDto,
                outcome: outcome);
        }

        return new ConnectApplicationResult(ConnectCommandStatus.Conflict);
    }

    private static PersistenceCommand CreatePersistenceCommand(
        Guid commandId,
        string commandType,
        string fingerprint,
        ConnectCommandOutcome outcome) =>
        new(
            commandId,
            commandType,
            fingerprint,
            CommandOutcomeSerializer.Serialize(outcome));

    private static ConnectApplicationResult MapFailure(
        Exception exception,
        Func<Exception, ConnectApplicationResult?>? mapFailure) =>
        mapFailure?.Invoke(exception) ?? DomainFailureMapper.Validation(exception);
}
