using Connect.Presentation.Backplane;
using Connect.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Connect.Presentation.UnitTests.Backplane;

public sealed class SignalRBackplaneOptionsTests
{
    [Fact]
    public void SignalRBackplane_Disabled_AllowsLocalSignalR()
    {
        IConfiguration configuration = Configuration(
            ("SignalR:Backplane:Enabled", "false"));

        var options =
            SignalRBackplaneOptions.FromConfiguration(configuration);

        options.Enabled.ShouldBeFalse();
        options.ChannelPrefix.ShouldBe(SignalRBackplaneOptions.DefaultChannelPrefix);
    }

    [Fact]
    public void SignalRBackplane_Enabled_UsesConfiguredChannelPrefix()
    {
        IConfiguration configuration = Configuration(
            ("SignalR:Backplane:Enabled", "true"),
            ("SignalR:Backplane:ConnectionString", "redis.internal:6379,password=secret"),
            ("SignalR:Backplane:ChannelPrefix", "voxxy:signalr:test"));

        var options =
            SignalRBackplaneOptions.FromConfiguration(configuration);

        options.Enabled.ShouldBeTrue();
        options.ChannelPrefix.ShouldBe("voxxy:signalr:test");
    }

    [Fact]
    public void SignalRBackplane_MissingConnectionString_FailsValidation()
    {
        IConfiguration configuration = Configuration(
            ("SignalR:Backplane:Enabled", "true"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            SignalRBackplaneOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain("connection string");
    }

    [Fact]
    public void SignalRBackplane_DoesNotExposeRedisPasswordInLogs()
    {
        IConfiguration configuration = Configuration(
            ("SignalR:Backplane:Enabled", "true"),
            ("SignalR:Backplane:ConnectionString", "redis.internal:6379,password=top-secret"));
        var options =
            SignalRBackplaneOptions.FromConfiguration(configuration);

        options.RedisEndpoint.ShouldContain("redis.internal:6379");
        options.RedisEndpoint.ShouldNotContain("secret");
    }

    [Fact]
    public void SignalRBackplane_RejectsConnectPersistenceNamespace()
    {
        IConfiguration configuration = Configuration(
            ("SignalR:Backplane:Enabled", "false"),
            ("SignalR:Backplane:ChannelPrefix", "connect:v2:signalr"));

        Should.Throw<InvalidOperationException>(() =>
            SignalRBackplaneOptions.FromConfiguration(configuration));
    }

    [Theory]
    [InlineData("connect:v2")]
    [InlineData("CONNECT:V2:signalr")]
    public void BackplaneOptions_RejectAuthoritativeKeyPrefix(string prefix)
    {
        IConfiguration configuration = Configuration(
            ("SignalR:Backplane:Enabled", "false"),
            ("SignalR:Backplane:ChannelPrefix", prefix));

        Should.Throw<InvalidOperationException>(() =>
            SignalRBackplaneOptions.FromConfiguration(configuration));
    }

    [Fact]
    public void BackplaneOptions_AllowEnvironmentSpecificSignalRPrefix()
    {
        var options = SignalRBackplaneOptions.FromConfiguration(
            Configuration(
                ("SignalR:Backplane:Enabled", "true"),
                ("SignalR:Backplane:ConnectionString", "localhost:6379"),
                ("SignalR:Backplane:ChannelPrefix", "voxxy:signalr:production")));

        options.ChannelPrefix.ShouldBe("voxxy:signalr:production");
    }

    [Fact]
    public void BackplaneEnabled_InvalidRedisConfigurationFailsStartup()
    {
        Should.Throw<Exception>(() =>
            SignalRBackplaneOptions.FromConfiguration(
                Configuration(
                    ("SignalR:Backplane:Enabled", "true"),
                    ("SignalR:Backplane:ConnectionString", "localhost:6379,connectTimeout=invalid"))));
    }

    [Fact]
    public void SignalRBackplane_Disabled_RegistersLocalSignalR()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConnectModulePresentation(
            Configuration(("SignalR:Backplane:Enabled", "false")));
        using ServiceProvider provider = services.BuildServiceProvider();

        HubLifetimeManager<PlayerHub> manager =
            provider.GetRequiredService<HubLifetimeManager<PlayerHub>>();

        manager.GetType().Name.ShouldBe("DefaultHubLifetimeManager`1");
    }

    [Fact]
    public void SignalRBackplane_Enabled_RegistersRedisBackplane()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConnectModulePresentation(
            Configuration(
                ("SignalR:Backplane:Enabled", "true"),
                ("SignalR:Backplane:ConnectionString", "localhost:6379"),
                ("SignalR:Backplane:ChannelPrefix", "voxxy:signalr:test")));
        using ServiceProvider provider = services.BuildServiceProvider();

        HubLifetimeManager<PlayerHub> manager =
            provider.GetRequiredService<HubLifetimeManager<PlayerHub>>();

        manager.GetType().Name.ShouldContain("RedisHubLifetimeManager");
    }

    [Fact]
    public void BackplaneHealth_IsReadinessOnly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConnectModulePresentation(
            Configuration(("SignalR:Backplane:Enabled", "false")));
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = provider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations
            .Single(item => item.Name == "signalr-backplane");

        registration.Tags.ShouldContain("ready");
        registration.Tags.ShouldContain("signalr");
        registration.Tags.ShouldContain("redis");
        registration.Tags.ShouldNotContain("live");
    }

    private static IConfiguration Configuration(
        params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.ToDictionary(
                    pair => pair.Key,
                    pair => (string?)pair.Value,
                    StringComparer.Ordinal))
            .Build();
}
