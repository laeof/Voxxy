using Connect.Application.Commands;
using Connect.Application.Results;

namespace Connect.Application.Abstractions.Handlers;

public interface IPlayHandler : IConnectCommandHandler<PlayCommand>;
public interface IPauseHandler : IConnectCommandHandler<PauseCommand>;
public interface IChangePositionHandler : IConnectCommandHandler<ChangePositionCommand>;
public interface IChangeVolumeHandler : IConnectCommandHandler<ChangeVolumeCommand>;
public interface IAddQueueItemHandler : IConnectCommandHandler<AddQueueItemCommand>;
public interface IRemoveQueueItemHandler : IConnectCommandHandler<RemoveQueueItemCommand>;
public interface IMoveQueueItemHandler : IConnectCommandHandler<MoveQueueItemCommand>;
public interface ISelectQueueItemHandler : IConnectCommandHandler<SelectQueueItemCommand>;
public interface IShuffleQueueHandler : IConnectCommandHandler<ShuffleQueueCommand>;
public interface IUnshuffleQueueHandler : IConnectCommandHandler<UnshuffleQueueCommand>;
public interface IChangeRepeatModeHandler : IConnectCommandHandler<ChangeRepeatModeCommand>;
public interface INextQueueItemHandler : IConnectCommandHandler<NextQueueItemCommand>;
public interface IPreviousQueueItemHandler : IConnectCommandHandler<PreviousQueueItemCommand>;

public interface IConnectCommandHandler<in TCommand>
{
    Task<ConnectApplicationResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);
}
