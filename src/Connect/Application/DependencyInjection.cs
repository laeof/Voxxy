using Connect.Application.Abstractions.Handlers;
using Connect.Application.DeviceLifecycle;
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

        return services;
    }
}
