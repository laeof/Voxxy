using Connect.Application.Abstractions.Repositories;
using Connect.Domain.Player;
using Connect.Infrastructure.Redis;
using StackExchange.Redis;

namespace Connect.Infrastructure.Repositories;

public sealed class PlayerSessionRepository : RedisCacheRepository<PlayerState>, IPlayerSessionRepository
{
    public PlayerSessionRepository(IConnectionMultiplexer multiplexer) : base(multiplexer, TableConstants.PlayerSessionTable)
    {

    }
}