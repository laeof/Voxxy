using Microsoft.Extensions.DependencyInjection;

namespace Connect.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModulePresentation(this IServiceCollection services)
    {
        services.AddSignalR();

        return services;
    }
}