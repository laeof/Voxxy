using System.Globalization;
using System.Text.Json;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.PlayerQueue;
using Connect.Application.Results;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;
using Connect.Domain.Synchronization;
using Connect.Infrastructure.Redis;
using Shouldly;
using StackExchange.Redis;

namespace Connect.Infrastructure.IntegrationTests;

[Collection(RedisCollection.Name)]
public sealed class RedisConnectStateStoreTests
{
    private static readonly DateTimeOffset Start =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly RedisFixture _fixture;
    private readonly RedisConnectStateStore _store;

    public RedisConnectStateStoreTests(RedisFixture fixture)
    {
        _fixture = fixture;
        _store = fixture.CreateStore();
    }

    [Fact]
    public async Task MissingStates_AreDeterministicVersionZero_AndLegacyKeysAreIgnored()
    {
        var userId = Guid.NewGuid();
        IDatabase database = _fixture.Multiplexer.GetDatabase();
        await database.StringSetAsync($"player_session:{userId}", """{"version":99}""");

        ConnectSnapshotReadResult result = await _store.ReadSnapshotAsync(userId, Start);

        result.Status.ShouldBe(PersistenceStatus.Success);
        result.Snapshot!.Player.Version.ShouldBe(0);
        result.Snapshot.Player.PositionUpdatedAt.ShouldBe(Start);
        result.Snapshot.Queue.Version.ShouldBe(0);
        result.Snapshot.Queue.Items.ShouldBeEmpty();
        result.Snapshot.Presence.Version.ShouldBe(0);
        result.Snapshot.Presence.Devices.ShouldBeEmpty();
    }

    [Fact]
    public async Task Player_RoundTripsPlayingAnchorAndTimestamp()
    {
        var userId = Guid.NewGuid();
        var player = PlayerState.Restore(true, 12_345, Start, 73, 1);

        PersistenceCommitResult commit = await _store.TryCommitPlayerAsync(
            new PlayerCommit(userId, Command("play", "one"), 0, player));
        PersistenceReadResult<PlayerState> read =
            await _store.ReadPlayerAsync(userId, Start.AddHours(1));

        commit.Status.ShouldBe(PersistenceStatus.Applied);
        read.Status.ShouldBe(PersistenceStatus.Success);
        read.State!.IsPlaying.ShouldBeTrue();
        read.State.PositionMs.ShouldBe(12_345);
        read.State.PositionUpdatedAt.ShouldBe(Start);
        read.State.VolumePercent.ShouldBe(73);
        read.State.Version.ShouldBe(1);
    }

    [Fact]
    public async Task Queue_RoundTripPreservesDuplicatesIdentityCanonicalAndShuffleOrder()
    {
        var userId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var first = new QueueItem(Guid.NewGuid(), trackId, 0);
        var second = new QueueItem(Guid.NewGuid(), trackId, 1);
        var queue = QueueState.Restore(
            [second, first],
            second.QueueItemId,
            RepeatMode.Track,
            true,
            1);

        await _store.TryCommitQueueAsync(
            new QueueCommit(userId, Command("queue", "roundtrip"), 0, queue));
        PersistenceReadResult<QueueState> read = await _store.ReadQueueAsync(userId);

        read.Status.ShouldBe(PersistenceStatus.Success);
        read.State!.Items.Select(item => item.TrackId)
            .ShouldAllBe(value => value == trackId);
        read.State.Items.Select(item => item.QueueItemId)
            .ShouldBe([second.QueueItemId, first.QueueItemId]);
        read.State.Items.Single(item => item.QueueItemId == first.QueueItemId)
            .CanonicalOrder.ShouldBe(first.CanonicalOrder);
        read.State.CurrentQueueItemId.ShouldBe(second.QueueItemId);
        read.State.IsShuffled.ShouldBeTrue();
        read.State.RepeatMode.ShouldBe(RepeatMode.Track);
    }

    [Fact]
    public async Task Queue_LegacyJsonWithoutPlaybackSource_DeserializesAsManualContext()
    {
        var userId = Guid.NewGuid();
        IDatabase database = _fixture.Multiplexer.GetDatabase();
        await database.HashSetAsync(
            ConnectRedisKeys.Queue(userId),
            [
                new HashEntry(
                    "json",
                    """
                    {"items":[],"currentQueueItemId":null,"repeatMode":"none","isShuffled":false,"version":3}
                    """),
                new HashEntry("version", "3")
            ]);

        PersistenceReadResult<QueueState> read = await _store.ReadQueueAsync(userId);

        read.Status.ShouldBe(PersistenceStatus.Success);
        read.State!.Version.ShouldBe(3);
        read.State.SourceId.ShouldBeNull();
        read.State.SourceType.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StartPlaybackContext_ReplacesExistingRedisContext(bool playing)
    {
        var userId = Guid.NewGuid();
        var coordinator = new ConnectStateCoordinator();
        var handler = new StartPlaybackContextHandler(_store, coordinator);
        DateTimeOffset commandTime = Start.AddSeconds(10);
        var firstSourceId = Guid.NewGuid();
        var secondSourceId = Guid.NewGuid();
        PlaybackContextItem[] firstItems =
        [
            new(Guid.NewGuid(), Guid.NewGuid()),
            new(Guid.NewGuid(), Guid.NewGuid())
        ];
        PlaybackContextItem[] secondItems =
        [
            new(Guid.NewGuid(), Guid.NewGuid()),
            new(Guid.NewGuid(), Guid.NewGuid())
        ];

        ConnectApplicationResult first = await handler.HandleAsync(
            new StartPlaybackContextCommand(
                userId,
                Guid.NewGuid(),
                firstSourceId,
                PlaybackSourceType.Album,
                firstItems,
                0,
                Start));
        first.Status.ShouldBe(ConnectCommandStatus.Applied);
        if (!playing)
        {
            PlayerState player = (await _store.ReadPlayerAsync(userId, commandTime)).State!;
            player.Pause(commandTime);
            PersistenceCommitResult pauseCommit = await _store.TryCommitPlayerAsync(
                new PlayerCommit(
                    userId,
                    Command("pause", "replacement-test"),
                    first.Player!.Version,
                    player));
            pauseCommit.Status.ShouldBe(PersistenceStatus.Applied);
        }

        ConnectApplicationResult replacement = await handler.HandleAsync(
            new StartPlaybackContextCommand(
                userId,
                Guid.NewGuid(),
                secondSourceId,
                PlaybackSourceType.Album,
                secondItems,
                1,
                commandTime));
        ConnectSnapshotReadResult snapshot =
            await _store.ReadSnapshotAsync(userId, commandTime);

        replacement.Status.ShouldBe(ConnectCommandStatus.Applied);
        snapshot.Status.ShouldBe(PersistenceStatus.Success);
        snapshot.Snapshot!.Queue.SourceId.ShouldBe(secondSourceId);
        snapshot.Snapshot.Queue.SourceType.ShouldBe(PlaybackSourceType.Album);
        snapshot.Snapshot.Queue.CurrentQueueItemId.ShouldBe(secondItems[1].QueueItemId);
        snapshot.Snapshot.Queue.Items.ShouldNotContain(
            item => firstItems.Any(firstItem => firstItem.QueueItemId == item.QueueItemId));
        snapshot.Snapshot.Player.IsPlaying.ShouldBeTrue();
    }

    [Fact]
    public async Task Presence_RoundTripPreservesDevicesConnectionsActiveAndOwner()
    {
        var userId = Guid.NewGuid();
        var firstDevice = Guid.NewGuid();
        var secondDevice = Guid.NewGuid();
        var presence = PresenceState.Restore(
            [
                Device.Restore(
                    firstDevice,
                    "First",
                    [
                        new DeviceConnection("connection-1", Start),
                        new DeviceConnection("connection-2", Start.AddSeconds(1))
                    ]),
                Device.Restore(
                    secondDevice,
                    "Second",
                    [new DeviceConnection("connection-3", Start)])
            ],
            firstDevice,
            "connection-1",
            1);

        await _store.TryCommitPresenceAsync(
            new PresenceCommit(userId, Command("presence", "roundtrip"), 0, presence));
        PersistenceReadResult<PresenceState> read =
            await _store.ReadPresenceAsync(userId);

        read.Status.ShouldBe(PersistenceStatus.Success);
        read.State!.Devices.Count.ShouldBe(2);
        read.State.Devices.Single(device => device.DeviceId == firstDevice)
            .Connections.Count.ShouldBe(2);
        read.State.ActiveDeviceId.ShouldBe(firstDevice);
        read.State.AudioOwnerConnectionId.ShouldBe("connection-1");
    }

    [Fact]
    public async Task MalformedJson_ReturnsCorruptStateWithoutFallback()
    {
        var userId = Guid.NewGuid();
        await _fixture.Multiplexer.GetDatabase().HashSetAsync(
            ConnectRedisKeys.Player(userId),
            [
                new HashEntry("version", "0"),
                new HashEntry("json", "{not-json")
            ]);

        PersistenceReadResult<PlayerState> read =
            await _store.ReadPlayerAsync(userId, Start);
        ConnectSnapshotReadResult snapshot =
            await _store.ReadSnapshotAsync(userId, Start);

        read.Status.ShouldBe(PersistenceStatus.CorruptState);
        read.State.ShouldBeNull();
        snapshot.Status.ShouldBe(PersistenceStatus.CorruptState);
        snapshot.Snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task ConcurrentPlayerCas_HasExactlyOneWinner()
    {
        var userId = Guid.NewGuid();
        var first = new PlayerState(Start);
        var second = new PlayerState(Start);
        first.ChangeVolume(10);
        second.ChangeVolume(90);

        PersistenceCommitResult[] results = await Task.WhenAll(
            _store.TryCommitPlayerAsync(
                new PlayerCommit(userId, Command("volume", "10"), 0, first)),
            _store.TryCommitPlayerAsync(
                new PlayerCommit(userId, Command("volume", "90"), 0, second)));

        results.Count(result => result.Status == PersistenceStatus.Applied).ShouldBe(1);
        results.Count(result => result.Status == PersistenceStatus.VersionConflict).ShouldBe(1);
        (await _store.ReadPlayerAsync(userId, Start)).State!.Version.ShouldBe(1);
    }

    [Fact]
    public async Task PlayerCas_PreservesLongVersionPrecisionBeyondLuaDoubleRange()
    {
        var userId = Guid.NewGuid();
        const long storedVersion = 9_007_199_254_740_993;
        string json = JsonSerializer.Serialize(new
        {
            isPlaying = false,
            positionMs = 0,
            positionUpdatedAt = Start,
            volumePercent = 50,
            version = storedVersion
        });
        await _fixture.Multiplexer.GetDatabase().HashSetAsync(
            ConnectRedisKeys.Player(userId),
            [
                new HashEntry(
                    "version",
                    storedVersion.ToString(CultureInfo.InvariantCulture)),
                new HashEntry("json", json)
            ]);
        var player = PlayerState.Restore(
            false,
            0,
            Start,
            51,
            storedVersion + 1);

        PersistenceCommitResult result = await _store.TryCommitPlayerAsync(
            new PlayerCommit(
                userId,
                Command("volume", "large-version"),
                storedVersion,
                player));

        result.Status.ShouldBe(PersistenceStatus.Applied);
        result.Versions.Player.ShouldBe(storedVersion + 1);
        (await _store.ReadPlayerAsync(userId, Start)).State!.Version
            .ShouldBe(storedVersion + 1);
    }

    [Fact]
    public async Task ConcurrentQueueAndPresenceCas_EachHaveOneWinner()
    {
        var queueUser = Guid.NewGuid();
        QueueState firstQueue = QueueWithOneItem();
        QueueState secondQueue = QueueWithOneItem();
        PersistenceCommitResult[] queueResults = await Task.WhenAll(
            _store.TryCommitQueueAsync(
                new QueueCommit(queueUser, Command("add", "first"), 0, firstQueue)),
            _store.TryCommitQueueAsync(
                new QueueCommit(queueUser, Command("add", "second"), 0, secondQueue)));

        var presenceUser = Guid.NewGuid();
        PresenceState firstPresence = PresenceWithConnection("first");
        PresenceState secondPresence = PresenceWithConnection("second");
        PersistenceCommitResult[] presenceResults = await Task.WhenAll(
            _store.TryCommitPresenceAsync(
                new PresenceCommit(
                    presenceUser,
                    Command("register", "first"),
                    0,
                    firstPresence)),
            _store.TryCommitPresenceAsync(
                new PresenceCommit(
                    presenceUser,
                    Command("register", "second"),
                    0,
                    secondPresence)));

        queueResults.Count(result => result.Status == PersistenceStatus.Applied).ShouldBe(1);
        queueResults.Count(result => result.Status == PersistenceStatus.VersionConflict).ShouldBe(1);
        presenceResults.Count(result => result.Status == PersistenceStatus.Applied).ShouldBe(1);
        presenceResults.Count(result => result.Status == PersistenceStatus.VersionConflict)
            .ShouldBe(1);
    }

    [Fact]
    public async Task PlayerQueueCommit_IsAllOrNothingOnConflict()
    {
        var userId = Guid.NewGuid();
        var initialPlayer = new PlayerState(Start);
        initialPlayer.ChangeVolume(40);
        await _store.TryCommitPlayerAsync(
            new PlayerCommit(userId, Command("seed-player", "40"), 0, initialPlayer));

        var player = PlayerState.Restore(false, 0, Start, 41, 1);
        QueueState queue = QueueWithOneItem();
        PersistenceCommitResult result = await _store.TryCommitPlayerAndQueueAsync(
            new PlayerQueueCommit(
                userId,
                Command("select", "conflict"),
                0,
                player,
                0,
                queue));

        result.Status.ShouldBe(PersistenceStatus.VersionConflict);
        (await _store.ReadPlayerAsync(userId, Start)).State!.Version.ShouldBe(1);
        (await _store.ReadQueueAsync(userId)).State!.Version.ShouldBe(0);
    }

    [Fact]
    public async Task PlayerPresenceCommit_WritesBothStatesAtomically()
    {
        var userId = Guid.NewGuid();
        var player = new PlayerState(Start);
        player.Play(Start);
        PresenceState presence = PresenceWithConnection("connection-1");

        PersistenceCommitResult result = await _store.TryCommitPlayerAndPresenceAsync(
            new PlayerPresenceCommit(
                userId,
                Command("activate", "both"),
                0,
                player,
                0,
                presence));
        ConnectSnapshotReadResult snapshot = await _store.ReadSnapshotAsync(userId, Start);

        result.Status.ShouldBe(PersistenceStatus.Applied);
        snapshot.Snapshot!.Player.Version.ShouldBe(1);
        snapshot.Snapshot.Presence.Version.ShouldBe(1);
    }

    [Fact]
    public async Task PlayerPresenceCommit_IsAllOrNothingOnPresenceConflict()
    {
        var userId = Guid.NewGuid();
        PresenceState storedPresence = PresenceWithConnection("stored");
        await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("seed-presence", "stored"),
                0,
                storedPresence));
        var player = new PlayerState(Start);
        player.Play(Start);
        PresenceState stalePresence = PresenceWithConnection("stale");

        PersistenceCommitResult result = await _store.TryCommitPlayerAndPresenceAsync(
            new PlayerPresenceCommit(
                userId,
                Command("activate", "stale"),
                0,
                player,
                0,
                stalePresence));

        result.Status.ShouldBe(PersistenceStatus.VersionConflict);
        (await _store.ReadPlayerAsync(userId, Start)).State!.Version.ShouldBe(0);
        (await _store.ReadPresenceAsync(userId)).State!.Devices[0]
            .Connections[0].ConnectionId.ShouldBe("stored");
    }

    [Fact]
    public async Task DuplicateCommandReturnsOriginalOutcome_AndCollisionIsRejected()
    {
        var userId = Guid.NewGuid();
        QueueState queue = QueueWithOneItem();
        string queueItemId = queue.Items[0].QueueItemId.ToString("D");
        var command = new PersistenceCommand(
            Guid.NewGuid(),
            "AddQueueItem",
            "track=one",
            queueItemId);
        var commit = new QueueCommit(userId, command, 0, queue);

        PersistenceCommitResult applied = await _store.TryCommitQueueAsync(commit);
        PersistenceCommitResult duplicate = await _store.TryCommitQueueAsync(commit);
        PersistenceCommitResult collision = await _store.TryCommitQueueAsync(
            commit with
            {
                Command = command with { Fingerprint = "track=other" }
            });

        applied.Status.ShouldBe(PersistenceStatus.Applied);
        duplicate.Status.ShouldBe(PersistenceStatus.Duplicate);
        duplicate.Outcome.ShouldBe(queueItemId);
        collision.Status.ShouldBe(PersistenceStatus.CommandCollision);
        (await _store.ReadQueueAsync(userId)).State!.Version.ShouldBe(1);

        TimeSpan? ttl = await _fixture.Multiplexer.GetDatabase()
            .KeyTimeToLiveAsync(ConnectRedisKeys.Command(userId, command.CommandId));
        ttl.ShouldNotBeNull();
        ttl!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task SnapshotNeverObservesMixedPlayerQueueCommit()
    {
        var userId = Guid.NewGuid();
        for (int version = 0; version < 15; version++)
        {
            var player = PlayerState.Restore(false, 0, Start, 50 + (version + 1) % 50, version + 1);
            var queue = QueueState.Restore(
                [new QueueItem(Guid.NewGuid(), Guid.NewGuid(), 0)],
                null,
                RepeatMode.None,
                false,
                version + 1);
            Task<PersistenceCommitResult> write = _store.TryCommitPlayerAndQueueAsync(
                new PlayerQueueCommit(
                    userId,
                    Command("pair", version.ToString(CultureInfo.InvariantCulture)),
                    version,
                    player,
                    version,
                    queue));
            ConnectSnapshotReadResult snapshot =
                await _store.ReadSnapshotAsync(userId, Start);
            await write;

            snapshot.Status.ShouldBe(PersistenceStatus.Success);
            snapshot.Snapshot!.Player.Version.ShouldBe(snapshot.Snapshot.Queue.Version);
        }
    }

    [Fact]
    public async Task RegistrationCreatesLease_AndHeartbeatRefreshesWithoutPresenceMutation()
    {
        var userId = Guid.NewGuid();
        PresenceState presence = PresenceWithConnection("connection-1");
        await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("register", "connection-1"),
                0,
                presence,
                "connection-1"));
        RedisKey leaseKey = ConnectRedisKeys.Lease(userId, "connection-1");
        IDatabase database = _fixture.Multiplexer.GetDatabase();

        (await database.KeyExistsAsync(leaseKey)).ShouldBeTrue();
        await Task.Delay(TimeSpan.FromSeconds(1));
        TimeSpan? beforeRefresh = await database.KeyTimeToLiveAsync(leaseKey);
        PersistenceStatus refresh =
            await _store.RefreshConnectionLeaseAsync(userId, "connection-1");
        TimeSpan? afterRefresh = await database.KeyTimeToLiveAsync(leaseKey);

        refresh.ShouldBe(PersistenceStatus.Applied);
        beforeRefresh.ShouldNotBeNull();
        afterRefresh.ShouldNotBeNull();
        afterRefresh.Value.ShouldBeGreaterThan(beforeRefresh.Value);
        (await _store.ReadPresenceAsync(userId)).State!.Version.ShouldBe(1);
    }

    [Fact]
    public async Task RefreshLease_AfterConcurrentPresenceMutation_Succeeds()
    {
        var userId = Guid.NewGuid();
        PresenceState presence = PresenceWithConnection("connection-a");
        Guid deviceId = presence.Devices[0].DeviceId;
        await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("register", "connection-a"),
                0,
                presence,
                "connection-a"));

        presence.RegisterConnection(
            deviceId,
            "Browser",
            "connection-b",
            Start.AddSeconds(1));
        await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("register", "connection-b"),
                1,
                presence,
                "connection-b"));

        IDatabase database = _fixture.Multiplexer.GetDatabase();
        RedisKey presenceKey = ConnectRedisKeys.Presence(userId);
        RedisKey leaseKey = ConnectRedisKeys.Lease(userId, "connection-a");
        RedisValue jsonBeforeHeartbeat =
            await database.HashGetAsync(presenceKey, "json");
        await Task.Delay(TimeSpan.FromSeconds(1));
        TimeSpan? ttlBeforeHeartbeat = await database.KeyTimeToLiveAsync(leaseKey);

        PersistenceStatus result =
            await _store.RefreshConnectionLeaseAsync(userId, "connection-a");

        RedisValue jsonAfterHeartbeat =
            await database.HashGetAsync(presenceKey, "json");
        TimeSpan? ttlAfterHeartbeat = await database.KeyTimeToLiveAsync(leaseKey);
        PersistenceReadResult<PresenceState> read =
            await _store.ReadPresenceAsync(userId);

        result.ShouldBe(PersistenceStatus.Applied);
        read.State!.Version.ShouldBe(2);
        jsonAfterHeartbeat.ShouldBe(jsonBeforeHeartbeat);
        ttlBeforeHeartbeat.ShouldNotBeNull();
        ttlAfterHeartbeat.ShouldNotBeNull();
        ttlAfterHeartbeat.Value.ShouldBeGreaterThan(ttlBeforeHeartbeat.Value);
    }

    [Fact]
    public async Task RefreshLease_ReturnsConnectionNotFound_WhenConnectionRemoved()
    {
        var userId = Guid.NewGuid();
        PresenceState presence = PresenceWithConnection("connection-1");
        await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("register", "connection-1"),
                0,
                presence,
                "connection-1"));

        presence.DisconnectConnection("connection-1");
        await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("disconnect", "connection-1"),
                1,
                presence));

        PersistenceStatus result =
            await _store.RefreshConnectionLeaseAsync(userId, "connection-1");

        result.ShouldBe(PersistenceStatus.ConnectionNotFound);
        (await _store.ReadPresenceAsync(userId)).State!.Version.ShouldBe(2);
    }

    [Fact]
    public async Task MissingLeaseIsReportedExpired_AndUnknownHeartbeatDoesNotCreateLease()
    {
        var userId = Guid.NewGuid();
        PresenceState presence = PresenceWithConnection("connection-1");
        await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("register", "connection-1"),
                0,
                presence));

        ExpiredConnectionsReadResult expired =
            await _store.ReadExpiredConnectionsAsync(userId, presence);
        PersistenceStatus refresh =
            await _store.RefreshConnectionLeaseAsync(userId, "unknown");

        expired.Status.ShouldBe(PersistenceStatus.Success);
        expired.ConnectionIds.ShouldBe(["connection-1"]);
        refresh.ShouldBe(PersistenceStatus.ConnectionNotFound);
        (await _fixture.Multiplexer.GetDatabase().KeyExistsAsync(
            ConnectRedisKeys.Lease(userId, "unknown"))).ShouldBeFalse();
    }

    [Fact]
    public async Task ActiveConnectionExpiry_CommitsPresenceAndPlayerAtomically()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var presence = PresenceState.Restore(
            [
                Device.Restore(
                    deviceId,
                    "Browser",
                    [new DeviceConnection("connection-1", Start)])
            ],
            deviceId,
            "connection-1",
            1);
        var player = new PlayerState(Start);
        player.Play(Start);

        await _store.TryCommitPlayerAndPresenceAsync(
            new PlayerPresenceCommit(
                userId,
                Command("seed", "active"),
                0,
                player,
                0,
                presence));

        long expectedPlayerVersion = player.Version;
        long expectedPresenceVersion = presence.Version;
        var coordinator = new ConnectStateCoordinator();
        coordinator.ExpireConnections(
            presence,
            player,
            ["connection-1"],
            Start.AddSeconds(2));
        PersistenceCommitResult commit = await _store.TryCommitPlayerAndPresenceAsync(
            new PlayerPresenceCommit(
                userId,
                Command("expire", "connection-1"),
                expectedPlayerVersion,
                player,
                expectedPresenceVersion,
                presence));

        ConnectSnapshotReadResult snapshot = await _store.ReadSnapshotAsync(userId, Start);
        commit.Status.ShouldBe(PersistenceStatus.Applied);
        snapshot.Snapshot!.Presence.ActiveDeviceId.ShouldBeNull();
        snapshot.Snapshot.Player.IsPlaying.ShouldBeFalse();
        snapshot.Snapshot.Player.PositionMs.ShouldBe(2_000);
    }

    [Fact]
    public async Task InactiveConnectionExpiry_DoesNotRewritePlayer()
    {
        var userId = Guid.NewGuid();
        var activeDeviceId = Guid.NewGuid();
        var inactiveDeviceId = Guid.NewGuid();
        var presence = PresenceState.Restore(
            [
                Device.Restore(
                    activeDeviceId,
                    "Active",
                    [new DeviceConnection("active", Start)]),
                Device.Restore(
                    inactiveDeviceId,
                    "Inactive",
                    [new DeviceConnection("inactive", Start)])
            ],
            activeDeviceId,
            "active",
            1);
        var player = PlayerState.Restore(true, 0, Start, 50, 1);
        await _store.TryCommitPlayerAndPresenceAsync(
            new PlayerPresenceCommit(
                userId,
                Command("seed", "two-devices"),
                0,
                player,
                0,
                presence));
        var coordinator = new ConnectStateCoordinator();
        coordinator.ExpireConnections(
            presence,
            player,
            ["inactive"],
            Start.AddSeconds(2));

        PersistenceCommitResult commit = await _store.TryCommitPresenceAsync(
            new PresenceCommit(
                userId,
                Command("expire", "inactive"),
                1,
                presence));
        ConnectSnapshotReadResult snapshot = await _store.ReadSnapshotAsync(userId, Start);

        commit.Status.ShouldBe(PersistenceStatus.Applied);
        snapshot.Snapshot!.Presence.Version.ShouldBe(2);
        snapshot.Snapshot.Player.Version.ShouldBe(1);
        snapshot.Snapshot.Player.IsPlaying.ShouldBeTrue();
        snapshot.Snapshot.Presence.AudioOwnerConnectionId.ShouldBe("active");
    }

    [Fact]
    public async Task StateMutationRefreshesSessionStateTtl()
    {
        var userId = Guid.NewGuid();
        var player = new PlayerState(Start);
        player.ChangeVolume(10);
        await _store.TryCommitPlayerAsync(
            new PlayerCommit(userId, Command("volume", "10"), 0, player));

        TimeSpan? ttl = await _fixture.Multiplexer.GetDatabase()
            .KeyTimeToLiveAsync(ConnectRedisKeys.Player(userId));

        ttl.ShouldNotBeNull();
        ttl!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        ttl.Value.ShouldBeLessThanOrEqualTo(_fixture.Options.StateTtl);
    }

    private static PersistenceCommand Command(string type, string fingerprint) =>
        new(Guid.NewGuid(), type, fingerprint, """{"status":"applied"}""");

    private static QueueState QueueWithOneItem()
    {
        var queue = new QueueState();
        queue.Add(Guid.NewGuid());
        return queue;
    }

    private static PresenceState PresenceWithConnection(string connectionId)
    {
        var presence = new PresenceState();
        presence.RegisterConnection(Guid.NewGuid(), "Browser", connectionId, Start);
        return presence;
    }
}
