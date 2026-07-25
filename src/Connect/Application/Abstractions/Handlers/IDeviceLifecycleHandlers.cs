using Connect.Application.Commands;
using Connect.Application.Results;

namespace Connect.Application.Abstractions.Handlers;

public interface IRegisterConnectionHandler
{
    Task<ConnectApplicationResult> HandleAsync(
        RegisterConnectionCommand command,
        CancellationToken cancellationToken = default);
}

public interface IRefreshConnectionLeaseHandler
{
    Task<ConnectApplicationResult> HandleAsync(
        RefreshConnectionLeaseCommand command,
        CancellationToken cancellationToken = default);
}

public interface IDisconnectConnectionHandler
{
    Task<ConnectApplicationResult> HandleAsync(
        DisconnectConnectionCommand command,
        CancellationToken cancellationToken = default);
}

public interface IExpireConnectionsHandler
{
    Task<ConnectApplicationResult> HandleAsync(
        ExpireConnectionsCommand command,
        CancellationToken cancellationToken = default);
}

public interface IDetectMissingLeasesService
{
    Task<ConnectApplicationResult> ExecuteAsync(
        DetectMissingLeasesCommand command,
        CancellationToken cancellationToken = default);
}

public interface ISelectActiveDeviceHandler
{
    Task<ConnectApplicationResult> HandleAsync(
        SelectActiveDeviceCommand command,
        CancellationToken cancellationToken = default);
}
