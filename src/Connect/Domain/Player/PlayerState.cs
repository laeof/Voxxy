using Connect.Domain.Player;

namespace Connect.Domain.Player;

public sealed class PlayerState
{
    public Guid UserId { get; set; }
    public Guid? TrackId { get; set; }
    public Guid? QueueId { get; set; }
    public string? ActiveDeviceId { get; set; } //ConnectionId of the active device
    public bool IsPlaying { get; set; }
    public int PositionMs { get; set; }
    public int VolumePercent { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public PlayerState(Guid userId)
    {
        UserId = userId;
        TrackId = null;
        QueueId = null;
        IsPlaying = false;
        VolumePercent = 50;
        ActiveDeviceId = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        PositionMs = 0;
    }

    public void Play(Guid? trackId, Guid? queueId, int positionMs, DateTimeOffset updatedAt)
    {
        TrackId = trackId;
        QueueId = queueId;
        PositionMs = positionMs;
        IsPlaying = true;
        UpdatedAt = updatedAt;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    public void ConnectToDevice(string connectionId)
    {
        ActiveDeviceId = connectionId;
    }

    public void ChangePosition(int positionMs, DateTimeOffset updatedAt)
    {
        PositionMs = positionMs;
        UpdatedAt = updatedAt;
    }

    public void ChangeVolume(int volumePercent)
    {
        VolumePercent = volumePercent;
    }
}