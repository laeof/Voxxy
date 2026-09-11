# T001-03 — Redis persistence policy

## State representation

Connect v2 does not read or convert legacy Connect keys. Each authoritative state
uses an independent Redis hash:

```text
connect:v2:player:{userId}
connect:v2:queue:{userId}
connect:v2:presence:{userId}
```

The braces are Redis Cluster hash tags. All keys for one user therefore share a
slot and can participate in the same Lua commit.

Each state hash contains:

```text
json     Explicit persistence-model JSON
version  Base-10 Int64 version
```

The separate version field lets Lua compare the complete `long` range as a
string, without IEEE-754 precision loss. Reads reject missing fields, malformed
JSON, invalid Domain invariants, and disagreement between the hash version and
the JSON model version.

Missing state is returned as deterministic version-zero Domain state. Reads do
not write it. The first successful mutation uses expected version zero and
creates the state atomically.

## Atomicity and deduplication

One shared Lua commit script handles isolated and two-state commits. It:

1. checks the per-command deduplication record;
2. compares every expected state version;
3. writes all changed states or none;
4. records the trusted Application outcome;
5. creates an optional connection lease;
6. refreshes TTL for existing session states.

Deduplication keys are independent so continuous traffic cannot keep every old
command alive:

```text
connect:v2:command:{userId}:{commandId}
```

The record contains command type plus payload fingerprint, original outcome,
and resulting versions. Reuse with the same fingerprint returns `Duplicate`;
reuse with a different type or fingerprint returns `CommandCollision`.

Snapshot reads use one Lua invocation to read Player, Queue, and Presence JSON
and versions without observing a cross-state commit halfway through.

## Connection leases

Lease freshness is not part of authoritative Presence JSON:

```text
connect:v2:lease:{userId}:{base64url(connectionId)}
```

Registration can create the lease in the same atomic commit as Presence.
Heartbeat checks connection ownership in the current Presence JSON inside Lua
and refreshes only the lease TTL. It does not compare or rewrite the Presence
version.

Cleanup reads Presence, checks which connection lease keys are missing, applies
those explicit IDs through `ConnectStateCoordinator`, and commits Presence
alone or Player plus Presence atomically. No automatic device fallback occurs.
A periodic cleanup worker is outside T001-03.

## TTL policy

Defaults:

```text
StateTtl                 7 days
CommandDeduplicationTtl  24 hours
ConnectionLeaseTtl       45 seconds
```

All values are configurable under `Connect:Redis`. State and command TTLs must
be at least one minute, lease TTL must be at least five seconds, and lease TTL
must be shorter than state TTL.

Every mutation refreshes all existing Player, Queue, and Presence keys for the
session to the same state TTL. Missing keys remain missing and use deterministic
initialization. Each command has its own TTL, preventing unbounded deduplication
growth. Heartbeat refreshes only its lease.

If all session state expires, the next read returns deterministic empty state.
Partial expiry is also deterministic: missing state is version zero, while
remaining states retain their versions. Coordinated TTL refresh during every
mutation minimizes partial expiry without introducing a monolithic snapshot or
shared version.
