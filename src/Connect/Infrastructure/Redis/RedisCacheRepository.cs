using System.Text.Json;
using Connect.Application.Abstractions;
using Connect.Application.Abstractions.Repositories;
using StackExchange.Redis;

namespace Connect.Infrastructure.Redis;

public class RedisCacheRepository<T> : ICacheRepository<T>
{
    private static readonly TimeSpan Expiry = TimeSpan.FromDays(7);
    private readonly string _keyPrefix;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheRepository(IConnectionMultiplexer multiplexer, string keyPrefix)
    {
        _database = multiplexer.GetDatabase();
        _keyPrefix = keyPrefix;
    }

    public async Task<T> GetValueAsync(Guid id)
    {
        RedisValue value = await _database.StringGetAsync(GetKey(id));

        if (value.IsNullOrEmpty)
        {
            return default;
        }

        string json = value.ToString();

        return JsonSerializer.Deserialize<T>(
            json,
            _serializerOptions);
    }

    public async Task SetValueAsync(Guid id, T value)
    {
        string json = JsonSerializer.Serialize(
            value,
            _serializerOptions);

        await _database.StringSetAsync(
            GetKey(id),
            json,
            Expiry);
    }

    public async Task RemoveValueAsync(Guid id)
    {
        await _database.KeyDeleteAsync(GetKey(id));
    }

    private RedisKey GetKey(Guid userId)
    {
        return new RedisKey($"{_keyPrefix}:{userId}");
    }
}