using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Connect.Presentation.Backplane;

internal sealed class SignalRBackplaneHealthCheck(
    SignalRBackplaneOptions options,
    IConnectionMultiplexer sharedRedis)
    : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("SignalR Redis backplane is disabled."));
        }

        return Task.FromResult(
            sharedRedis.IsConnected
                ? HealthCheckResult.Healthy("Shared Redis deployment is connected.")
                : HealthCheckResult.Unhealthy("Shared Redis deployment is unavailable."));
    }
}
