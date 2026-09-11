using Connect.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace Connect.MultiInstance.IntegrationTests;

[Collection(MultiInstanceCollection.Name)]
public sealed class BackplaneReadinessTests(MultiInstanceFixture fixture)
{
    [Fact]
    public async Task BackplaneRedisUnavailable_MarksReadinessUnhealthy()
    {
        using ServiceProvider provider = Provider("127.0.0.1:1,connectTimeout=100");

        HealthReport report = await provider
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Tags.Contains("ready"));

        report.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task BackplaneRedisUnavailable_DoesNotFailLiveness()
    {
        using ServiceProvider provider = Provider("127.0.0.1:1,connectTimeout=100");

        HealthReport report = await provider
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Tags.Contains("live"));

        report.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task BackplaneRedisRecovers_ReadinessBecomesHealthy()
    {
        using ServiceProvider unavailable = Provider("127.0.0.1:1,connectTimeout=100");
        HealthReport before = await unavailable
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Tags.Contains("ready"));

        using ServiceProvider recovered = Provider(fixture.RedisConnectionString);
        HealthReport after = await recovered
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Tags.Contains("ready"));

        before.Status.ShouldBe(HealthStatus.Unhealthy);
        after.Status.ShouldBe(HealthStatus.Healthy);
    }

    private static ServiceProvider Provider(string connectionString)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SignalR:Backplane:Enabled"] = "true",
                    ["SignalR:Backplane:ConnectionString"] = connectionString,
                    ["SignalR:Backplane:ChannelPrefix"] = "voxxy:signalr:health-test",
                    ["SignalR:Backplane:HealthTimeoutMilliseconds"] = "250",
                })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConnectModulePresentation(configuration);
        return services.BuildServiceProvider();
    }
}
