using Connect.Infrastructure.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Connect.Infrastructure.IntegrationTests;

public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7.2-alpine")
        .Build();

    public IConnectionMultiplexer Multiplexer { get; private set; } = null!;
    public ConnectRedisOptions Options { get; } = new()
    {
        StateTtl = TimeSpan.FromMinutes(5),
        CommandDeduplicationTtl = TimeSpan.FromMinutes(2),
        ConnectionLeaseTtl = TimeSpan.FromSeconds(5)
    };

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(
            _container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await Multiplexer.DisposeAsync();
        await _container.DisposeAsync();
    }

    public RedisConnectStateStore CreateStore() => new(Multiplexer, Options);
}

[CollectionDefinition(Name)]
public sealed class RedisCollection : ICollectionFixture<RedisFixture>
{
    public const string Name = "Connect Redis";
}
