using Connect.Application.Abstractions.Handlers;
using Connect.Application.DeviceLifecycle;
using Connect.Application.PlayerQueue;
using Connect.Domain.Synchronization;
using Microsoft.Extensions.DependencyInjection;

namespace Connect.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModuleApplication(this IServiceCollection services)
    {
        services.AddScoped<ConnectStateCoordinator>();
        services.AddScoped<IRegisterConnectionHandler, RegisterConnectionHandler>();
        services.AddScoped<IRefreshConnectionLeaseHandler, RefreshConnectionLeaseHandler>();
        services.AddScoped<IDisconnectConnectionHandler, DisconnectConnectionHandler>();
        services.AddScoped<IExpireConnectionsHandler, ExpireConnectionsHandler>();
        services.AddScoped<IDetectMissingLeasesService, DetectMissingLeasesService>();
        services.AddScoped<ISelectActiveDeviceHandler, SelectActiveDeviceHandler>();
        services.AddScoped<IPlayHandler, PlayHandler>();
        services.AddScoped<IPauseHandler, PauseHandler>();
        services.AddScoped<IChangePositionHandler, ChangePositionHandler>();
        services.AddScoped<IChangeVolumeHandler, ChangeVolumeHandler>();
        services.AddScoped<IAddQueueItemHandler, AddQueueItemHandler>();
        services.AddScoped<IRemoveQueueItemHandler, RemoveQueueItemHandler>();
        services.AddScoped<IMoveQueueItemHandler, MoveQueueItemHandler>();
        services.AddScoped<ISelectQueueItemHandler, SelectQueueItemHandler>();
        services.AddScoped<IShuffleQueueHandler, ShuffleQueueHandler>();
        services.AddScoped<IUnshuffleQueueHandler, UnshuffleQueueHandler>();
        services.AddScoped<IChangeRepeatModeHandler, ChangeRepeatModeHandler>();
        services.AddScoped<INextQueueItemHandler, NextQueueItemHandler>();
        services.AddScoped<IPreviousQueueItemHandler, PreviousQueueItemHandler>();

        return services;
    }
}
