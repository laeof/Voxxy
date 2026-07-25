using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;
using Connect.Domain.Synchronization;

namespace Connect.Application.PlayerQueue;

public sealed class RemoveQueueItemHandler(
    IConnectStateStore store,
    ConnectStateCoordinator coordinator)
    : IRemoveQueueItemHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        RemoveQueueItemCommand command,
        CancellationToken cancellationToken = default) =>
        CoordinatedHandler.ExecuteAsync(
            store,
            command,
            nameof(RemoveQueueItemCommand),
            [CommandFingerprint.GuidValue(command.QueueItemId)],
            (queue, player) => coordinator.RemoveQueueItem(
                queue,
                player,
                command.QueueItemId,
                command.ServerTime),
            cancellationToken);
}

public sealed class SelectQueueItemHandler(
    IConnectStateStore store,
    ConnectStateCoordinator coordinator)
    : ISelectQueueItemHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        SelectQueueItemCommand command,
        CancellationToken cancellationToken = default) =>
        CoordinatedHandler.ExecuteAsync(
            store,
            command,
            nameof(SelectQueueItemCommand),
            [CommandFingerprint.GuidValue(command.QueueItemId)],
            (queue, player) => coordinator.SelectTrack(
                queue,
                player,
                command.QueueItemId,
                command.ServerTime),
            cancellationToken);
}

public sealed class NextQueueItemHandler(
    IConnectStateStore store,
    ConnectStateCoordinator coordinator)
    : INextQueueItemHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        NextQueueItemCommand command,
        CancellationToken cancellationToken = default) =>
        CoordinatedHandler.ExecuteAsync(
            store,
            command,
            nameof(NextQueueItemCommand),
            [],
            (queue, player) => coordinator.Next(queue, player, command.ServerTime),
            cancellationToken);
}

public sealed class PreviousQueueItemHandler(
    IConnectStateStore store,
    ConnectStateCoordinator coordinator)
    : IPreviousQueueItemHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        PreviousQueueItemCommand command,
        CancellationToken cancellationToken = default) =>
        CoordinatedHandler.ExecuteAsync(
            store,
            command,
            nameof(PreviousQueueItemCommand),
            [],
            (queue, player) => coordinator.Previous(queue, player, command.ServerTime),
            cancellationToken);
}

internal static class CoordinatedHandler
{
    public static Task<ConnectApplicationResult> ExecuteAsync<TCommand>(
        IConnectStateStore store,
        TCommand command,
        string commandType,
        IReadOnlyCollection<string?> fingerprintValues,
        Action<Connect.Domain.Queue.QueueState, Connect.Domain.Player.PlayerState> mutate,
        CancellationToken cancellationToken)
        where TCommand : class
    {
        string? validationError = command switch
        {
            RemoveQueueItemCommand value => CommandValidation.Validate(value),
            SelectQueueItemCommand value => CommandValidation.Validate(value),
            NextQueueItemCommand value => CommandValidation.Validate(value),
            PreviousQueueItemCommand value => CommandValidation.Validate(value),
            _ => throw new InvalidOperationException(
                $"Unsupported coordinated command '{typeof(TCommand).Name}'.")
        };
        if (validationError is not null)
        {
            return Task.FromResult(HandlerResultFactory.ValidationFailed(validationError));
        }

        (Guid userId, Guid commandId, DateTimeOffset serverTime) = command switch
        {
            RemoveQueueItemCommand value =>
                (value.UserId, value.CommandId, value.ServerTime),
            SelectQueueItemCommand value =>
                (value.UserId, value.CommandId, value.ServerTime),
            NextQueueItemCommand value => (value.UserId, value.CommandId, value.ServerTime),
            PreviousQueueItemCommand value =>
                (value.UserId, value.CommandId, value.ServerTime),
            _ => throw new InvalidOperationException(
                $"Unsupported coordinated command '{typeof(TCommand).Name}'.")
        };

        return PlayerQueueCommandExecutors.ExecuteCoordinatedAsync(
            store,
            userId,
            commandId,
            serverTime,
            commandType,
            fingerprintValues,
            mutate,
            exception => exception is KeyNotFoundException notFound
                ? DomainFailureMapper.QueueItemNotFound(notFound)
                : null,
            cancellationToken);
    }
}
