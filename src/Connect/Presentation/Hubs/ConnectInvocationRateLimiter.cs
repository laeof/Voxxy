using System.Collections.Concurrent;

namespace Connect.Presentation.Hubs;

public enum ConnectRateLimitBucket
{
    Heartbeat,
    Position,
    Volume,
    General
}

public sealed class ConnectInvocationRateLimiter(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<(string ConnectionId, ConnectRateLimitBucket Bucket), TokenBucket>
        _buckets = new();

    public bool TryAcquire(string connectionId, ConnectRateLimitBucket bucket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        (int capacity, double tokensPerSecond) = bucket switch
        {
            ConnectRateLimitBucket.Heartbeat => (1, 0.2),
            ConnectRateLimitBucket.Position => (10, 4),
            ConnectRateLimitBucket.Volume => (10, 4),
            _ => (20, 5)
        };
        TokenBucket state = _buckets.GetOrAdd(
            (connectionId, bucket),
            _ => new TokenBucket(capacity, timeProvider.GetUtcNow()));
        return state.TryAcquire(capacity, tokensPerSecond, timeProvider.GetUtcNow());
    }

    public void RemoveConnection(string connectionId)
    {
        foreach ((string ConnectionId, ConnectRateLimitBucket Bucket) key in _buckets.Keys)
        {
            if (string.Equals(key.ConnectionId, connectionId, StringComparison.Ordinal))
            {
                _buckets.TryRemove(key, out _);
            }
        }
    }

    public int TrackedBucketCount => _buckets.Count;

    private sealed class TokenBucket(int tokens, DateTimeOffset updatedAt)
    {
        private readonly object _gate = new();
        private double _tokens = tokens;
        private DateTimeOffset _updatedAt = updatedAt;

        public bool TryAcquire(
            int capacity,
            double tokensPerSecond,
            DateTimeOffset now)
        {
            lock (_gate)
            {
                double elapsedSeconds = Math.Max(0, (now - _updatedAt).TotalSeconds);
                _tokens = Math.Min(capacity, _tokens + elapsedSeconds * tokensPerSecond);
                _updatedAt = now;
                if (_tokens < 1)
                {
                    return false;
                }

                _tokens--;
                return true;
            }
        }
    }
}
