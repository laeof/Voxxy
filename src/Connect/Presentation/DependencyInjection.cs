using Connect.Presentation.Application;
using Connect.Presentation.Broadcasting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Connect.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModulePresentation(this IServiceCollection services)
    {
        services.AddSignalR();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IConnectCommandFacade, ConnectCommandFacade>();
        services.AddSingleton<IConnectBroadcaster, ConnectBroadcaster>();

        return services;
    }
}
