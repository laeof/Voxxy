namespace Connect.Infrastructure.Redis.Scripts;

internal static class ConnectRedisScripts
{
    // KEYS: changed state keys, per-command key, player key, queue key, presence key.
    // ARGV: command id, fingerprint, outcome, state TTL ms, command TTL ms,
    // changed state count, then expected version/new version/new JSON per state,
    // optional lease key and lease TTL ms.
    public const string Commit = """
        local commandId = ARGV[1]
        local fingerprint = ARGV[2]
        local outcome = ARGV[3]
        local stateTtl = tonumber(ARGV[4])
        local commandTtl = tonumber(ARGV[5])
        local stateCount = tonumber(ARGV[6])
        local commandKeyIndex = stateCount + 1
        local playerKeyIndex = stateCount + 2
        local queueKeyIndex = stateCount + 3
        local presenceKeyIndex = stateCount + 4

        local existingCommand = redis.call('GET', KEYS[commandKeyIndex])
        if existingCommand then
            local decoded = cjson.decode(existingCommand)
            if decoded.fingerprint ~= fingerprint then
                return {
                    'collision',
                    decoded.outcome or '',
                    tostring(decoded.playerVersion or 0),
                    tostring(decoded.queueVersion or 0),
                    tostring(decoded.presenceVersion or 0)
                }
            end
            return {
                'duplicate',
                decoded.outcome or '',
                tostring(decoded.playerVersion or 0),
                tostring(decoded.queueVersion or 0),
                tostring(decoded.presenceVersion or 0)
            }
        end

        local function readVersion(key)
            local keyType = redis.call('TYPE', key).ok
            if keyType == 'none' then
                return '0'
            end

            if keyType ~= 'hash' then
                return nil
            end

            local version = redis.call('HGET', key, 'version')
            local json = redis.call('HGET', key, 'json')
            if not version or not json then
                return nil
            end

            return version
        end

        local currentVersions = {}
        for index = 1, stateCount do
            local argumentOffset = 7 + ((index - 1) * 3)
            local expectedVersion = ARGV[argumentOffset]
            local currentVersion = readVersion(KEYS[index])

            if currentVersion == nil then
                return { 'corrupt', '' }
            end

            currentVersions[index] = currentVersion
            if currentVersion ~= expectedVersion then
                local playerVersion = readVersion(KEYS[playerKeyIndex])
                local queueVersion = readVersion(KEYS[queueKeyIndex])
                local presenceVersion = readVersion(KEYS[presenceKeyIndex])
                return {
                    'conflict',
                    '',
                    playerVersion or '-1',
                    queueVersion or '-1',
                    presenceVersion or '-1'
                }
            end
        end

        for index = 1, stateCount do
            local argumentOffset = 7 + ((index - 1) * 3)
            redis.call(
                'HSET',
                KEYS[index],
                'version',
                ARGV[argumentOffset + 1],
                'json',
                ARGV[argumentOffset + 2])
            redis.call('PEXPIRE', KEYS[index], stateTtl)
        end

        for index = playerKeyIndex, presenceKeyIndex do
            if redis.call('EXISTS', KEYS[index]) == 1 then
                redis.call('PEXPIRE', KEYS[index], stateTtl)
            end
        end

        local leaseArgumentOffset = 7 + (stateCount * 3)
        local leaseKey = ARGV[leaseArgumentOffset]
        local leaseTtl = tonumber(ARGV[leaseArgumentOffset + 1])
        if leaseKey and leaseKey ~= '' then
            redis.call('SET', leaseKey, '1', 'PX', leaseTtl)
        end

        local playerVersion = readVersion(KEYS[playerKeyIndex])
        local queueVersion = readVersion(KEYS[queueKeyIndex])
        local presenceVersion = readVersion(KEYS[presenceKeyIndex])
        local commandRecord = cjson.encode({
            fingerprint = fingerprint,
            outcome = outcome,
            playerVersion = playerVersion or '0',
            queueVersion = queueVersion or '0',
            presenceVersion = presenceVersion or '0'
        })
        redis.call('SET', KEYS[commandKeyIndex], commandRecord, 'PX', commandTtl)

        return {
            'applied',
            outcome,
            playerVersion or '-1',
            queueVersion or '-1',
            presenceVersion or '-1'
        }
        """;

    // KEYS: player, queue, presence. Returns JSON/version pairs from one Lua read.
    public const string ReadSnapshot = """
        local result = {}
        for index = 1, 3 do
            local keyType = redis.call('TYPE', KEYS[index]).ok
            if keyType == 'none' then
                table.insert(result, false)
                table.insert(result, false)
            elseif keyType ~= 'hash' then
                table.insert(result, '__corrupt')
                table.insert(result, false)
            else
                table.insert(result, redis.call('HGET', KEYS[index], 'json') or '__corrupt')
                table.insert(result, redis.call('HGET', KEYS[index], 'version') or false)
            end
        end
        return result
        """;

    // KEYS: presence state, connection lease.
    // ARGV: connection id, lease TTL ms.
    public const string RefreshLease = """
        local keyType = redis.call('TYPE', KEYS[1]).ok
        if keyType == 'none' then
            return 'missing'
        end

        if keyType ~= 'hash' then
            return 'corrupt'
        end

        local json = redis.call('HGET', KEYS[1], 'json')
        local version = redis.call('HGET', KEYS[1], 'version')
        if not json or not version then
            return 'corrupt'
        end

        local ok, presence = pcall(cjson.decode, json)
        if not ok or type(presence) ~= 'table' or type(presence.devices) ~= 'table' then
            return 'corrupt'
        end

        local connectionId = ARGV[1]
        local found = false
        for _, device in ipairs(presence.devices) do
            if type(device) ~= 'table' or type(device.connections) ~= 'table' then
                return 'corrupt'
            end

            for _, connection in ipairs(device.connections) do
                if type(connection) ~= 'table' or
                    type(connection.connectionId) ~= 'string' then
                    return 'corrupt'
                end

                if connection.connectionId == connectionId then
                    found = true
                    break
                end
            end

            if found then
                break
            end
        end

        if not found then
            return 'not-found'
        end

        redis.call('SET', KEYS[2], '1', 'PX', tonumber(ARGV[2]))
        return 'applied'
        """;
}
