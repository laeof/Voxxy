using Connect.Domain.Player;
using Connect.Domain.Presence;

namespace Connect.Application.UnitTests.Fakes;

internal static class TestStates
{
    public static readonly DateTimeOffset Time =
        new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    public static PresenceState Presence(
        Guid deviceId,
        string connectionId,
        bool active = false)
    {
        var presence = new PresenceState();
        presence.RegisterConnection(deviceId, "Browser", connectionId, Time);
        if (active)
        {
            presence.SelectActiveDevice(deviceId);
        }

        return presence;
    }

    public static PlayerState PlayingPlayer()
    {
        var player = new PlayerState(Time);
        player.Play(Time);
        return player;
    }
}
