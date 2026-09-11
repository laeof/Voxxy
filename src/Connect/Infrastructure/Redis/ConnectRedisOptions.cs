using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Connect.Infrastructure.Redis;

public sealed class ConnectRedisOptions
{
    public const string SectionName = "Connect:Redis";

    public TimeSpan StateTtl { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan CommandDeduplicationTtl { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan ConnectionLeaseTtl { get; init; } = TimeSpan.FromSeconds(45);

    public static ConnectRedisOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ConnectRedisOptions
        {
            StateTtl = ReadTimeSpan(configuration, nameof(StateTtl), TimeSpan.FromDays(7)),
            CommandDeduplicationTtl = ReadTimeSpan(
                configuration,
                nameof(CommandDeduplicationTtl),
                TimeSpan.FromHours(24)),
            ConnectionLeaseTtl = ReadTimeSpan(
                configuration,
                nameof(ConnectionLeaseTtl),
                TimeSpan.FromSeconds(45))
        };

        options.Validate();
        return options;
    }

    public void Validate()
    {
        if (StateTtl < TimeSpan.FromMinutes(1))
        {
            throw new InvalidOperationException("Connect Redis state TTL must be at least one minute.");
        }

        if (CommandDeduplicationTtl < TimeSpan.FromMinutes(1))
        {
            throw new InvalidOperationException(
                "Connect command deduplication TTL must be at least one minute.");
        }

        if (ConnectionLeaseTtl < TimeSpan.FromSeconds(5))
        {
            throw new InvalidOperationException(
                "Connect connection lease TTL must be at least five seconds.");
        }

        if (ConnectionLeaseTtl >= StateTtl)
        {
            throw new InvalidOperationException(
                "Connect connection lease TTL must be shorter than state TTL.");
        }
    }

    private static TimeSpan ReadTimeSpan(
        IConfiguration configuration,
        string propertyName,
        TimeSpan fallback)
    {
        string? value = configuration[$"{SectionName}:{propertyName}"];
        if (value is null)
        {
            return fallback;
        }

        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan parsed)
            ? parsed
            : throw new InvalidOperationException(
                $"Connect Redis option '{propertyName}' is not a valid TimeSpan.");
    }
}
