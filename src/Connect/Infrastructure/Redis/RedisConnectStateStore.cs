using System.Globalization;
using Connect.Application.Abstractions.Persistence;
using Connect.Domain.Player;
using Connect.Domain.Presence;
using Connect.Domain.Queue;
using Connect.Infrastructure.Redis.Models;
using Connect.Infrastructure.Redis.Scripts;
using Connect.Infrastructure.Redis.Serialization;
using StackExchange.Redis;

namespace Connect.Infrastructure.Redis;

public sealed class RedisConnectStateStore : IConnectStateStore
{
    private readonly IDatabase _database;
    private readonly ConnectRedisOptions _options;
    private readonly ConnectRedisSerializer _serializer = new();

    public RedisConnectStateStore(
        IConnectionMultiplexer multiplexer,
        ConnectRedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _database = multiplexer.GetDatabase();
        _options = options;
    }

    public Task<PersistenceReadResult<PlayerState>> ReadPlayerAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default) =>
        ReadStateAsync<PlayerStateRedisModel, PlayerState>(
            ConnectRedisKeys.Player(userId),
            () => new PlayerState(serverTime),
            ConnectRedisMapper.ToDomain,
            static state => state.Version,
            cancellationToken);

    public Task<PersistenceReadResult<QueueState>> ReadQueueAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        ReadStateAsync<QueueStateRedisModel, QueueState>(
            ConnectRedisKeys.Queue(userId),
            static () => new QueueState(),
            ConnectRedisMapper.ToDomain,
            static state => state.Version,
            cancellationToken);

    public Task<PersistenceReadResult<PresenceState>> ReadPresenceAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        ReadStateAsync<PresenceStateRedisModel, PresenceState>(
            ConnectRedisKeys.Presence(userId),
            static () => new PresenceState(),
            ConnectRedisMapper.ToDomain,
            static state => state.Version,
            cancellationToken);

    public async Task<ConnectSnapshotReadResult> ReadSnapshotAsync(
        Guid userId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            RedisResult result = await _database.ScriptEvaluateAsync(
                ConnectRedisScripts.ReadSnapshot,
                [
                    ConnectRedisKeys.Player(userId),
                    ConnectRedisKeys.Queue(userId),
                    ConnectRedisKeys.Presence(userId)
                ]);
            RedisResult[] values = (RedisResult[]?)result
                ?? throw new InvalidOperationException("Redis snapshot result is missing.");

            if (!TryMapSnapshot(values, serverTime, out ConnectSnapshotState? snapshot, out string? error))
            {
                return new ConnectSnapshotReadResult(
                    PersistenceStatus.CorruptState,
                    null,
                    error);
            }

            return new ConnectSnapshotReadResult(
                PersistenceStatus.Success,
                snapshot,
                null);
        }
        catch (RedisException exception)
        {
            return new ConnectSnapshotReadResult(
                PersistenceStatus.Unavailable,
                null,
                exception.Message);
        }
    }

    public Task<PersistenceCommitResult> TryRecordCommandAsync(
        CommandRecordCommit commit,
        CancellationToken cancellationToken = default) =>
        CommitAsync(
            commit.UserId,
            commit.Command,
            [],
            commit.RegisteredConnectionId,
            cancellationToken);

    public Task<PersistenceCommitResult> TryCommitPlayerAsync(
        PlayerCommit commit,
        CancellationToken cancellationToken = default) =>
        CommitAsync(
            commit.UserId,
            commit.Command,
            [
                new StateWrite(
                    ConnectRedisKeys.Player(commit.UserId),
                    commit.ExpectedVersion,
                    commit.State.Version,
                    _serializer.Serialize(ConnectRedisMapper.ToRedis(commit.State)))
            ],
            null,
            cancellationToken);

    public Task<PersistenceCommitResult> TryCommitQueueAsync(
        QueueCommit commit,
        CancellationToken cancellationToken = default) =>
        CommitAsync(
            commit.UserId,
            commit.Command,
            [
                new StateWrite(
                    ConnectRedisKeys.Queue(commit.UserId),
                    commit.ExpectedVersion,
                    commit.State.Version,
                    _serializer.Serialize(ConnectRedisMapper.ToRedis(commit.State)))
            ],
            null,
            cancellationToken);

    public Task<PersistenceCommitResult> TryCommitPresenceAsync(
        PresenceCommit commit,
        CancellationToken cancellationToken = default) =>
        CommitAsync(
            commit.UserId,
            commit.Command,
            [
                new StateWrite(
                    ConnectRedisKeys.Presence(commit.UserId),
                    commit.ExpectedVersion,
                    commit.State.Version,
                    _serializer.Serialize(ConnectRedisMapper.ToRedis(commit.State)))
            ],
            commit.RegisteredConnectionId,
            cancellationToken);

    public Task<PersistenceCommitResult> TryCommitPlayerAndQueueAsync(
        PlayerQueueCommit commit,
        CancellationToken cancellationToken = default) =>
        CommitAsync(
            commit.UserId,
            commit.Command,
            [
                new StateWrite(
                    ConnectRedisKeys.Player(commit.UserId),
                    commit.ExpectedPlayerVersion,
                    commit.Player.Version,
                    _serializer.Serialize(ConnectRedisMapper.ToRedis(commit.Player))),
                new StateWrite(
                    ConnectRedisKeys.Queue(commit.UserId),
                    commit.ExpectedQueueVersion,
                    commit.Queue.Version,
                    _serializer.Serialize(ConnectRedisMapper.ToRedis(commit.Queue)))
            ],
            null,
            cancellationToken);

    public Task<PersistenceCommitResult> TryCommitPlayerAndPresenceAsync(
        PlayerPresenceCommit commit,
        CancellationToken cancellationToken = default) =>
        CommitAsync(
            commit.UserId,
            commit.Command,
            [
                new StateWrite(
                    ConnectRedisKeys.Player(commit.UserId),
                    commit.ExpectedPlayerVersion,
                    commit.Player.Version,
                    _serializer.Serialize(ConnectRedisMapper.ToRedis(commit.Player))),
                new StateWrite(
                    ConnectRedisKeys.Presence(commit.UserId),
                    commit.ExpectedPresenceVersion,
                    commit.Presence.Version,
                    _serializer.Serialize(ConnectRedisMapper.ToRedis(commit.Presence)))
            ],
            null,
            cancellationToken);

    public async Task<PersistenceStatus> RefreshConnectionLeaseAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            RedisResult result = await _database.ScriptEvaluateAsync(
                ConnectRedisScripts.RefreshLease,
                [
                    ConnectRedisKeys.Presence(userId),
                    ConnectRedisKeys.Lease(userId, connectionId)
                ],
                [
                    connectionId,
                    ToMilliseconds(_options.ConnectionLeaseTtl)
                ]);
            string status = (string)result!;
            return status switch
            {
                "applied" => PersistenceStatus.Applied,
                "corrupt" => PersistenceStatus.CorruptState,
                "missing" => PersistenceStatus.ConnectionNotFound,
                "not-found" => PersistenceStatus.ConnectionNotFound,
                _ => throw new InvalidOperationException(
                    $"Redis returned unsupported lease status '{status}'.")
            };
        }
        catch (RedisException)
        {
            return PersistenceStatus.Unavailable;
        }
    }

    public async Task<ExpiredConnectionsReadResult> ReadExpiredConnectionsAsync(
        Guid userId,
        PresenceState presence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presence);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            DeviceConnection[] connections =
            [
                .. presence.Devices.SelectMany(device => device.Connections)
            ];
            Task<bool>[] checks = connections.Select(connection =>
                    _database.KeyExistsAsync(
                        ConnectRedisKeys.Lease(userId, connection.ConnectionId)))
                .ToArray();
            bool[] leaseExists = await Task.WhenAll(checks);
            string[] expired = connections
                .Where((_, index) => !leaseExists[index])
                .Select(connection => connection.ConnectionId)
                .ToArray();

            return new ExpiredConnectionsReadResult(
                PersistenceStatus.Success,
                expired,
                null);
        }
        catch (RedisException exception)
        {
            return new ExpiredConnectionsReadResult(
                PersistenceStatus.Unavailable,
                [],
                exception.Message);
        }
    }

    private async Task<PersistenceReadResult<TDomain>> ReadStateAsync<TModel, TDomain>(
        RedisKey key,
        Func<TDomain> createInitial,
        Func<TModel, TDomain> map,
        Func<TDomain, long> getVersion,
        CancellationToken cancellationToken)
        where TModel : class
        where TDomain : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            RedisValue[] values = await _database.HashGetAsync(key, ["json", "version"]);
            if (values[0].IsNull && values[1].IsNull)
            {
                return new PersistenceReadResult<TDomain>(
                    PersistenceStatus.Success,
                    createInitial(),
                    null);
            }

            if (values[0].IsNull ||
                !TryReadInt64(values[1].ToString(), out long storedVersion))
            {
                return new PersistenceReadResult<TDomain>(
                    PersistenceStatus.CorruptState,
                    null,
                    "Redis state hash is missing a valid version or JSON value.");
            }

            if (!_serializer.TryDeserialize(
                    values[0].ToString(),
                    out TModel? model,
                    out string? error))
            {
                return new PersistenceReadResult<TDomain>(
                    PersistenceStatus.CorruptState,
                    null,
                    error);
            }

            try
            {
                TDomain state = map(model!);
                if (getVersion(state) != storedVersion)
                {
                    return new PersistenceReadResult<TDomain>(
                        PersistenceStatus.CorruptState,
                        null,
                        "Redis state hash version does not match JSON state version.");
                }

                return new PersistenceReadResult<TDomain>(
                    PersistenceStatus.Success,
                    state,
                    null);
            }
            catch (Exception exception) when (
                exception is ArgumentException or InvalidOperationException or OverflowException)
            {
                return new PersistenceReadResult<TDomain>(
                    PersistenceStatus.CorruptState,
                    null,
                    exception.Message);
            }
        }
        catch (RedisException exception)
        {
            return new PersistenceReadResult<TDomain>(
                PersistenceStatus.Unavailable,
                null,
                exception.Message);
        }
    }

    private async Task<PersistenceCommitResult> CommitAsync(
        Guid userId,
        PersistenceCommand command,
        IReadOnlyList<StateWrite> writes,
        string? registeredConnectionId,
        CancellationToken cancellationToken)
    {
        ValidateCommand(command);
        foreach (StateWrite write in writes)
        {
            if (write.ExpectedVersion < 0 ||
                write.NewVersion != checked(write.ExpectedVersion + 1))
            {
                throw new ArgumentException(
                    "Commit state version must equal expected version plus one.",
                    nameof(writes));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        RedisKey[] keys =
        [
            .. writes.Select(write => (RedisKey)write.Key),
            ConnectRedisKeys.Command(userId, command.CommandId),
            ConnectRedisKeys.Player(userId),
            ConnectRedisKeys.Queue(userId),
            ConnectRedisKeys.Presence(userId)
        ];
        var arguments = new List<RedisValue>
        {
            command.CommandId.ToString("D"),
            $"{command.CommandType}\n{command.Fingerprint}",
            command.Outcome,
            ToMilliseconds(_options.StateTtl),
            ToMilliseconds(_options.CommandDeduplicationTtl),
            writes.Count
        };
        foreach (StateWrite write in writes)
        {
            arguments.Add(write.ExpectedVersion);
            arguments.Add(write.NewVersion);
            arguments.Add(write.Json);
        }

        arguments.Add(
            registeredConnectionId is null
                ? RedisValue.EmptyString
                : ConnectRedisKeys.Lease(userId, registeredConnectionId));
        arguments.Add(ToMilliseconds(_options.ConnectionLeaseTtl));

        try
        {
            RedisResult result = await _database.ScriptEvaluateAsync(
                ConnectRedisScripts.Commit,
                keys,
                [.. arguments]);
            RedisResult[] values = (RedisResult[]?)result
                ?? throw new InvalidOperationException("Redis commit result is missing.");
            return MapCommitResult(values);
        }
        catch (RedisException)
        {
            return new PersistenceCommitResult(
                PersistenceStatus.Unavailable,
                null,
                new ConnectVersions(null, null, null));
        }
    }

    private bool TryMapSnapshot(
        RedisResult[] values,
        DateTimeOffset serverTime,
        out ConnectSnapshotState? snapshot,
        out string? error)
    {
        if (!TryMapSnapshotState<PlayerStateRedisModel, PlayerState>(
                values[0],
                values[1],
                () => new PlayerState(serverTime),
                ConnectRedisMapper.ToDomain,
                static state => state.Version,
                out PlayerState? player,
                out error) ||
            !TryMapSnapshotState<QueueStateRedisModel, QueueState>(
                values[2],
                values[3],
                static () => new QueueState(),
                ConnectRedisMapper.ToDomain,
                static state => state.Version,
                out QueueState? queue,
                out error) ||
            !TryMapSnapshotState<PresenceStateRedisModel, PresenceState>(
                values[4],
                values[5],
                static () => new PresenceState(),
                ConnectRedisMapper.ToDomain,
                static state => state.Version,
                out PresenceState? presence,
                out error))
        {
            snapshot = null;
            return false;
        }

        snapshot = new ConnectSnapshotState(player!, queue!, presence!);
        return true;
    }

    private bool TryMapSnapshotState<TModel, TDomain>(
        RedisResult value,
        RedisResult versionValue,
        Func<TDomain> createInitial,
        Func<TModel, TDomain> map,
        Func<TDomain, long> getVersion,
        out TDomain? state,
        out string? error)
        where TModel : class
        where TDomain : class
    {
        if (value.IsNull && versionValue.IsNull)
        {
            state = createInitial();
            error = null;
            return true;
        }

        if (value.IsNull ||
            string.Equals((string?)value, "__corrupt", StringComparison.Ordinal) ||
            !TryReadInt64((string?)versionValue, out long storedVersion))
        {
            state = null;
            error = "Redis state hash is missing a valid version or JSON value.";
            return false;
        }

        if (!_serializer.TryDeserialize(
                (string)value!,
                out TModel? model,
                out error))
        {
            state = null;
            return false;
        }

        try
        {
            state = map(model!);
            if (getVersion(state) != storedVersion)
            {
                state = null;
                error = "Redis state hash version does not match JSON state version.";
                return false;
            }

            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            state = null;
            error = exception.Message;
            return false;
        }
    }

    private static PersistenceCommitResult MapCommitResult(
        RedisResult[] values)
    {
        string status = (string)values[0]!;
        string? outcome = values.Length > 1 ? (string?)values[1] : null;
        var versions = new ConnectVersions(
            ReadVersion(values, 2),
            ReadVersion(values, 3),
            ReadVersion(values, 4));

        return status switch
        {
            "applied" => new PersistenceCommitResult(
                PersistenceStatus.Applied,
                outcome,
                versions),
            "duplicate" => new PersistenceCommitResult(
                PersistenceStatus.Duplicate,
                outcome,
                versions),
            "conflict" => new PersistenceCommitResult(
                PersistenceStatus.VersionConflict,
                null,
                versions),
            "collision" => new PersistenceCommitResult(
                PersistenceStatus.CommandCollision,
                outcome,
                versions),
            "corrupt" => new PersistenceCommitResult(
                PersistenceStatus.CorruptState,
                null,
                versions),
            "invalid-version" => throw new InvalidOperationException(
                "Commit state version must equal expected version plus one."),
            _ => throw new InvalidOperationException(
                $"Redis returned unsupported commit status '{status}'.")
        };
    }

    private static long? ReadVersion(
        RedisResult[] values,
        int index)
    {
        if (values.Length <= index ||
            !TryReadInt64((string?)values[index], out long version) ||
            version < 0)
        {
            return null;
        }

        return version;
    }

    private static void ValidateCommand(PersistenceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.CommandId == Guid.Empty)
        {
            throw new ArgumentException("Command ID is required.", nameof(command));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.CommandType);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Fingerprint);
    }

    private static long ToMilliseconds(TimeSpan value) =>
        checked((long)value.TotalMilliseconds);

    private static bool TryReadInt64(string? value, out long result) =>
        long.TryParse(
            value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out result);

    private sealed record StateWrite(
        string Key,
        long ExpectedVersion,
        long NewVersion,
        string Json);
}
