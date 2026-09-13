using Connect.Application.Abstractions.Persistence;
using StackExchange.Redis;

namespace Connect.Infrastructure.Redis;

public sealed class RedisConnectCleanupLease(
    IConnectionMultiplexer multiplexer,
    ConnectCleanupLeaseOptions options)
    : IConnectCleanupLease
{
    private const string ReleaseScript =
        """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string ownerToken = Guid.NewGuid().ToString("N");
        bool acquired;
        try
        {
            acquired = await _database.StringSetAsync(
                ConnectRedisKeys.CleanupLease(userId),
                ownerToken,
                options.Ttl,
                When.NotExists);
        }
        catch (RedisException exception)
        {
            throw new ConnectInfrastructureUnavailableException(
                "Connect cleanup lease storage is unavailable.",
                exception);
        }
        return acquired
            ? new LeaseHandle(
                _database,
                ConnectRedisKeys.CleanupLease(userId),
                ownerToken)
            : null;
    }

    private sealed class LeaseHandle(
        IDatabase database,
        RedisKey key,
        RedisValue ownerToken)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await database.ScriptEvaluateAsync(
                    ReleaseScript,
                    [key],
                    [ownerToken]);
            }
            catch (RedisException)
            {
                // TTL guarantees eventual release when Redis is temporarily unavailable.
            }
        }
    }
}

public sealed record ConnectCleanupLeaseOptions(TimeSpan Ttl);
