using Connect.Application.Commands;
using Connect.Application.DeviceLifecycle;
using Connect.Application.Results;
using Connect.Domain.Synchronization;
using Connect.Infrastructure.Redis;
using Shouldly;

namespace Connect.Infrastructure.IntegrationTests;

[Collection(RedisCollection.Name)]
public sealed class ConnectCleanupInfrastructureTests(RedisFixture fixture)
{
    private static readonly DateTimeOffset Start =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PresenceMutation_AddsUserToSessionIndex()
    {
        Guid userId = await RegisterAsync("indexed");
        var discovery = new RedisConnectSessionDiscovery(fixture.Multiplexer);

        List<Guid> users = await ReadUsersAsync(discovery);

        users.ShouldContain(userId);
    }

    [Fact]
    public async Task SessionDiscovery_RemovesMissingPresenceCandidate()
    {
        Guid userId = await RegisterAsync("remove-candidate");
        var discovery = new RedisConnectSessionDiscovery(fixture.Multiplexer);

        await discovery.RemoveCandidateAsync(userId);

        (await ReadUsersAsync(discovery)).ShouldNotContain(userId);
    }

    [Fact]
    public async Task CleanupLease_OnlyOneOwnerAcquiresUser()
    {
        var cleanupLease = new RedisConnectCleanupLease(
            fixture.Multiplexer,
            new ConnectCleanupLeaseOptions(TimeSpan.FromSeconds(5)));
        var userId = Guid.NewGuid();

        await using IAsyncDisposable? first = await cleanupLease.TryAcquireAsync(userId);
        await using IAsyncDisposable? second = await cleanupLease.TryAcquireAsync(userId);

        first.ShouldNotBeNull();
        second.ShouldBeNull();
    }

    [Fact]
    public async Task CleanupLease_DisposeReleasesOwnedLease()
    {
        var cleanupLease = new RedisConnectCleanupLease(
            fixture.Multiplexer,
            new ConnectCleanupLeaseOptions(TimeSpan.FromSeconds(5)));
        var userId = Guid.NewGuid();
        IAsyncDisposable? first = await cleanupLease.TryAcquireAsync(userId);
        await first!.DisposeAsync();

        await using IAsyncDisposable? next = await cleanupLease.TryAcquireAsync(userId);

        next.ShouldNotBeNull();
    }

    [Fact]
    public async Task RegisteredConnection_WithMissingLease_IsExpired()
    {
        string connectionId = $"expired-{Guid.NewGuid():N}";
        Guid userId = await RegisterAsync(connectionId);
        await fixture.Multiplexer.GetDatabase()
            .KeyDeleteAsync(ConnectRedisKeys.Lease(userId, connectionId));
        RedisConnectStateStore store = fixture.CreateStore();
        var service = new DetectMissingLeasesService(
            store,
            new ExpireConnectionsHandler(store, new ConnectStateCoordinator()));

        ConnectApplicationResult result = await service.ExecuteAsync(
            new DetectMissingLeasesCommand(userId, Guid.NewGuid(), Start.AddMinutes(1)));

        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        result.Outcome!.RemovedConnectionCount.ShouldBe(1);
        result.Presence!.Devices.Single().Connections.ShouldBeEmpty();
    }

    [Fact]
    public async Task RegisteredConnection_WithFreshLease_IsPreserved()
    {
        string connectionId = $"fresh-{Guid.NewGuid():N}";
        Guid userId = await RegisterAsync(connectionId);
        RedisConnectStateStore store = fixture.CreateStore();
        var service = new DetectMissingLeasesService(
            store,
            new ExpireConnectionsHandler(store, new ConnectStateCoordinator()));

        ConnectApplicationResult result = await service.ExecuteAsync(
            new DetectMissingLeasesCommand(userId, Guid.NewGuid(), Start.AddMinutes(1)));

        result.Status.ShouldBe(ConnectCommandStatus.NoChanges);
        result.Presence!.Devices.Single().Connections.Single().ConnectionId
            .ShouldBe(connectionId);
    }

    private async Task<Guid> RegisterAsync(string connectionId)
    {
        var userId = Guid.NewGuid();
        var handler = new RegisterConnectionHandler(fixture.CreateStore());
        ConnectApplicationResult result = await handler.HandleAsync(
            new RegisterConnectionCommand(
                userId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Browser",
                connectionId,
                Start));
        result.Status.ShouldBe(ConnectCommandStatus.Applied);
        return userId;
    }

    private static async Task<List<Guid>> ReadUsersAsync(
        RedisConnectSessionDiscovery discovery)
    {
        var users = new List<Guid>();
        await foreach (Guid userId in discovery.GetCandidateUsersAsync())
        {
            users.Add(userId);
        }
        return users;
    }
}
