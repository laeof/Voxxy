using Connect.Application.Commands;
using Connect.Application.DeviceLifecycle;
using Connect.Application.Results;
using Connect.Domain.Synchronization;
using Shouldly;
using Connect.Infrastructure.Redis;

namespace Connect.Infrastructure.IntegrationTests;

[Collection(RedisCollection.Name)]
public sealed class ApplicationDeviceLifecycleTests(RedisFixture fixture)
{
    private static readonly DateTimeOffset Start =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_Heartbeat_Disconnect_CompletesLifecycle()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        RedisConnectStateStore store = fixture.CreateStore();
        var register = new RegisterConnectionHandler(store);
        var refresh = new RefreshConnectionLeaseHandler(store);
        var disconnect = new DisconnectConnectionHandler(
            store,
            new ConnectStateCoordinator());

        ConnectApplicationResult registration = await register.HandleAsync(
            new RegisterConnectionCommand(
                userId,
                Guid.NewGuid(),
                deviceId,
                "Browser",
                "connection-a",
                Start));
        ConnectApplicationResult heartbeat = await refresh.HandleAsync(
            new RefreshConnectionLeaseCommand(userId, "connection-a"));
        ConnectApplicationResult disconnection = await disconnect.HandleAsync(
            new DisconnectConnectionCommand(
                userId,
                Guid.NewGuid(),
                "connection-a",
                Start.AddSeconds(1)));

        registration.Status.ShouldBe(ConnectCommandStatus.Applied);
        heartbeat.Status.ShouldBe(ConnectCommandStatus.Applied);
        disconnection.Status.ShouldBe(ConnectCommandStatus.Applied);
        disconnection.Presence!.Devices.Single().Connections.ShouldBeEmpty();
    }

    [Fact]
    public async Task DuplicateRegistration_ReturnsOriginalOutcome()
    {
        var userId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var handler = new RegisterConnectionHandler(fixture.CreateStore());
        var command = new RegisterConnectionCommand(
            userId,
            commandId,
            deviceId,
            "Browser",
            "connection-a",
            Start);

        ConnectApplicationResult first = await handler.HandleAsync(command);
        ConnectApplicationResult duplicate = await handler.HandleAsync(command);

        first.Status.ShouldBe(ConnectCommandStatus.Applied);
        duplicate.Status.ShouldBe(ConnectCommandStatus.Duplicate);
        duplicate.Outcome.ShouldBe(first.Outcome);
    }
}
