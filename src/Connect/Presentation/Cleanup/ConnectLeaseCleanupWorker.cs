using System.Diagnostics;
using Connect.Application.Abstractions.Handlers;
using Connect.Application.Abstractions.Persistence;
using Connect.Application.Commands;
using Connect.Application.Results;
using Connect.Presentation.Broadcasting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Connect.Presentation.Cleanup;

public sealed class ConnectLeaseCleanupWorker(
    IConnectSessionDiscovery discovery,
    IConnectCleanupLease cleanupLease,
    IServiceScopeFactory scopeFactory,
    ConnectCleanupOptions options,
    TimeProvider timeProvider,
    ConnectCleanupMetrics metrics,
    ILogger<ConnectLeaseCleanupWorker> logger)
    : BackgroundService
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private int _cycleRunning;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _cycleRunning, 1, 0) != 0)
        {
            metrics.CyclesSkipped.Add(1);
            return;
        }

        long started = Stopwatch.GetTimestamp();
        metrics.Cycles.Add(1);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Connect cleanup cycle started. WorkerInstanceId={WorkerInstanceId}",
                _instanceId);
        }

        try
        {
            var candidates = new List<Guid>(options.BatchSize);
            await foreach (Guid userId in discovery
                               .GetCandidateUsersAsync(cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                candidates.Add(userId);
                if (candidates.Count >= options.BatchSize)
                {
                    break;
                }
            }

            metrics.UsersScanned.Add(candidates.Count);
            await Parallel.ForEachAsync(
                candidates,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = options.MaxConcurrency
                },
                ProcessUserAsync);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            metrics.Failures.Add(1);
            logger.LogWarning(
                exception,
                "Connect cleanup discovery unavailable. WorkerInstanceId={WorkerInstanceId}",
                _instanceId);
        }
        finally
        {
            double durationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            metrics.Duration.Record(durationMs);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Connect cleanup cycle completed. WorkerInstanceId={WorkerInstanceId} Duration={Duration}",
                    _instanceId,
                    durationMs);
            }
            Volatile.Write(ref _cycleRunning, 0);
        }
    }

    private async ValueTask ProcessUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using IAsyncDisposable? lease =
                await cleanupLease.TryAcquireAsync(userId, cancellationToken);
            if (lease is null)
            {
                return;
            }

            using IServiceScope scope = scopeFactory.CreateScope();
            IDetectMissingLeasesService cleanup =
                scope.ServiceProvider.GetRequiredService<IDetectMissingLeasesService>();
            IConnectBroadcaster broadcaster =
                scope.ServiceProvider.GetRequiredService<IConnectBroadcaster>();
            var commandId = Guid.NewGuid();
            ConnectApplicationResult result = await cleanup.ExecuteAsync(
                new DetectMissingLeasesCommand(
                    userId,
                    commandId,
                    timeProvider.GetUtcNow()),
                cancellationToken);

            if (result.Status == ConnectCommandStatus.Applied)
            {
                await broadcaster.BroadcastAsync(userId, commandId, result, cancellationToken);
                int expired = result.Outcome?.RemovedConnectionCount ?? 0;
                metrics.ConnectionsExpired.Add(expired);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Connect leases expired. WorkerInstanceId={WorkerInstanceId} UserId={UserId} ExpiredConnectionCount={ExpiredConnectionCount} ApplicationStatus={ApplicationStatus} PlayerVersion={PlayerVersion} PresenceVersion={PresenceVersion}",
                        _instanceId,
                        userId,
                        expired,
                        result.Status,
                        result.Outcome?.PlayerVersion,
                        result.Outcome?.PresenceVersion);
                }
                return;
            }

            if (result.Status == ConnectCommandStatus.NoChanges)
            {
                if (result.Presence is { Devices.Count: 0 })
                {
                    await discovery.RemoveCandidateAsync(userId, cancellationToken);
                }
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Connect cleanup found no expired leases. WorkerInstanceId={WorkerInstanceId} UserId={UserId}",
                        _instanceId,
                        userId);
                }
                return;
            }

            if (result.Status == ConnectCommandStatus.Conflict)
            {
                metrics.Conflicts.Add(1);
            }
            else
            {
                metrics.Failures.Add(1);
            }
            logger.LogWarning(
                "Connect cleanup did not complete. WorkerInstanceId={WorkerInstanceId} UserId={UserId} ApplicationStatus={ApplicationStatus}",
                _instanceId,
                userId,
                result.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            metrics.Failures.Add(1);
            logger.LogError(
                exception,
                "Connect cleanup failed for user. WorkerInstanceId={WorkerInstanceId} UserId={UserId}",
                _instanceId,
                userId);
        }
    }
}
