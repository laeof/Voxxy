using Connect.Presentation.Application;
using Connect.Presentation.Broadcasting;
using Connect.Presentation.Cleanup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Connect.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModulePresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ConnectCleanupOptions cleanupOptions =
            configuration.GetSection(ConnectCleanupOptions.SectionName)
                .Get<ConnectCleanupOptions>()
            ?? new ConnectCleanupOptions();
        cleanupOptions.Validate();

        services.AddSignalR();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(cleanupOptions);
        services.AddSingleton<ConnectCleanupMetrics>();
        services.AddScoped<IConnectCommandFacade, ConnectCommandFacade>();
        services.AddSingleton<IConnectBroadcaster, ConnectBroadcaster>();
        services.AddHostedService<ConnectLeaseCleanupWorker>();

        return services;
    }
}
