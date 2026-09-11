using System.Globalization;
using Connect.Application.Abstractions.Persistence;
using Connect.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Connect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRedis(configuration);
        services.AddSingleton(ConnectRedisOptions.FromConfiguration(configuration));
        TimeSpan cleanupLeaseTtl =
            TimeSpan.TryParse(
                configuration["Connect:Cleanup:UserLeaseTtl"],
                CultureInfo.InvariantCulture,
                out TimeSpan configuredCleanupLeaseTtl)
                ? configuredCleanupLeaseTtl
                : TimeSpan.FromSeconds(30);
        services.AddSingleton(new ConnectCleanupLeaseOptions(cleanupLeaseTtl));
        services.AddScoped<IConnectStateStore, RedisConnectStateStore>();
        services.AddSingleton<IConnectSessionDiscovery, RedisConnectSessionDiscovery>();
        services.AddSingleton<IConnectCleanupLease, RedisConnectCleanupLease>();

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
