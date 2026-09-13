using System.Runtime.CompilerServices;
using Connect.Application.Abstractions.Persistence;
using StackExchange.Redis;

namespace Connect.Infrastructure.Redis;

public sealed class RedisConnectSessionDiscovery(IConnectionMultiplexer multiplexer)
    : IConnectSessionDiscovery
{
    private readonly IConnectionMultiplexer _multiplexer = multiplexer;
    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async IAsyncEnumerable<Guid> GetCandidateUsersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Cleanup is best-effort Connect maintenance. Do not enqueue an SSCAN that will sit in
        // the backlog when Redis is known to be disconnected.
        if (!_multiplexer.IsConnected)
        {
            yield break;
        }

        await using IAsyncEnumerator<RedisValue> enumerator = _database.SetScanAsync(
                ConnectRedisKeys.Sessions,
                pageSize: 100)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            RedisValue value;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }
                value = enumerator.Current;
            }
            catch (RedisException exception)
            {
                throw new ConnectInfrastructureUnavailableException(
                    "Connect session discovery is unavailable.",
                    exception);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (Guid.TryParse(value.ToString(), out Guid userId) && userId != Guid.Empty)
            {
                yield return userId;
            }
        }
    }

    public async Task RemoveCandidateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _database.SetRemoveAsync(
                ConnectRedisKeys.Sessions,
                userId.ToString("D"));
        }
        catch (RedisException exception)
        {
            throw new ConnectInfrastructureUnavailableException(
                "Connect session discovery is unavailable.",
                exception);
        }
    }
}
