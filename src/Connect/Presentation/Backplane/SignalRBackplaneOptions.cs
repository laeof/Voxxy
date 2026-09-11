using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Connect.Presentation.Backplane;

public sealed class SignalRBackplaneOptions
{
    public const string SectionName = "SignalR:Backplane";
    public const string DefaultChannelPrefix = "voxxy:signalr";
    public const int DefaultHealthTimeoutMilliseconds = 2_000;

    public bool Enabled { get; init; }
    public string? ConnectionString { get; init; }
    public string ChannelPrefix { get; init; } = DefaultChannelPrefix;
    public int HealthTimeoutMilliseconds { get; init; } = DefaultHealthTimeoutMilliseconds;

    public static SignalRBackplaneOptions FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(SectionName);
        bool enabled = bool.TryParse(section[nameof(Enabled)], out bool value) && value;
        string? connectionString =
            section[nameof(ConnectionString)] ?? configuration.GetConnectionString("Redis");
        string channelPrefix =
            section[nameof(ChannelPrefix)] ?? DefaultChannelPrefix;
        int healthTimeoutMilliseconds =
            int.TryParse(
                section[nameof(HealthTimeoutMilliseconds)],
                out int configuredHealthTimeout)
                ? configuredHealthTimeout
                : DefaultHealthTimeoutMilliseconds;
        var options = new SignalRBackplaneOptions
        {
            Enabled = enabled,
            ConnectionString = connectionString,
            ChannelPrefix = channelPrefix,
            HealthTimeoutMilliseconds = healthTimeoutMilliseconds
        };
        options.Validate();
        return options;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ChannelPrefix))
        {
            throw new InvalidOperationException(
                "SignalR Redis backplane channel prefix is required.");
        }
        string normalizedPrefix = ChannelPrefix.Trim();
        if (normalizedPrefix.Equals("connect:v2", StringComparison.OrdinalIgnoreCase) ||
            normalizedPrefix.StartsWith("connect:v2:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SignalR Redis backplane must not use the Connect persistence namespace.");
        }
        if (Enabled && string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "SignalR Redis backplane connection string is required when enabled.");
        }
        if (HealthTimeoutMilliseconds <= 0)
        {
            throw new InvalidOperationException(
                "SignalR Redis backplane health timeout must be greater than zero.");
        }
        if (Enabled)
        {
            _ = ParseRedisConfiguration();
        }
    }

    public ConfigurationOptions ParseRedisConfiguration()
    {
        var configuration =
            ConfigurationOptions.Parse(ConnectionString ?? string.Empty);
        configuration.AbortOnConnectFail = false;
        configuration.ConnectTimeout = HealthTimeoutMilliseconds;
        configuration.SyncTimeout = HealthTimeoutMilliseconds;
        return configuration;
    }

    public string RedisEndpoint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                return "not-configured";
            }

            ConfigurationOptions configuration = ParseRedisConfiguration();
            return string.Join(",", configuration.EndPoints.Select(endpoint => endpoint.ToString()));
        }
    }
}
