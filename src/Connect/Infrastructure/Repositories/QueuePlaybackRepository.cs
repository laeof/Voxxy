using Connect.Application.Abstractions.Repositories;
using Connect.Domain.Queue;
using Connect.Infrastructure.Redis;
using StackExchange.Redis;

namespace Connect.Infrastructure.Repositories;

public sealed class QueuePlaybackRepository : RedisCacheRepository<QueuePlayback>, IQueuePlaybackRepository
{
    public QueuePlaybackRepository(IConnectionMultiplexer multiplexer) : base(multiplexer, TableConstants.QueuePlaybackTable)
    {

    }
}