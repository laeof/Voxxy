using Microsoft.Extensions.DependencyInjection;

namespace Connect.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModuleApplication(this IServiceCollection services)
    {
        return services;
    }
}
