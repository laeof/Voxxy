using Connect.Presentation;
using Web.Api.Factories;
using Web.Api.Infrastructure;

namespace Web.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddSingleton<CookieOptionsFactory>();

        return services;
    }

    public static IServiceCollection AddApplicationModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConnectModule(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapApplicationModules(this IEndpointRouteBuilder app)
    {
        app.MapConnectModule();

        return app;
    }
}
