using Connect.Domain.Presence;
using Shouldly;

namespace Connect.Domain.UnitTests.Presence;

public sealed class PresenceStateTests
{
    private static readonly DateTimeOffset Start =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterConnection_CreatesDeviceAndFirstConnection()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();

        presence.RegisterConnection(deviceId, "Browser", "connection-1", Start)
            .ShouldBeTrue();

        Device device = presence.Devices.ShouldHaveSingleItem();
        device.DeviceId.ShouldBe(deviceId);
        device.IsOnline.ShouldBeTrue();
        device.Connections.ShouldHaveSingleItem().ConnectionId.ShouldBe("connection-1");
        presence.ActiveDeviceId.ShouldBe(deviceId);
        presence.AudioOwnerConnectionId.ShouldBe("connection-1");
        presence.Version.ShouldBe(1);
    }

    [Fact]
    public void RegisterConnection_SecondDevice_DoesNotTakeOwnershipFromActiveDevice()
    {
        PresenceState presence = PresenceWithConnection(out Guid activeDeviceId);
        var secondDeviceId = Guid.NewGuid();

        presence.RegisterConnection(
            secondDeviceId,
            "Second browser",
            "connection-2",
            Start.AddSeconds(1));

        presence.ActiveDeviceId.ShouldBe(activeDeviceId);
        presence.AudioOwnerConnectionId.ShouldBe("connection-1");
    }

    [Fact]
    public void RegisterConnection_AllowsSecondTabOnSameDevice()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);

        presence.RegisterConnection(
            deviceId,
            "Browser",
            "connection-2",
            Start.AddSeconds(1));

        presence.Devices.ShouldHaveSingleItem().Connections.Count.ShouldBe(2);
        presence.Version.ShouldBe(2);
    }

    [Fact]
    public void RegisterConnection_SameRuntimeSession_ReplacesPreviousConnection()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();
        var runtimeSessionId = Guid.NewGuid();
        presence.RegisterConnection(
            deviceId,
            "Browser",
            "connection-old",
            Start,
            runtimeSessionId);

        presence.RegisterConnection(
            deviceId,
            "Browser",
            "connection-new",
            Start.AddSeconds(1),
            runtimeSessionId);

        DeviceConnection connection = presence.Devices.ShouldHaveSingleItem()
            .Connections.ShouldHaveSingleItem();
        connection.ConnectionId.ShouldBe("connection-new");
        connection.RuntimeSessionId.ShouldBe(runtimeSessionId);
        presence.AudioOwnerConnectionId.ShouldBe("connection-new");
    }

    [Fact]
    public void RegisterConnection_DifferentRuntimeSessions_RemainIndependent()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();
        presence.RegisterConnection(
            deviceId,
            "Browser",
            "first-tab",
            Start,
            Guid.NewGuid());

        presence.RegisterConnection(
            deviceId,
            "Browser",
            "second-tab",
            Start.AddSeconds(1),
            Guid.NewGuid());

        presence.Devices.ShouldHaveSingleItem().Connections.Count.ShouldBe(2);
        presence.AudioOwnerConnectionId.ShouldBe("first-tab");
    }

    [Fact]
    public void RegisterConnection_RepeatedRegistrationIsIdempotent()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);
        long version = presence.Version;

        presence.RegisterConnection(deviceId, "Browser", "connection-1", Start)
            .ShouldBeFalse();

        presence.Version.ShouldBe(version);
        presence.Devices.ShouldHaveSingleItem().Connections.ShouldHaveSingleItem();
    }

    [Fact]
    public void RegisterConnection_RepeatedRegistrationWithNewName_RenamesOnce()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);
        long version = presence.Version;

        presence.RegisterConnection(deviceId, "Renamed browser", "connection-1", Start)
            .ShouldBeTrue();

        presence.Devices.ShouldHaveSingleItem().Name.ShouldBe("Renamed browser");
        presence.Devices.ShouldHaveSingleItem().Connections.ShouldHaveSingleItem();
        presence.Version.ShouldBe(version + 1);
    }

    [Fact]
    public void RegisterConnection_RejectsConnectionOwnedByAnotherDeviceWithoutMutation()
    {
        PresenceState presence = PresenceWithConnection(out Guid originalDeviceId);
        var otherDeviceId = Guid.NewGuid();
        long version = presence.Version;

        Should.Throw<InvalidOperationException>(() =>
            presence.RegisterConnection(
                otherDeviceId,
                "Other device",
                "connection-1",
                Start.AddSeconds(1)));

        presence.Version.ShouldBe(version);
        presence.Devices.ShouldHaveSingleItem().DeviceId.ShouldBe(originalDeviceId);
        presence.Devices.ShouldHaveSingleItem().Name.ShouldBe("Browser");
        presence.Devices.ShouldHaveSingleItem().Connections.ShouldHaveSingleItem();
    }

    [Fact]
    public void Restore_RejectsDuplicateConnectionIdsAcrossDevices()
    {
        var first = Device.Restore(
            Guid.NewGuid(),
            "First",
            [new DeviceConnection("connection-1", Start)]);
        var second = Device.Restore(
            Guid.NewGuid(),
            "Second",
            [new DeviceConnection("connection-1", Start.AddSeconds(1))]);

        Should.Throw<ArgumentException>(() =>
            PresenceState.Restore([first, second], null, null, 0));
    }

    [Fact]
    public void Reconnect_AddsNewConnectionWithoutChangingDeviceIdentity()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);

        presence.RegisterConnection(
            deviceId,
            "Browser",
            "connection-new",
            Start.AddMinutes(1));

        Device device = presence.Devices.ShouldHaveSingleItem();
        device.DeviceId.ShouldBe(deviceId);
        device.Connections.Select(connection => connection.ConnectionId)
            .ShouldBe(["connection-1", "connection-new"], ignoreOrder: true);
    }

    [Fact]
    public void DisconnectOneOfSeveralConnections_LeavesDeviceOnline()
    {
        PresenceState presence = PresenceWithTwoConnections(out Guid deviceId);
        presence.SelectActiveDevice(deviceId);

        DisconnectConnectionResult result = presence.DisconnectConnection("connection-2");

        result.ActiveDeviceLost.ShouldBeFalse();
        presence.ActiveDeviceId.ShouldBe(deviceId);
        presence.Devices.ShouldHaveSingleItem().IsOnline.ShouldBeTrue();
    }

    [Fact]
    public void DisconnectAudioOwner_SelectsEarliestRemainingConnection()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();
        presence.RegisterConnection(deviceId, "Browser", "z-owner", Start);
        presence.RegisterConnection(deviceId, "Browser", "b-second", Start.AddSeconds(1));
        presence.RegisterConnection(deviceId, "Browser", "a-third", Start.AddSeconds(1));
        presence.SelectActiveDevice(deviceId);

        DisconnectConnectionResult result = presence.DisconnectConnection("z-owner");

        result.AudioOwnerChanged.ShouldBeTrue();
        presence.AudioOwnerConnectionId.ShouldBe("a-third");
        presence.ActiveDeviceId.ShouldBe(deviceId);
    }

    [Fact]
    public void DisconnectFinalActiveConnection_ClearsActiveAndOwner()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);
        presence.SelectActiveDevice(deviceId);

        DisconnectConnectionResult result = presence.DisconnectConnection("connection-1");

        result.ActiveDeviceLost.ShouldBeTrue();
        presence.ActiveDeviceId.ShouldBeNull();
        presence.AudioOwnerConnectionId.ShouldBeNull();
    }

    [Fact]
    public void SelectActiveDevice_FirstRegisteredDeviceIsAlreadySelected()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);

        SelectActiveDeviceResult result = presence.SelectActiveDevice(deviceId);

        result.Status.ShouldBe(SelectActiveDeviceStatus.NoOp);
        result.ActiveDeviceId.ShouldBe(deviceId);
        result.AudioOwnerConnectionId.ShouldBe("connection-1");
    }

    [Fact]
    public void SelectActiveDevice_PrefersCallingConnectionOnSelectedDevice()
    {
        PresenceState presence = PresenceWithTwoConnections(out Guid deviceId);

        SelectActiveDeviceResult result =
            presence.SelectActiveDevice(deviceId, "connection-2");

        result.Status.ShouldBe(SelectActiveDeviceStatus.Selected);
        result.ActiveDeviceId.ShouldBe(deviceId);
        result.AudioOwnerConnectionId.ShouldBe("connection-2");
        presence.AudioOwnerConnectionId.ShouldBe("connection-2");
    }

    [Fact]
    public void SelectActiveDevice_IgnoresPreferredConnectionOwnedByAnotherDevice()
    {
        PresenceState presence = PresenceWithTwoConnections(out Guid deviceId);

        SelectActiveDeviceResult result =
            presence.SelectActiveDevice(deviceId, "foreign-connection");

        result.AudioOwnerConnectionId.ShouldBe("connection-1");
    }

    [Fact]
    public void SelectActiveDevice_ReturnsOfflineForOwnedOfflineDevice()
    {
        var deviceId = Guid.NewGuid();
        var device = Device.Restore(deviceId, "Offline", []);
        var presence = PresenceState.Restore([device], null, null, 4);

        SelectActiveDeviceResult result = presence.SelectActiveDevice(deviceId);

        result.Status.ShouldBe(SelectActiveDeviceStatus.DeviceOffline);
        presence.Version.ShouldBe(4);
    }

    [Fact]
    public void AudioOwnerElection_UsesConnectionIdAsTieBreaker()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();
        presence.RegisterConnection(deviceId, "Browser", "z-connection", Start);
        presence.RegisterConnection(deviceId, "Browser", "a-connection", Start);

        presence.SelectActiveDevice(deviceId);

        presence.AudioOwnerConnectionId.ShouldBe("a-connection");
    }

    [Fact]
    public void SelectSameActiveDevice_IsNoOp()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);
        presence.SelectActiveDevice(deviceId);
        long version = presence.Version;

        SelectActiveDeviceResult result = presence.SelectActiveDevice(deviceId);

        result.Status.ShouldBe(SelectActiveDeviceStatus.NoOp);
        presence.Version.ShouldBe(version);
    }

    [Fact]
    public void ExpireConnections_RemovesExplicitConnectionAndIncrementsOnce()
    {
        PresenceState presence = PresenceWithTwoConnections(out _);
        long version = presence.Version;

        ExpireConnectionsResult result = presence.ExpireConnections(["connection-1"]);

        result.RemovedCount.ShouldBe(1);
        presence.Version.ShouldBe(version + 1);
        presence.Devices.ShouldHaveSingleItem()
            .Connections.ShouldHaveSingleItem()
            .ConnectionId.ShouldBe("connection-2");
    }

    [Fact]
    public void ExpireConnections_UnknownIdIsNoOp()
    {
        PresenceState presence = PresenceWithConnection(out _);
        long version = presence.Version;

        ExpireConnectionsResult result = presence.ExpireConnections(["unknown"]);

        result.RemovedCount.ShouldBe(0);
        presence.Version.ShouldBe(version);
        presence.Devices.ShouldHaveSingleItem().Connections.ShouldHaveSingleItem();
    }

    [Fact]
    public void ExpireConnections_DuplicateIdsRemoveOnce()
    {
        PresenceState presence = PresenceWithTwoConnections(out _);
        long version = presence.Version;

        ExpireConnectionsResult result =
            presence.ExpireConnections(["connection-1", "connection-1"]);

        result.RemovedCount.ShouldBe(1);
        presence.Version.ShouldBe(version + 1);
    }

    [Fact]
    public void ExpireConnections_MultipleRemovalsIncrementVersionOnce()
    {
        PresenceState presence = PresenceWithTwoConnections(out _);
        long version = presence.Version;

        ExpireConnectionsResult result =
            presence.ExpireConnections(["connection-1", "connection-2"]);

        result.RemovedCount.ShouldBe(2);
        presence.Version.ShouldBe(version + 1);
    }

    [Fact]
    public void ExpireConnections_NonOwnerPreservesAudioOwner()
    {
        PresenceState presence = PresenceWithTwoConnections(out Guid deviceId);
        presence.SelectActiveDevice(deviceId);
        long version = presence.Version;

        ExpireConnectionsResult result = presence.ExpireConnections(["connection-2"]);

        result.AudioOwnerChanged.ShouldBeFalse();
        result.AudioOwnerConnectionId.ShouldBe("connection-1");
        presence.AudioOwnerConnectionId.ShouldBe("connection-1");
        presence.Version.ShouldBe(version + 1);
    }

    [Fact]
    public void ExpireConnections_OwnerSelectsNextDeterministically()
    {
        var presence = new PresenceState();
        var deviceId = Guid.NewGuid();
        presence.RegisterConnection(deviceId, "Browser", "owner", Start);
        presence.RegisterConnection(deviceId, "Browser", "z-next", Start.AddSeconds(1));
        presence.RegisterConnection(deviceId, "Browser", "a-next", Start.AddSeconds(1));
        presence.SelectActiveDevice(deviceId);

        ExpireConnectionsResult result = presence.ExpireConnections(["owner"]);

        result.AudioOwnerChanged.ShouldBeTrue();
        result.AudioOwnerConnectionId.ShouldBe("a-next");
        presence.AudioOwnerConnectionId.ShouldBe("a-next");
    }

    [Fact]
    public void ExpireConnections_FinalActiveConnectionClearsActiveAndOwner()
    {
        PresenceState presence = PresenceWithConnection(out Guid deviceId);
        presence.SelectActiveDevice(deviceId);

        ExpireConnectionsResult result = presence.ExpireConnections(["connection-1"]);

        result.ActiveDeviceLost.ShouldBeTrue();
        result.AudioOwnerChanged.ShouldBeTrue();
        presence.ActiveDeviceId.ShouldBeNull();
        presence.AudioOwnerConnectionId.ShouldBeNull();
    }

    [Fact]
    public void ExpireConnections_InactiveDeviceDoesNotAffectActiveDevice()
    {
        var presence = new PresenceState();
        var activeDeviceId = Guid.NewGuid();
        var inactiveDeviceId = Guid.NewGuid();
        presence.RegisterConnection(activeDeviceId, "Active", "active-connection", Start);
        presence.RegisterConnection(
            inactiveDeviceId,
            "Inactive",
            "inactive-connection",
            Start);
        presence.SelectActiveDevice(activeDeviceId);

        ExpireConnectionsResult result =
            presence.ExpireConnections(["inactive-connection"]);

        result.ActiveDeviceLost.ShouldBeFalse();
        result.AudioOwnerChanged.ShouldBeFalse();
        presence.ActiveDeviceId.ShouldBe(activeDeviceId);
        presence.AudioOwnerConnectionId.ShouldBe("active-connection");
    }

    [Fact]
    public void Heartbeat_IsNotAnAuthoritativePresenceMutation()
    {
        typeof(PresenceState).GetMethod("TouchConnection").ShouldBeNull();
        typeof(Device).GetMethod("TouchConnection").ShouldBeNull();
        typeof(DeviceConnection).GetProperty("LastSeenAt").ShouldBeNull();
    }

    [Fact]
    public void PublicApi_DoesNotExposeMutablePresenceCollectionsOrDeviceTransitions()
    {
        PresenceState presence = PresenceWithConnection(out _);
        Device device = presence.Devices.ShouldHaveSingleItem();

        Should.Throw<NotSupportedException>(() =>
            ((IList<Device>)presence.Devices).Clear());
        Should.Throw<NotSupportedException>(() =>
            ((IList<DeviceConnection>)device.Connections).Clear());

        typeof(Device).GetMethod("Rename").ShouldBeNull();
        typeof(Device).GetMethod("RegisterConnection").ShouldBeNull();
        typeof(Device).GetMethod("RemoveConnection").ShouldBeNull();
    }

    [Fact]
    public void Restore_DefensivelyCopiesDevicesAndConnections()
    {
        var deviceId = Guid.NewGuid();
        var sourceConnection = new DeviceConnection("connection-1", Start);
        var sourceDevice = Device.Restore(deviceId, "Browser", [sourceConnection]);

        var presence = PresenceState.Restore([sourceDevice], null, null, 4);
        Device restoredDevice = presence.Devices.ShouldHaveSingleItem();

        restoredDevice.ShouldNotBeSameAs(sourceDevice);
        DeviceConnection restoredConnection = restoredDevice.Connections.ShouldHaveSingleItem();
        restoredConnection.ShouldNotBeSameAs(sourceConnection);
        restoredConnection.ConnectedAt.ShouldBe(Start);
    }

    private static PresenceState PresenceWithConnection(out Guid deviceId)
    {
        var presence = new PresenceState();
        deviceId = Guid.NewGuid();
        presence.RegisterConnection(deviceId, "Browser", "connection-1", Start);
        return presence;
    }

    private static PresenceState PresenceWithTwoConnections(out Guid deviceId)
    {
        PresenceState presence = PresenceWithConnection(out deviceId);
        presence.RegisterConnection(
            deviceId,
            "Browser",
            "connection-2",
            Start.AddSeconds(1));
        return presence;
    }
}
