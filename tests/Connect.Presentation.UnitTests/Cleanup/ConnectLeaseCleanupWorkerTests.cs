using System.Runtime.CompilerServices;
using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Results;
using Connect.Contracts.States;
using Connect.Presentation.Broadcasting;
using Connect.Presentation.Cleanup;
using Connect.Presentation.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Connect.Presentation.UnitTests.Cleanup;

public sealed class ConnectLeaseCleanupWorkerTests
{
    [Fact]
    public async Task CleanupWorker_ProcessesCandidates_AndBroadcastsChange()
    {
        var userId = Guid.NewGuid();
        var discovery = new TestDiscovery(userId);
        var handler = new TestCleanupHandler(_ => Applied());
        var broadcaster = new TestBroadcaster();
        using ConnectCleanupMetrics metrics = new();
        using ConnectLeaseCleanupWorker worker = CreateWorker(
            discovery,
            handler,
            broadcaster,
            metrics);

        await worker.RunCycleAsync();

        handler.Users.ShouldBe([userId]);
        broadcaster.Calls.ShouldBe(1);
        broadcaster.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task CleanupWorker_DoesNotBroadcastNoChanges()
    {
        var discovery = new TestDiscovery(Guid.NewGuid());
        var handler = new TestCleanupHandler(_ => NoChanges());
        var broadcaster = new TestBroadcaster();
        using ConnectCleanupMetrics metrics = new();
        using ConnectLeaseCleanupWorker worker = CreateWorker(
            discovery,
            handler,
            broadcaster,
            metrics);

        await worker.RunCycleAsync();

        broadcaster.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task CleanupWorker_ContinuesAfterSingleUserFailure()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var discovery = new TestDiscovery(first, second);
        var handler = new TestCleanupHandler(userId =>
            userId == first ? throw new InvalidOperationException("failure") : Applied());
        var broadcaster = new TestBroadcaster();
        using ConnectCleanupMetrics metrics = new();
        using ConnectLeaseCleanupWorker worker = CreateWorker(
            discovery,
            handler,
            broadcaster,
            metrics);

        await worker.RunCycleAsync();

        handler.Users.Count.ShouldBe(2);
        broadcaster.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task CleanupWorker_RemovesEmptyPresenceCandidate()
    {
        var userId = Guid.NewGuid();
        var discovery = new TestDiscovery(userId);
        var handler = new TestCleanupHandler(_ => NoChanges());
        using ConnectCleanupMetrics metrics = new();
        using ConnectLeaseCleanupWorker worker = CreateWorker(
            discovery,
            handler,
            new TestBroadcaster(),
            metrics);

        await worker.RunCycleAsync();

        discovery.Removed.ShouldBe([userId]);
    }

    [Fact]
    public async Task CleanupWorker_UsesBoundedConcurrency()
    {
        Guid[] users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        var discovery = new TestDiscovery(users);
        var handler = new TestCleanupHandler(async _ =>
        {
            await Task.Delay(20);
            return Applied();
        });
        using ConnectCleanupMetrics metrics = new();
        using ConnectLeaseCleanupWorker worker = CreateWorker(
            discovery,
            handler,
            new TestBroadcaster(),
            metrics,
            maxConcurrency: 2);

        await worker.RunCycleAsync();

        handler.MaximumConcurrency.ShouldBeLessThanOrEqualTo(2);
    }

    private static ConnectLeaseCleanupWorker CreateWorker(
        TestDiscovery discovery,
        TestCleanupHandler handler,
        TestBroadcaster broadcaster,
        ConnectCleanupMetrics metrics,
        int maxConcurrency = 4)
    {
        var services = new ServiceCollection();
        services.AddScoped<IDetectMissingLeasesService>(_ => handler);
        services.AddSingleton<IConnectBroadcaster>(broadcaster);
        ServiceProvider provider = services.BuildServiceProvider();
        return new ConnectLeaseCleanupWorker(
            discovery,
            new TestLease(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            new ConnectCleanupOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                BatchSize = 100,
                MaxConcurrency = maxConcurrency,
                UserLeaseTtl = TimeSpan.FromSeconds(5)
            },
            TimeProvider.System,
            metrics,
            NullLogger<ConnectLeaseCleanupWorker>.Instance);
    }

    private static ConnectApplicationResult Applied() =>
        new(
            ConnectCommandStatus.Applied,
            Presence: EmptyPresence(1),
            Outcome: new ConnectCommandOutcome(
                PresenceVersion: 1,
                RemovedConnectionCount: 1));

    private static ConnectApplicationResult NoChanges() =>
        new(
            ConnectCommandStatus.NoChanges,
            Presence: EmptyPresence(0),
            Outcome: new ConnectCommandOutcome(
                PresenceVersion: 0,
                RemovedConnectionCount: 0));

    private static PresenceStateDto EmptyPresence(long version) =>
        new([], null, null, version);

    private sealed class TestDiscovery(params Guid[] users) : IConnectSessionDiscovery
    {
        public List<Guid> Removed { get; } = [];

        public async IAsyncEnumerable<Guid> GetCandidateUsersAsync(
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            foreach (Guid user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return user;
            }
        }

        public Task RemoveCandidateAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            Removed.Add(userId);
            return Task.CompletedTask;
        }
    }

    private sealed class TestLease : IConnectCleanupLease
    {
        public Task<IAsyncDisposable?> TryAcquireAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IAsyncDisposable?>(new Handle());

        private sealed class Handle : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class TestCleanupHandler : IDetectMissingLeasesService
    {
        private readonly Func<Guid, Task<ConnectApplicationResult>> _execute;
        private int _active;

        public TestCleanupHandler(Func<Guid, ConnectApplicationResult> execute)
            : this(userId => Task.FromResult(execute(userId)))
        {
        }

        public TestCleanupHandler(Func<Guid, Task<ConnectApplicationResult>> execute) =>
            _execute = execute;

        public List<Guid> Users { get; } = [];
        public int MaximumConcurrency { get; private set; }

        public async Task<ConnectApplicationResult> ExecuteAsync(
            DetectMissingLeasesCommand command,
            CancellationToken cancellationToken = default)
        {
            lock (Users)
            {
                Users.Add(command.UserId);
            }
            int active = Interlocked.Increment(ref _active);
            MaximumConcurrency = Math.Max(MaximumConcurrency, active);
            try
            {
                return await _execute(command.UserId);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
    }
}
