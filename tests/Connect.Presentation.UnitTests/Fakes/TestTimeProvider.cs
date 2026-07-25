namespace Connect.Presentation.UnitTests.Fakes;

internal sealed class TestTimeProvider(DateTimeOffset value) : TimeProvider
{
    public int GetUtcNowCalls { get; private set; }

    public override DateTimeOffset GetUtcNow()
    {
        GetUtcNowCalls++;
        return value;
    }
}
