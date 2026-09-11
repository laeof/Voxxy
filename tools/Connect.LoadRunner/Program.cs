using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

LoadProfile profile = LoadProfile.Parse(args);
TokenIdentity[] identities = await LoadIdentitiesAsync(profile.TokenFile);
if (identities.Length < profile.Connections)
{
    throw new InvalidOperationException(
        $"Profile requires {profile.Connections} identities/tokens; file has {identities.Length}.");
}

var measurements = new ConcurrentBag<Measurement>();
var statuses = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
var clients = new List<HubConnection>(profile.Connections);
using var runTimeout = new CancellationTokenSource(profile.Duration + TimeSpan.FromMinutes(2));

try
{
    await Parallel.ForEachAsync(
        Enumerable.Range(0, profile.Connections),
        new ParallelOptions
        {
            MaxDegreeOfParallelism = profile.ConnectionParallelism,
            CancellationToken = runTimeout.Token,
        },
        async (index, cancellationToken) =>
        {
            TokenIdentity identity = identities[index];
            HubConnection connection = new HubConnectionBuilder()
                .WithUrl(
                    profile.HubUrl,
                    options =>
                    {
                        options.Transports = HttpTransportType.WebSockets;
                        options.SkipNegotiation = true;
                        options.AccessTokenProvider =
                            () => Task.FromResult<string?>(identity.AccessToken);
                    })
                .WithAutomaticReconnect()
                .Build();
            await MeasureAsync("connect", () => connection.StartAsync(cancellationToken));
            lock (clients)
            {
                clients.Add(connection);
            }

            Snapshot snapshot = await MeasureValueAsync(
                "snapshot",
                () => connection.InvokeAsync<Snapshot>("GetSnapshot", cancellationToken));
            Count(snapshot.Status);
            CommandAck registration = await MeasureValueAsync(
                "register",
                () => connection.InvokeAsync<CommandAck>(
                    "RegisterConnection",
                    new
                    {
                        commandId = Guid.NewGuid(),
                        deviceId = StableDeviceId(identity.UserId, index),
                        deviceName = "Connect Load Runner",
                    },
                    cancellationToken));
            Count(registration.Status);
        });

    using var commandTimer = new PeriodicTimer(profile.CommandInterval);
    DateTimeOffset finishAt = DateTimeOffset.UtcNow + profile.Duration;
    var random = new Random(profile.Seed);
    while (DateTimeOffset.UtcNow < finishAt &&
           await commandTimer.WaitForNextTickAsync(runTimeout.Token))
    {
        HubConnection client = clients[random.Next(clients.Count)];
        int selection = random.Next(100);
        if (selection < 60)
        {
            CommandAck ack = await MeasureValueAsync(
                "heartbeat",
                () => client.InvokeAsync<CommandAck>(
                    "RefreshConnectionLease",
                    runTimeout.Token));
            Count(ack.Status);
        }
        else if (selection < 72)
        {
            await MutationAsync(client, "ChangePosition", new
            {
                commandId = Guid.NewGuid(),
                positionMs = random.Next(0, 300_000),
            });
        }
        else if (selection < 80)
        {
            await MutationAsync(client, "ChangeVolume", new
            {
                commandId = Guid.NewGuid(),
                volumePercent = random.Next(0, 101),
            });
        }
        else if (selection < 86)
        {
            await MutationAsync(
                client,
                selection % 2 == 0 ? "Play" : "Pause",
                new { commandId = Guid.NewGuid() });
        }
        else
        {
            Snapshot snapshot = await MeasureValueAsync(
                "snapshot",
                () => client.InvokeAsync<Snapshot>(
                    "GetSnapshot",
                    runTimeout.Token));
            Count(snapshot.Status);
        }
    }
}
finally
{
    await Parallel.ForEachAsync(clients, async (client, _) =>
    {
        await client.StopAsync(CancellationToken.None);
        await client.DisposeAsync();
    });
}

LoadResult result = LoadResult.Create(profile, measurements, statuses);
Directory.CreateDirectory(profile.OutputDirectory);
string jsonPath = Path.Combine(profile.OutputDirectory, "connect-load-results.json");
string markdownPath = Path.Combine(profile.OutputDirectory, "connect-load-summary.md");
await File.WriteAllTextAsync(
    jsonPath,
    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
await File.WriteAllTextAsync(markdownPath, result.ToMarkdown());
Console.WriteLine(result.ToMarkdown());

async Task MutationAsync(HubConnection connection, string method, object request)
{
    CommandAck ack = await MeasureValueAsync(
        "mutation",
        () => connection.InvokeAsync<CommandAck>(method, request, runTimeout.Token));
    Count(ack.Status);
}

async Task MeasureAsync(string operation, Func<Task> action)
{
    long started = Stopwatch.GetTimestamp();
    try
    {
        await action();
        measurements.Add(new Measurement(operation, Elapsed(started), true));
    }
    catch
    {
        measurements.Add(new Measurement(operation, Elapsed(started), false));
        throw;
    }
}

async Task<T> MeasureValueAsync<T>(string operation, Func<Task<T>> action)
{
    long started = Stopwatch.GetTimestamp();
    try
    {
        T result = await action();
        measurements.Add(new Measurement(operation, Elapsed(started), true));
        return result;
    }
    catch
    {
        measurements.Add(new Measurement(operation, Elapsed(started), false));
        throw;
    }
}

void Count(string status) => statuses.AddOrUpdate(status, 1, (_, value) => value + 1);
static double Elapsed(long started) => Stopwatch.GetElapsedTime(started).TotalMilliseconds;
static Guid StableDeviceId(Guid userId, int index)
{
    byte[] bytes = userId.ToByteArray();
    BitConverter.GetBytes(index).CopyTo(bytes, 0);
    return new Guid(bytes);
}

static async Task<TokenIdentity[]> LoadIdentitiesAsync(string file)
{
    await using FileStream stream = File.OpenRead(file);
    return await JsonSerializer.DeserializeAsync<TokenIdentity[]>(
               stream,
               new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
           ?? [];
}

internal sealed record TokenIdentity(Guid UserId, string AccessToken);
internal sealed record Snapshot(string Status);
internal sealed record CommandAck(string Status);
internal sealed record Measurement(string Operation, double DurationMs, bool Success);

internal sealed record LoadProfile(
    string Name,
    Uri HubUrl,
    string TokenFile,
    string OutputDirectory,
    int Connections,
    int ConnectionParallelism,
    TimeSpan Duration,
    TimeSpan CommandInterval,
    int Seed)
{
    public static LoadProfile Parse(string[] args)
    {
        Dictionary<string, string> values = args
            .Select(item => item.Split('=', 2))
            .Where(item => item.Length == 2)
            .ToDictionary(item => item[0].TrimStart('-'), item => item[1]);
        string name = values.GetValueOrDefault("profile", "ci");
        (int connections, TimeSpan duration, TimeSpan interval) = name switch
        {
            "connection-ramp" => (1_000, TimeSpan.FromMinutes(10), TimeSpan.FromMilliseconds(100)),
            "steady" => (500, TimeSpan.FromMinutes(30), TimeSpan.FromMilliseconds(50)),
            "burst" => (500, TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(5)),
            "reconnect-storm" => (500, TimeSpan.FromMinutes(10), TimeSpan.FromMilliseconds(25)),
            _ => (20, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(250)),
        };
        return new LoadProfile(
            name,
            new Uri(values.GetValueOrDefault("url", "http://localhost/api/hubs/connect")),
            values.GetValueOrDefault("tokens", "connect-load-tokens.json"),
            values.GetValueOrDefault("output", "artifacts/connect-load"),
            int.Parse(values.GetValueOrDefault("connections", connections.ToString())),
            50,
            TimeSpan.FromSeconds(double.Parse(
                values.GetValueOrDefault("duration-seconds", duration.TotalSeconds.ToString()))),
            interval,
            1729);
    }
}

internal sealed record Percentiles(double P50, double P95, double P99, double Max);
internal sealed record LoadResult(
    string Profile,
    int Connections,
    double DurationSeconds,
    IReadOnlyDictionary<string, Percentiles> Latency,
    IReadOnlyDictionary<string, int> Statuses,
    double ErrorRate)
{
    public static LoadResult Create(
        LoadProfile profile,
        IEnumerable<Measurement> measurements,
        IReadOnlyDictionary<string, int> statuses)
    {
        Measurement[] all = measurements.ToArray();
        Dictionary<string, Percentiles> latency = all
            .Where(item => item.Success)
            .GroupBy(item => item.Operation)
            .ToDictionary(group => group.Key, group => Calculate(group.Select(x => x.DurationMs)));
        return new LoadResult(
            profile.Name,
            profile.Connections,
            profile.Duration.TotalSeconds,
            latency,
            new Dictionary<string, int>(statuses),
            all.Length == 0 ? 0 : all.Count(item => !item.Success) / (double)all.Length);
    }

    public string ToMarkdown()
    {
        var lines = new List<string>
        {
            "# Connect load summary",
            "",
            $"- Profile: `{Profile}`",
            $"- Connections: {Connections}",
            $"- Duration: {DurationSeconds:F0}s",
            $"- Error rate: {ErrorRate:P2}",
            "",
            "| Operation | p50 ms | p95 ms | p99 ms | max ms |",
            "|---|---:|---:|---:|---:|",
        };
        lines.AddRange(Latency.Select(item =>
            $"| {item.Key} | {item.Value.P50:F1} | {item.Value.P95:F1} | " +
            $"{item.Value.P99:F1} | {item.Value.Max:F1} |"));
        return string.Join(Environment.NewLine, lines);
    }

    private static Percentiles Calculate(IEnumerable<double> source)
    {
        double[] values = source.Order().ToArray();
        double At(double percentile) =>
            values[Math.Min(values.Length - 1, (int)Math.Ceiling(values.Length * percentile) - 1)];
        return new Percentiles(At(0.50), At(0.95), At(0.99), values[^1]);
    }
}
