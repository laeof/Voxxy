using Connect.Presentation.Application;
using Connect.Presentation.Backplane;
using Connect.Presentation.Broadcasting;
using Connect.Presentation.Cleanup;
using Connect.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

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
        var backplaneOptions =
            SignalRBackplaneOptions.FromConfiguration(configuration);
        var transportOptions = new ConnectTransportOptions
        {
            MaximumReceiveMessageSize =
                configuration.GetValue<long?>(
                    $"{ConnectTransportOptions.SectionName}:MaximumReceiveMessageSize")
                ?? ConnectTransportOptions.DefaultMaximumReceiveMessageSize,
            AllowedOrigins = new HashSet<string>(
                configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [],
                StringComparer.OrdinalIgnoreCase)
        };
        transportOptions.Validate();

        ISignalRServerBuilder signalR = services.AddSignalR();
        services.Configure<HubOptions<PlayerHub>>(
            options =>
                options.MaximumReceiveMessageSize = transportOptions.MaximumReceiveMessageSize);
        if (backplaneOptions.Enabled)
        {
            signalR.AddStackExchangeRedis(
                backplaneOptions.ConnectionString!,
                redis =>
                {
                    redis.Configuration.ChannelPrefix =
                        RedisChannel.Literal(backplaneOptions.ChannelPrefix);
                    // A syntactically valid but temporarily unavailable Redis endpoint is a
                    // readiness failure. The official backplane owns reconnect recovery.
                    redis.Configuration.AbortOnConnectFail = false;
                });
        }
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(transportOptions);
        services.AddSingleton<ConnectInvocationRateLimiter>();
        services.AddSingleton<ConnectTransportMetrics>();
        services.AddSingleton(backplaneOptions);
        services.AddHostedService<SignalRBackplaneStartupLogger>();
        services.AddHealthChecks()
            .AddCheck<SignalRBackplaneHealthCheck>(
                "signalr-backplane",
                tags: ["ready", "signalr", "redis"]);
        services.AddSingleton(cleanupOptions);
        services.AddSingleton<ConnectCleanupMetrics>();
        services.AddScoped<IConnectCommandFacade, ConnectCommandFacade>();
        services.AddSingleton<IConnectBroadcaster, ConnectBroadcaster>();
        services.AddHostedService<ConnectLeaseCleanupWorker>();

        return services;
    }
}
