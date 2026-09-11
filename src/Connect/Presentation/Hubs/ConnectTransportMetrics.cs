using System.Diagnostics.Metrics;

namespace Connect.Presentation.Hubs;

public sealed class ConnectTransportMetrics : IDisposable
{
    private readonly Meter _meter = new("Voxxy.Connect.Transport");

    public UpDownCounter<long> CurrentConnections { get; }
    public Counter<long> ConnectionsStarted { get; }
    public Counter<long> ConnectionsClosed { get; }
    public Counter<long> Commands { get; }
    public Counter<long> Conflicts { get; }
    public Counter<long> Duplicates { get; }
    public Counter<long> Failures { get; }
    public Counter<long> RateLimited { get; }
    public Counter<long> ReconnectRegistrations { get; }
    public Histogram<double> CommandDuration { get; }
    public Histogram<double> SnapshotDuration { get; }
    public Histogram<double> BroadcastDuration { get; }
    public Counter<long> BroadcastFailures { get; }

    public ConnectTransportMetrics()
    {
        CurrentConnections =
            _meter.CreateUpDownCounter<long>("connect.connections.current");
        ConnectionsStarted = _meter.CreateCounter<long>("connect.connections.started");
        ConnectionsClosed = _meter.CreateCounter<long>("connect.connections.closed");
        Commands = _meter.CreateCounter<long>("connect.commands.total");
        Conflicts = _meter.CreateCounter<long>("connect.commands.conflicts");
        Duplicates = _meter.CreateCounter<long>("connect.commands.duplicates");
        Failures = _meter.CreateCounter<long>("connect.commands.failures");
        RateLimited = _meter.CreateCounter<long>("connect.rate_limited");
        ReconnectRegistrations =
            _meter.CreateCounter<long>("connect.reconnect.registrations");
        CommandDuration =
            _meter.CreateHistogram<double>("connect.commands.duration", "ms");
        SnapshotDuration =
            _meter.CreateHistogram<double>("connect.snapshots.duration", "ms");
        BroadcastDuration =
            _meter.CreateHistogram<double>("connect.broadcast.duration", "ms");
        BroadcastFailures =
            _meter.CreateCounter<long>("connect.broadcast.failures");
    }

    public void Dispose() => _meter.Dispose();
}
