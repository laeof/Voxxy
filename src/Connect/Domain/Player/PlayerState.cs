namespace Connect.Domain.Player;

public sealed class PlayerState
{
    public const int DefaultVolumePercent = 50;

    public bool IsPlaying { get; private set; }
    public long PositionMs { get; private set; }
    public DateTimeOffset PositionUpdatedAt { get; private set; }
    public int VolumePercent { get; private set; }
    public long Version { get; private set; }

    public PlayerState(DateTimeOffset serverTime)
        : this(false, 0, serverTime, DefaultVolumePercent, 0)
    {
    }

    private PlayerState(
        bool isPlaying,
        long positionMs,
        DateTimeOffset positionUpdatedAt,
        int volumePercent,
        long version)
    {
        ValidatePosition(positionMs);
        ValidateVolume(volumePercent);
        ValidateVersion(version);

        IsPlaying = isPlaying;
        PositionMs = positionMs;
        PositionUpdatedAt = positionUpdatedAt;
        VolumePercent = volumePercent;
        Version = version;
    }

    public static PlayerState Restore(
        bool isPlaying,
        long positionMs,
        DateTimeOffset positionUpdatedAt,
        int volumePercent,
        long version) =>
        new(isPlaying, positionMs, positionUpdatedAt, volumePercent, version);

    public bool Play(DateTimeOffset serverTime)
    {
        if (IsPlaying)
        {
            return false;
        }

        IsPlaying = true;
        PositionUpdatedAt = serverTime;
        IncrementVersion();
        return true;
    }

    public bool Pause(DateTimeOffset serverTime)
    {
        if (!IsPlaying)
        {
            return false;
        }

        PositionMs = GetPositionAt(serverTime);
        IsPlaying = false;
        PositionUpdatedAt = serverTime;
        IncrementVersion();
        return true;
    }

    public long GetPositionAt(DateTimeOffset serverTime)
    {
        if (!IsPlaying)
        {
            return PositionMs;
        }

        long elapsedTicks = serverTime.UtcTicks - PositionUpdatedAt.UtcTicks;
        if (elapsedTicks <= 0)
        {
            return PositionMs;
        }

        long elapsedMs = elapsedTicks / TimeSpan.TicksPerMillisecond;
        return checked(PositionMs + elapsedMs);
    }

    public bool Seek(long positionMs, DateTimeOffset serverTime)
    {
        ValidatePosition(positionMs);

        if (!IsPlaying && PositionMs == positionMs)
        {
            return false;
        }

        PositionMs = positionMs;
        PositionUpdatedAt = serverTime;
        IncrementVersion();
        return true;
    }

    public bool ResetPosition(DateTimeOffset serverTime) => Seek(0, serverTime);

    public bool Complete(long positionMs, DateTimeOffset serverTime)
    {
        ValidatePosition(positionMs);

        if (!IsPlaying && PositionMs == positionMs)
        {
            return false;
        }

        IsPlaying = false;
        PositionMs = positionMs;
        PositionUpdatedAt = serverTime;
        IncrementVersion();
        return true;
    }

    public bool ChangeVolume(int volumePercent)
    {
        ValidateVolume(volumePercent);

        if (VolumePercent == volumePercent)
        {
            return false;
        }

        VolumePercent = volumePercent;
        IncrementVersion();
        return true;
    }

    public bool ClearAndPause(DateTimeOffset serverTime)
    {
        if (!IsPlaying && PositionMs == 0)
        {
            return false;
        }

        IsPlaying = false;
        PositionMs = 0;
        PositionUpdatedAt = serverTime;
        IncrementVersion();
        return true;
    }

    private static void ValidatePosition(long positionMs)
    {
        if (positionMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(positionMs),
                positionMs,
                "Position cannot be negative.");
        }
    }

    private static void ValidateVolume(int volumePercent)
    {
        if (volumePercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volumePercent),
                volumePercent,
                "Volume must be between 0 and 100.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                "Version cannot be negative.");
        }
    }

    private void IncrementVersion() => Version++;
}
