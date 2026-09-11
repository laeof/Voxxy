using Connect.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Connect.Infrastructure.IntegrationTests;

public sealed class ConnectRedisOptionsTests
{
    [Fact]
    public void Defaults_AreValidAndLeaseIsShorterThanState()
    {
        var options = ConnectRedisOptions.FromConfiguration(
            new ConfigurationBuilder().Build());

        Should.NotThrow(options.Validate);
        options.ConnectionLeaseTtl.ShouldBeLessThan(options.StateTtl);
    }

    [Fact]
    public void Validate_RejectsTooSmallAndInconsistentValues()
    {
        var options = new ConnectRedisOptions
        {
            StateTtl = TimeSpan.FromSeconds(30),
            CommandDeduplicationTtl = TimeSpan.FromSeconds(30),
            ConnectionLeaseTtl = TimeSpan.FromSeconds(2)
        };

        Should.Throw<InvalidOperationException>(options.Validate);
    }
}
