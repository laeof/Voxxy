using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Connect.Presentation.Backplane;

public sealed class SignalRBackplaneOptions
{
    public const string SectionName = "SignalR:Backplane";
    public const string DefaultChannelPrefix = "voxxy:signalr";

    public bool Enabled { get; init; }
    public string? ConnectionString { get; init; }
    public string ChannelPrefix { get; init; } = DefaultChannelPrefix;

    public static SignalRBackplaneOptions FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(SectionName);
        bool enabled = bool.TryParse(section[nameof(Enabled)], out bool value) && value;
        string? connectionString =
            section[nameof(ConnectionString)] ?? configuration.GetConnectionString("Redis");
        string channelPrefix =
            section[nameof(ChannelPrefix)] ?? DefaultChannelPrefix;
        var options = new SignalRBackplaneOptions
        {
            Enabled = enabled,
            ConnectionString = connectionString,
            ChannelPrefix = channelPrefix
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
        if (ChannelPrefix.StartsWith("connect:v2", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "SignalR Redis backplane must not use the Connect persistence namespace.");
        }
        if (Enabled && string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "SignalR Redis backplane connection string is required when enabled.");
        }
    }

    public string RedisEndpoint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                return "not-configured";
            }

            var configuration =
                ConfigurationOptions.Parse(ConnectionString);
            return string.Join(",", configuration.EndPoints.Select(endpoint => endpoint.ToString()));
        }
    }
}
