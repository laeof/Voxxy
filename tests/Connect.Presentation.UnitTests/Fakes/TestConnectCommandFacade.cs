using Connect.Application.Commands;
using Connect.Application.Results;
using Connect.Presentation.Application;

namespace Connect.Presentation.UnitTests.Fakes;

internal sealed class TestConnectCommandFacade : IConnectCommandFacade
{
    public int Calls { get; private set; }
    public object? LastCommand { get; private set; }
    public ConnectApplicationResult Result { get; set; } =
        new(ConnectCommandStatus.NoChanges);
    public Exception? Exception { get; set; }
    public Queue<ConnectApplicationResult> Results { get; } = new();

    public Task<ConnectApplicationResult> RegisterAsync(
        RegisterConnectionCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> HeartbeatAsync(
        RefreshConnectionLeaseCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> DisconnectAsync(
        DisconnectConnectionCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> SelectDeviceAsync(
        SelectActiveDeviceCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> PlayAsync(
        PlayCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> PauseAsync(
        PauseCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> ChangePositionAsync(
        ChangePositionCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> ChangeVolumeAsync(
        ChangeVolumeCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> AddQueueItemAsync(
        AddQueueItemCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> RemoveQueueItemAsync(
        RemoveQueueItemCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> MoveQueueItemAsync(
        MoveQueueItemCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> SelectQueueItemAsync(
        SelectQueueItemCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> ShuffleQueueAsync(
        ShuffleQueueCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> UnshuffleQueueAsync(
        UnshuffleQueueCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> ChangeRepeatModeAsync(
        ChangeRepeatModeCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> NextQueueItemAsync(
        NextQueueItemCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> PreviousQueueItemAsync(
        PreviousQueueItemCommand command,
        CancellationToken cancellationToken) =>
        Execute(command, cancellationToken);

    public Task<ConnectApplicationResult> GetSnapshotAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken) =>
        Execute((userId, serverTime), cancellationToken);

    private Task<ConnectApplicationResult> Execute(
        object command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastCommand = command;
        ConnectApplicationResult result = Results.Count > 0 ? Results.Dequeue() : Result;
        return Exception is null
            ? Task.FromResult(result)
            : Task.FromException<ConnectApplicationResult>(Exception);
    }
}
