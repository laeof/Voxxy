namespace Connect.Contracts.States;

public sealed record PlayerStateDto(
    bool IsPlaying,
    long PositionMs,
    DateTimeOffset PositionUpdatedAt,
    int VolumePercent,
    long Version);
