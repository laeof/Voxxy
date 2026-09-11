using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Connect.Presentation.Backplane;

internal sealed class SignalRBackplaneHealthCheck(
    SignalRBackplaneOptions options,
    ILogger<SignalRBackplaneHealthCheck> logger)
    : IHealthCheck
{
    private bool? _lastHealthy;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return HealthCheckResult.Healthy("SignalR Redis backplane is disabled.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.HealthTimeoutMilliseconds);
            ConfigurationOptions healthConfiguration = options.ParseRedisConfiguration();
            healthConfiguration.AbortOnConnectFail = true;
            await using ConnectionMultiplexer connection =
                await ConnectionMultiplexer.ConnectAsync(healthConfiguration)
                    .WaitAsync(timeout.Token);
            await connection.GetDatabase().PingAsync().WaitAsync(timeout.Token);
            LogReadinessChange(healthy: true);
            return HealthCheckResult.Healthy("SignalR Redis backplane is reachable.");
        }
        catch (Exception exception) when (
            exception is RedisException or TimeoutException or OperationCanceledException)
        {
            LogReadinessChange(healthy: false);
            return HealthCheckResult.Unhealthy(
                "SignalR Redis backplane is unavailable.");
        }
    }

    private void LogReadinessChange(bool healthy)
    {
        if (_lastHealthy == healthy)
        {
            return;
        }

        _lastHealthy = healthy;
        LogLevel level = healthy ? LogLevel.Information : LogLevel.Warning;
        if (!logger.IsEnabled(level))
        {
            return;
        }
        logger.Log(
            level,
            "SignalR backplane readiness changed. Healthy={Healthy} RedisEndpoint={RedisEndpoint}",
            healthy,
            options.RedisEndpoint);
    }
}
