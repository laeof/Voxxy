using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Connect.Application.Results;
using Connect.Presentation.Application;
using Connect.Presentation.Broadcasting;
using Connect.Presentation.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Connect.MultiInstance.IntegrationTests;

public sealed class MultiInstanceFixture : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7.2-alpine")
        .Build();

    public TestConnectHost Instance1 { get; private set; } = null!;
    public TestConnectHost Instance2 { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        string prefix = $"voxxy:signalr:test:{Guid.NewGuid():N}";
        Instance1 = await TestConnectHost.StartAsync(_redis.GetConnectionString(), prefix);
        Instance2 = await TestConnectHost.StartAsync(_redis.GetConnectionString(), prefix);
    }

    public async Task DisposeAsync()
    {
        if (Instance1 is not null)
        {
            await Instance1.DisposeAsync();
        }
        if (Instance2 is not null)
        {
            await Instance2.DisposeAsync();
        }
        await _redis.DisposeAsync();
    }
}

public sealed class TestConnectHost : IAsyncDisposable
{
    private readonly WebApplication _application;

    private TestConnectHost(
        WebApplication application,
        Uri baseAddress,
        FacadeProxy facade)
    {
        _application = application;
        BaseAddress = baseAddress;
        Facade = facade;
    }

    public Uri BaseAddress { get; }
    public FacadeProxy Facade { get; }
    public IConnectBroadcaster Broadcaster =>
        _application.Services.GetRequiredService<IConnectBroadcaster>();

    public static async Task<TestConnectHost> StartAsync(
        string redisConnectionString,
        string channelPrefix)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services
            .AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization();
        builder.Services
            .AddSignalR()
            .AddStackExchangeRedis(
                redisConnectionString,
                options =>
                {
                    options.Configuration.ChannelPrefix = RedisChannel.Literal(channelPrefix);
                    options.Configuration.AbortOnConnectFail = true;
                });
        builder.Services.AddSingleton(TimeProvider.System);
        IConnectCommandFacade facade =
            DispatchProxy.Create<IConnectCommandFacade, FacadeProxy>();
        builder.Services.AddSingleton(facade);
        builder.Services.AddSingleton<IConnectBroadcaster, ConnectBroadcaster>();

        WebApplication application = builder.Build();
        application.UseAuthentication();
        application.UseAuthorization();
        application.MapHub<PlayerHub>("/api/hubs/connect");
        await application.StartAsync();
        string address = application.Services
            .GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .Single();
        return new TestConnectHost(application, new Uri(address), (FacadeProxy)(object)facade);
    }

    public async ValueTask DisposeAsync()
    {
        await _application.StopAsync();
        await _application.DisposeAsync();
    }
}

public class FacadeProxy : DispatchProxy
{
    public ConnectApplicationResult Result { get; set; } =
        new(ConnectCommandStatus.NoChanges);
    public int Calls { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Calls++;
        return Task.FromResult(Result);
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string token = Request.Headers.Authorization
            .ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (!Guid.TryParse(token, out Guid userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("A GUID bearer token is required."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString("D"))],
            SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

[CollectionDefinition(Name)]
public sealed class MultiInstanceCollection : ICollectionFixture<MultiInstanceFixture>
{
    public const string Name = "Connect multi-instance";
}
