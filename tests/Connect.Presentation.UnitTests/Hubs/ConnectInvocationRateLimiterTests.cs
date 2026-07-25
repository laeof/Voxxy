using Connect.Presentation.Hubs;

namespace Connect.Presentation.UnitTests.Hubs;

public sealed class ConnectInvocationRateLimiterTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HeartbeatWithinLimit_IsAccepted_AndAboveLimitIsRejected()
    {
        var time = new MutableTimeProvider(Now);
        var limiter = new ConnectInvocationRateLimiter(time);

        limiter.TryAcquire("connection", ConnectRateLimitBucket.Heartbeat).ShouldBeTrue();
        limiter.TryAcquire("connection", ConnectRateLimitBucket.Heartbeat).ShouldBeFalse();
    }

    [Fact]
    public void PositionBurst_IsBounded()
    {
        var limiter = new ConnectInvocationRateLimiter(new MutableTimeProvider(Now));

        Enumerable.Range(0, 10)
            .All(_ => limiter.TryAcquire("connection", ConnectRateLimitBucket.Position))
            .ShouldBeTrue();
        limiter.TryAcquire("connection", ConnectRateLimitBucket.Position).ShouldBeFalse();
    }

    [Fact]
    public void RateLimitUsesTimeProvider()
    {
        var time = new MutableTimeProvider(Now);
        var limiter = new ConnectInvocationRateLimiter(time);
        limiter.TryAcquire("connection", ConnectRateLimitBucket.Heartbeat).ShouldBeTrue();
        limiter.TryAcquire("connection", ConnectRateLimitBucket.Heartbeat).ShouldBeFalse();

        time.Advance(TimeSpan.FromSeconds(5));

        limiter.TryAcquire("connection", ConnectRateLimitBucket.Heartbeat).ShouldBeTrue();
    }

    [Fact]
    public void LimiterState_IsRemovedAfterDisconnect()
    {
        var limiter = new ConnectInvocationRateLimiter(new MutableTimeProvider(Now));
        limiter.TryAcquire("connection", ConnectRateLimitBucket.Heartbeat);

        limiter.RemoveConnection("connection");

        limiter.TrackedBucketCount.ShouldBe(0);
        limiter.TryAcquire("connection", ConnectRateLimitBucket.Heartbeat).ShouldBeTrue();
    }

    [Fact]
    public void DifferentConnectionsHaveIndependentLimits()
    {
        var limiter = new ConnectInvocationRateLimiter(new MutableTimeProvider(Now));

        limiter.TryAcquire("a", ConnectRateLimitBucket.Heartbeat).ShouldBeTrue();
        limiter.TryAcquire("a", ConnectRateLimitBucket.Heartbeat).ShouldBeFalse();
        limiter.TryAcquire("b", ConnectRateLimitBucket.Heartbeat).ShouldBeTrue();
    }

    private sealed class MutableTimeProvider(DateTimeOffset value) : TimeProvider
    {
        private DateTimeOffset _value = value;

        public override DateTimeOffset GetUtcNow() => _value;

        public void Advance(TimeSpan duration) => _value += duration;
    }
}
