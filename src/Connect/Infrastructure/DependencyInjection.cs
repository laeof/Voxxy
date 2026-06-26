using Connect.Application.Abstractions.Repositories;
using Connect.Application.Abstractions.Services;
using Connect.Domain.Devices;
using Connect.Domain.Player;
using Connect.Domain.Queue;
using Connect.Infrastructure.Redis;
using Connect.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Connect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRedis(configuration);

        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICacheRepository<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            string connectionString =
                configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Redis connection string is missing");

            return ConnectionMultiplexer.Connect(connectionString);
        });

        return services;
    }
}