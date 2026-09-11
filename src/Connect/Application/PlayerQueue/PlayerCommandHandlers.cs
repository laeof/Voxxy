using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Common;
using Connect.Application.Results;

namespace Connect.Application.PlayerQueue;

public sealed class PlayHandler(IConnectStateStore store) : IPlayHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        PlayCommand command,
        CancellationToken cancellationToken = default) =>
        PlayerHandler.ExecuteAsync(
            store,
            command,
            nameof(PlayCommand),
            [],
            player => player.Play(command.ServerTime),
            cancellationToken);
}

public sealed class PauseHandler(IConnectStateStore store) : IPauseHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        PauseCommand command,
        CancellationToken cancellationToken = default) =>
        PlayerHandler.ExecuteAsync(
            store,
            command,
            nameof(PauseCommand),
            [],
            player => player.Pause(command.ServerTime),
            cancellationToken);
}

public sealed class ChangePositionHandler(IConnectStateStore store) : IChangePositionHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        ChangePositionCommand command,
        CancellationToken cancellationToken = default) =>
        PlayerHandler.ExecuteAsync(
            store,
            command,
            nameof(ChangePositionCommand),
            [command.PositionMs.ToString(System.Globalization.CultureInfo.InvariantCulture)],
            player => player.Seek(command.PositionMs, command.ServerTime),
            cancellationToken);
}

public sealed class ChangeVolumeHandler(IConnectStateStore store) : IChangeVolumeHandler
{
    public Task<ConnectApplicationResult> HandleAsync(
        ChangeVolumeCommand command,
        CancellationToken cancellationToken = default) =>
        PlayerHandler.ExecuteAsync(
            store,
            command,
            nameof(ChangeVolumeCommand),
            [command.VolumePercent.ToString(System.Globalization.CultureInfo.InvariantCulture)],
            player => player.ChangeVolume(command.VolumePercent),
            cancellationToken);
}

internal static class PlayerHandler
{
    public static Task<ConnectApplicationResult> ExecuteAsync<TCommand>(
        IConnectStateStore store,
        TCommand command,
        string commandType,
        IReadOnlyCollection<string?> fingerprintValues,
        Func<Connect.Domain.Player.PlayerState, bool> mutate,
        CancellationToken cancellationToken)
        where TCommand : class
    {
        string? validationError = command switch
        {
            PlayCommand value => CommandValidation.Validate(value),
            PauseCommand value => CommandValidation.Validate(value),
            ChangePositionCommand value => CommandValidation.Validate(value),
            ChangeVolumeCommand value => CommandValidation.Validate(value),
            _ => throw new InvalidOperationException(
                $"Unsupported Player command '{typeof(TCommand).Name}'.")
        };
        if (validationError is not null)
        {
            return Task.FromResult(HandlerResultFactory.ValidationFailed(validationError));
        }

        (Guid userId, Guid commandId, DateTimeOffset serverTime) = command switch
        {
            PlayCommand value => (value.UserId, value.CommandId, value.ServerTime),
            PauseCommand value => (value.UserId, value.CommandId, value.ServerTime),
            ChangePositionCommand value =>
                (value.UserId, value.CommandId, value.ServerTime),
            ChangeVolumeCommand value => (value.UserId, value.CommandId, value.ServerTime),
            _ => throw new InvalidOperationException(
                $"Unsupported Player command '{typeof(TCommand).Name}'.")
        };

        return PlayerQueueCommandExecutors.ExecutePlayerAsync(
            store,
            userId,
            commandId,
            serverTime,
            commandType,
            fingerprintValues,
            mutate,
            cancellationToken);
    }
}
