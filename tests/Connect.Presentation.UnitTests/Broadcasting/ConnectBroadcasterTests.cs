using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Presentation.Broadcasting;
using Connect.Presentation.Transport;
using Connect.Presentation.UnitTests.Fakes;

namespace Connect.Presentation.UnitTests.Broadcasting;

public sealed class ConnectBroadcasterTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    public async Task CoordinatedPlayerQueue_UsesCombinedEvent()
    {
        var client = new TestConnectHubClient();
        var broadcaster = new ConnectBroadcaster(new TestHubContext(client));

        await broadcaster.BroadcastAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ConnectApplicationResult(
                ConnectCommandStatus.Applied,
                Player: Player(2),
                Queue: Queue(3)),
            CancellationToken.None);

        client.LastEvent.ShouldBeOfType<PlayerQueueStateChangedEvent>();
    }

    [Fact]
    public async Task CoordinatedPlayerPresence_UsesCombinedEvent()
    {
        var client = new TestConnectHubClient();
        var broadcaster = new ConnectBroadcaster(new TestHubContext(client));

        await broadcaster.BroadcastAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ConnectApplicationResult(
                ConnectCommandStatus.Applied,
                Player: Player(2),
                Presence: Presence(3)),
            CancellationToken.None);

        client.LastEvent.ShouldBeOfType<PlayerPresenceStateChangedEvent>();
    }

    [Theory]
    [InlineData(ConnectCommandStatus.Duplicate)]
    [InlineData(ConnectCommandStatus.NoChanges)]
    [InlineData(ConnectCommandStatus.Conflict)]
    public async Task NonAppliedResult_DoesNotSendEvent(ConnectCommandStatus status)
    {
        var client = new TestConnectHubClient();
        var broadcaster = new ConnectBroadcaster(new TestHubContext(client));

        await broadcaster.BroadcastAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ConnectApplicationResult(status, Player: Player(2)),
            CancellationToken.None);

        client.LastEvent.ShouldBeNull();
    }

    private static PlayerStateDto Player(long version) =>
        new(false, 0, Now, 50, version);

    private static QueueStateDto Queue(long version) =>
        new([], null, RepeatModeDto.None, false, version);

    private static PresenceStateDto Presence(long version) =>
        new([], null, null, version);
}
