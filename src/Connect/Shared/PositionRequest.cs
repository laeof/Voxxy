namespace Connect.Shared;

public sealed class PositionRequest
{
    public int PositionMs { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}