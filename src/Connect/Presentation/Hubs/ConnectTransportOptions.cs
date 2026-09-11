namespace Connect.Presentation.Hubs;

public sealed class ConnectTransportOptions
{
    public const string SectionName = "Connect:Transport";
    public const long DefaultMaximumReceiveMessageSize = 64 * 1024;

    public long MaximumReceiveMessageSize { get; init; } = DefaultMaximumReceiveMessageSize;
    public IReadOnlySet<string> AllowedOrigins { get; init; } = new HashSet<string>();

    public void Validate()
    {
        if (MaximumReceiveMessageSize is < 1024 or > 64 * 1024)
        {
            throw new InvalidOperationException(
                "Connect maximum receive message size must be between 1 KB and 64 KB.");
        }
    }
}
