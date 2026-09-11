using Connect.Application.Abstractions.Handlers;
using Connect.Application.Commands;
using Connect.Application.Results;

namespace Connect.Presentation.Application;

public sealed class ConnectCommandFacade(
    IRegisterConnectionHandler register,
    IRefreshConnectionLeaseHandler heartbeat,
    IDisconnectConnectionHandler disconnect,
    ISelectActiveDeviceHandler selectDevice,
    IPlayHandler play,
    IPauseHandler pause,
    IChangePositionHandler changePosition,
    IChangeVolumeHandler changeVolume,
    IAddQueueItemHandler addQueueItem,
    IRemoveQueueItemHandler removeQueueItem,
    IMoveQueueItemHandler moveQueueItem,
    ISelectQueueItemHandler selectQueueItem,
    IShuffleQueueHandler shuffleQueue,
    IUnshuffleQueueHandler unshuffleQueue,
    IChangeRepeatModeHandler changeRepeatMode,
    INextQueueItemHandler nextQueueItem,
    ICompleteCurrentTrackHandler completeCurrentTrack,
    IStartPlaybackContextHandler startPlaybackContext,
    IPreviousQueueItemHandler previousQueueItem,
    IConnectSnapshotReader snapshotReader)
    : IConnectCommandFacade
{
    public Task<ConnectApplicationResult> RegisterAsync(
        RegisterConnectionCommand command,
        CancellationToken cancellationToken) =>
        register.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> HeartbeatAsync(
        RefreshConnectionLeaseCommand command,
        CancellationToken cancellationToken) =>
        heartbeat.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> DisconnectAsync(
        DisconnectConnectionCommand command,
        CancellationToken cancellationToken) =>
        disconnect.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> SelectDeviceAsync(
        SelectActiveDeviceCommand command,
        CancellationToken cancellationToken) =>
        selectDevice.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> PlayAsync(
        PlayCommand command,
        CancellationToken cancellationToken) =>
        play.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> PauseAsync(
        PauseCommand command,
        CancellationToken cancellationToken) =>
        pause.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> ChangePositionAsync(
        ChangePositionCommand command,
        CancellationToken cancellationToken) =>
        changePosition.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> ChangeVolumeAsync(
        ChangeVolumeCommand command,
        CancellationToken cancellationToken) =>
        changeVolume.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> AddQueueItemAsync(
        AddQueueItemCommand command,
        CancellationToken cancellationToken) =>
        addQueueItem.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> RemoveQueueItemAsync(
        RemoveQueueItemCommand command,
        CancellationToken cancellationToken) =>
        removeQueueItem.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> MoveQueueItemAsync(
        MoveQueueItemCommand command,
        CancellationToken cancellationToken) =>
        moveQueueItem.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> SelectQueueItemAsync(
        SelectQueueItemCommand command,
        CancellationToken cancellationToken) =>
        selectQueueItem.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> ShuffleQueueAsync(
        ShuffleQueueCommand command,
        CancellationToken cancellationToken) =>
        shuffleQueue.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> UnshuffleQueueAsync(
        UnshuffleQueueCommand command,
        CancellationToken cancellationToken) =>
        unshuffleQueue.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> ChangeRepeatModeAsync(
        ChangeRepeatModeCommand command,
        CancellationToken cancellationToken) =>
        changeRepeatMode.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> NextQueueItemAsync(
        NextQueueItemCommand command,
        CancellationToken cancellationToken) =>
        nextQueueItem.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> CompleteCurrentTrackAsync(
        CompleteCurrentTrackCommand command,
        CancellationToken cancellationToken) =>
        completeCurrentTrack.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> StartPlaybackContextAsync(
        StartPlaybackContextCommand command,
        CancellationToken cancellationToken) =>
        startPlaybackContext.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> PreviousQueueItemAsync(
        PreviousQueueItemCommand command,
        CancellationToken cancellationToken) =>
        previousQueueItem.HandleAsync(command, cancellationToken);

    public Task<ConnectApplicationResult> GetSnapshotAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken) =>
        snapshotReader.GetAsync(userId, serverTime, cancellationToken);
}
