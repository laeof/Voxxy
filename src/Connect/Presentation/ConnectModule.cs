using Connect.Application;
using Connect.Infrastructure;
using Connect.Presentation.Endpoints;
using Connect.Presentation.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Connect.Presentation;

public static class ConnectModule
{
    public static IServiceCollection AddConnectModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConnectModulePresentation();
        services.AddConnectModuleApplication();
        services.AddConnectModuleInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapConnectModule(this IEndpointRouteBuilder app)
    {
        app.MapConnectEndpoints();
        app.MapHub<PlayerHub>("/hubs/player");

        return app;
    }
}