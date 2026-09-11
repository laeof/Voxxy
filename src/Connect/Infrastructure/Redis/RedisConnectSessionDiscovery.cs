using System.Runtime.CompilerServices;
using Connect.Application.Abstractions.Persistence;
using StackExchange.Redis;

namespace Connect.Infrastructure.Redis;

public sealed class RedisConnectSessionDiscovery(IConnectionMultiplexer multiplexer)
    : IConnectSessionDiscovery
{
    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async IAsyncEnumerable<Guid> GetCandidateUsersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (RedisValue value in _database.SetScanAsync(
                           ConnectRedisKeys.Sessions,
                           pageSize: 100)
                           .WithCancellation(cancellationToken))
        {
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
        await _database.SetRemoveAsync(
            ConnectRedisKeys.Sessions,
            userId.ToString("D"));
    }
}
