namespace Connect.Shared;

public sealed class PlayRequest
{
    public Guid? TrackId { get; set; }
    public Guid? QueueId { get; set; }
    public int PositionMs { get; set; }
    public int VolumePercent { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}