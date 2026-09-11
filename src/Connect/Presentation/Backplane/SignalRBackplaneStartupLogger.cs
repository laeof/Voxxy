using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Connect.Presentation.Backplane;

internal sealed partial class SignalRBackplaneStartupLogger(
    SignalRBackplaneOptions options,
    IHostEnvironment environment,
    ILogger<SignalRBackplaneStartupLogger> logger)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        BackplaneConfigured(
            logger,
            options.Enabled,
            options.ChannelPrefix,
            options.RedisEndpoint,
            environment.EnvironmentName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1900,
        Level = LogLevel.Information,
        Message = "SignalR Redis backplane configured. BackplaneEnabled={BackplaneEnabled} ChannelPrefix={ChannelPrefix} RedisEndpoint={RedisEndpoint} Environment={Environment}")]
    private static partial void BackplaneConfigured(
        ILogger logger,
        bool backplaneEnabled,
        string channelPrefix,
        string redisEndpoint,
        string environment);
}
