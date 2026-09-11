using Connect.Domain.Player;
using Shouldly;

namespace Connect.Domain.UnitTests.Player;

public sealed class PlayerStateTests
{
    private static readonly DateTimeOffset InitialTime =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewPlayer_HasDeterministicInitialState()
    {
        var player = new PlayerState(InitialTime);

        player.IsPlaying.ShouldBeFalse();
        player.PositionMs.ShouldBe(0);
        player.PositionUpdatedAt.ShouldBe(InitialTime);
        player.VolumePercent.ShouldBe(PlayerState.DefaultVolumePercent);
        player.Version.ShouldBe(0);
    }

    [Fact]
    public void Play_MutatesOnce_AndRepeatedPlayIsNoOp()
    {
        var player = new PlayerState(InitialTime);
        DateTimeOffset playTime = InitialTime.AddSeconds(1);

        player.Play(playTime).ShouldBeTrue();
        player.Play(playTime.AddSeconds(1)).ShouldBeFalse();

        player.IsPlaying.ShouldBeTrue();
        player.PositionUpdatedAt.ShouldBe(playTime);
        player.Version.ShouldBe(1);
    }

    [Fact]
    public void Pause_MutatesOnce_AndRepeatedPauseIsNoOp()
    {
        var player = new PlayerState(InitialTime);
        player.Play(InitialTime.AddSeconds(1));
        DateTimeOffset pauseTime = InitialTime.AddSeconds(2);

        player.Pause(pauseTime).ShouldBeTrue();
        player.Pause(pauseTime.AddSeconds(1)).ShouldBeFalse();

        player.IsPlaying.ShouldBeFalse();
        player.PositionMs.ShouldBe(1_000);
        player.PositionUpdatedAt.ShouldBe(pauseTime);
        player.Version.ShouldBe(2);
    }

    [Fact]
    public void Pause_AnchorsDerivedPositionAtSuppliedServerTime()
    {
        var player = PlayerState.Restore(true, 2_000, InitialTime, 50, 7);
        DateTimeOffset pauseTime = InitialTime.AddMilliseconds(3_250);

        player.Pause(pauseTime).ShouldBeTrue();

        player.PositionMs.ShouldBe(5_250);
        player.PositionUpdatedAt.ShouldBe(pauseTime);
        player.GetPositionAt(pauseTime.AddMinutes(1)).ShouldBe(5_250);
        player.Version.ShouldBe(8);
    }

    [Fact]
    public void GetPositionAt_ClockAnomalyDoesNotDecreasePosition()
    {
        var player = PlayerState.Restore(true, 2_000, InitialTime, 50, 7);

        player.GetPositionAt(InitialTime.AddSeconds(-1)).ShouldBe(2_000);
    }

    [Fact]
    public void GetPositionAt_ThrowsOnPositionOverflow()
    {
        var player = PlayerState.Restore(true, long.MaxValue, InitialTime, 50, 7);

        Should.Throw<OverflowException>(() =>
            player.GetPositionAt(InitialTime.AddMilliseconds(1)));
    }

    [Fact]
    public void Seek_UsesServerTime_AndIncrementsVersion()
    {
        var player = new PlayerState(InitialTime);
        DateTimeOffset seekTime = InitialTime.AddSeconds(3);

        player.Seek(2_500, seekTime).ShouldBeTrue();

        player.PositionMs.ShouldBe(2_500);
        player.PositionUpdatedAt.ShouldBe(seekTime);
        player.Version.ShouldBe(1);
    }

    [Fact]
    public void Seek_ToSamePosition_IsNoOp()
    {
        var player = PlayerState.Restore(false, 2_500, InitialTime, 50, 7);

        player.Seek(2_500, InitialTime.AddSeconds(1)).ShouldBeFalse();

        player.Version.ShouldBe(7);
        player.PositionUpdatedAt.ShouldBe(InitialTime);
    }

    [Fact]
    public void Seek_ToStoredAnchorWhilePlaying_IsMutation()
    {
        var player = PlayerState.Restore(true, 0, InitialTime, 50, 7);
        DateTimeOffset seekTime = InitialTime.AddSeconds(4);

        player.Seek(0, seekTime).ShouldBeTrue();

        player.PositionMs.ShouldBe(0);
        player.PositionUpdatedAt.ShouldBe(seekTime);
        player.Version.ShouldBe(8);
    }

    [Fact]
    public void ResetPosition_FromZeroAnchorWhilePlaying_IsMutation()
    {
        var player = PlayerState.Restore(true, 0, InitialTime, 50, 7);
        DateTimeOffset resetTime = InitialTime.AddSeconds(4);

        player.ResetPosition(resetTime).ShouldBeTrue();

        player.PositionMs.ShouldBe(0);
        player.PositionUpdatedAt.ShouldBe(resetTime);
        player.Version.ShouldBe(8);
    }

    [Fact]
    public void Seek_RejectsNegativePosition()
    {
        var player = new PlayerState(InitialTime);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            player.Seek(-1, InitialTime.AddSeconds(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void ChangeVolume_AcceptsBoundaries(int volume)
    {
        var player = new PlayerState(InitialTime);

        player.ChangeVolume(volume).ShouldBeTrue();

        player.VolumePercent.ShouldBe(volume);
        player.Version.ShouldBe(1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ChangeVolume_RejectsOutOfRangeValue(int volume)
    {
        var player = new PlayerState(InitialTime);

        Should.Throw<ArgumentOutOfRangeException>(() => player.ChangeVolume(volume));
    }

    [Fact]
    public void ChangeVolume_ToSameValue_IsNoOp()
    {
        var player = new PlayerState(InitialTime);

        player.ChangeVolume(PlayerState.DefaultVolumePercent).ShouldBeFalse();
        player.Version.ShouldBe(0);
    }

    [Fact]
    public void Restore_PreservesExistingVersion()
    {
        var player = PlayerState.Restore(true, 10, InitialTime, 75, 42);

        player.Version.ShouldBe(42);
        player.IsPlaying.ShouldBeTrue();
        player.PositionMs.ShouldBe(10);
        player.VolumePercent.ShouldBe(75);
    }
}
