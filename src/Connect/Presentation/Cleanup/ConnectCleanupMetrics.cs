using System.Diagnostics.Metrics;

namespace Connect.Presentation.Cleanup;

public sealed class ConnectCleanupMetrics : IDisposable
{
    private readonly Meter _meter = new("Voxxy.Connect.Cleanup");

    public Counter<long> Cycles { get; }
    public Counter<long> UsersScanned { get; }
    public Counter<long> ConnectionsExpired { get; }
    public Counter<long> Conflicts { get; }
    public Counter<long> Failures { get; }
    public Counter<long> CyclesSkipped { get; }
    public Histogram<double> Duration { get; }

    public ConnectCleanupMetrics()
    {
        Cycles = _meter.CreateCounter<long>("connect.cleanup.cycles");
        UsersScanned = _meter.CreateCounter<long>("connect.cleanup.users_scanned");
        ConnectionsExpired = _meter.CreateCounter<long>("connect.cleanup.connections_expired");
        Conflicts = _meter.CreateCounter<long>("connect.cleanup.conflicts");
        Failures = _meter.CreateCounter<long>("connect.cleanup.failures");
        CyclesSkipped = _meter.CreateCounter<long>("connect.cleanup.cycles_skipped");
        Duration = _meter.CreateHistogram<double>("connect.cleanup.duration", "ms");
    }

    public void Dispose() => _meter.Dispose();
}
