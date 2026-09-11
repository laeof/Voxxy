using System.Globalization;
using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;
using Connect.Domain.Queue;

namespace Connect.Application.PlayerQueue;

public sealed class AddQueueItemHandler(IConnectStateStore store) : IAddQueueItemHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        AddQueueItemCommand command,
        CancellationToken cancellationToken = default)
    {
        string? error = CommandValidation.Validate(command);
        if (error is not null)
        {
            return Task.FromResult(HandlerResultFactory.ValidationFailed(error));
        }

        return PlayerQueueCommandExecutors.ExecuteQueueAsync(
            store,
            command.UserId,
            command.CommandId,
            nameof(AddQueueItemCommand),
            [
                CommandFingerprint.GuidValue(command.QueueItemId),
                CommandFingerprint.GuidValue(command.TrackId)
            ],
            queue =>
            {
                long version = queue.Version;
                queue.Add(command.TrackId, command.QueueItemId);
                return queue.Version != version;
            },
            queue => new ConnectCommandOutcome(
                QueueItemId: command.QueueItemId,
                QueueVersion: queue.Version),
            null,
            cancellationToken);
    }
}

public sealed class MoveQueueItemHandler(IConnectStateStore store) : IMoveQueueItemHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        MoveQueueItemCommand command,
        CancellationToken cancellationToken = default)
    {
        string? error = CommandValidation.Validate(command);
        if (error is not null)
        {
            return Task.FromResult(HandlerResultFactory.ValidationFailed(error));
        }

        return PlayerQueueCommandExecutors.ExecuteQueueAsync(
            store,
            command.UserId,
            command.CommandId,
            nameof(MoveQueueItemCommand),
            [
                CommandFingerprint.GuidValue(command.QueueItemId),
                command.TargetIndex.ToString(CultureInfo.InvariantCulture)
            ],
            queue => queue.Reorder(command.QueueItemId, command.TargetIndex),
            null,
            exception => exception switch
            {
                KeyNotFoundException value =>
                    DomainFailureMapper.QueueItemNotFound(value),
                ArgumentOutOfRangeException value =>
                    DomainFailureMapper.InvalidQueueIndex(value),
                _ => null
            },
            cancellationToken);
    }
}

public sealed class ShuffleQueueHandler(IConnectStateStore store) : IShuffleQueueHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        ShuffleQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        string? error = CommandValidation.Validate(command);
        if (error is not null)
        {
            return Task.FromResult(HandlerResultFactory.ValidationFailed(error));
        }

        return PlayerQueueCommandExecutors.ExecuteQueueAsync(
            store,
            command.UserId,
            command.CommandId,
            nameof(ShuffleQueueCommand),
            [command.Seed.ToString(CultureInfo.InvariantCulture)],
            queue => queue.Shuffle(
                DeterministicShuffle.CreateOrder(
                    queue.Items.Select(item => item.QueueItemId),
                    command.Seed)),
            null,
            null,
            cancellationToken);
    }
}

public sealed class UnshuffleQueueHandler(IConnectStateStore store) : IUnshuffleQueueHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        UnshuffleQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        string? error = CommandValidation.Validate(command);
        if (error is not null)
        {
            return Task.FromResult(HandlerResultFactory.ValidationFailed(error));
        }

        return PlayerQueueCommandExecutors.ExecuteQueueAsync(
            store,
            command.UserId,
            command.CommandId,
            nameof(UnshuffleQueueCommand),
            [],
            queue => queue.Unshuffle(),
            null,
            null,
            cancellationToken);
    }
}

public sealed class ChangeRepeatModeHandler(IConnectStateStore store)
    : IChangeRepeatModeHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        ChangeRepeatModeCommand command,
        CancellationToken cancellationToken = default)
    {
        string? error = CommandValidation.Validate(command);
        if (error is not null)
        {
            return Task.FromResult(HandlerResultFactory.ValidationFailed(error));
        }

        return PlayerQueueCommandExecutors.ExecuteQueueAsync(
            store,
            command.UserId,
            command.CommandId,
            nameof(ChangeRepeatModeCommand),
            [((int)command.RepeatMode).ToString(CultureInfo.InvariantCulture)],
            queue => queue.SetRepeatMode(command.RepeatMode),
            null,
            null,
            cancellationToken);
    }
}
