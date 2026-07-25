namespace Connect.Presentation.Cleanup;

public sealed class ConnectCleanupOptions
{
    public const string SectionName = "Connect:Cleanup";

    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(15);
    public int BatchSize { get; init; } = 100;
    public int MaxConcurrency { get; init; } = 4;
    public TimeSpan UserLeaseTtl { get; init; } = TimeSpan.FromSeconds(30);

    public void Validate()
    {
        if (Interval < TimeSpan.FromSeconds(1))
        {
            throw new InvalidOperationException("Connect cleanup interval must be at least one second.");
        }
        if (BatchSize <= 0)
        {
            throw new InvalidOperationException("Connect cleanup batch size must be positive.");
        }
        if (MaxConcurrency <= 0)
        {
            throw new InvalidOperationException("Connect cleanup max concurrency must be positive.");
        }
        if (UserLeaseTtl <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Connect cleanup user lease TTL must be positive.");
        }
    }
}
