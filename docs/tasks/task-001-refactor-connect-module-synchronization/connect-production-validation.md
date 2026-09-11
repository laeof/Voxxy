# T001-11 — Connect production validation

## Status

This task establishes a reproducible validation system and initial operating limits. The SLO
values below are rollout targets, not confirmed production capacity. A full 30-minute run on
production-equivalent hardware is required before rollout.

## Load runner

`tools/Connect.LoadRunner` is a native .NET SignalR client using WebSockets with negotiation
disabled. Each entry in the token file represents a distinct authenticated user. Do not reuse one
token for all clients.

Token file shape:

```json
[
  { "userId": "00000000-0000-0000-0000-000000000001", "accessToken": "<jwt>" }
]
```

Run the scaled CI profile:

```bash
dotnet run --project tools/Connect.LoadRunner -- \
  --profile=ci \
  --url=http://localhost:8088/api/hubs/connect \
  --tokens=connect-load-tokens.json
```

Production profiles:

```bash
--profile=connection-ramp
--profile=steady
--profile=burst
--profile=reconnect-storm
```

The runner records connect, snapshot, registration, heartbeat and mutation latency and writes:

```text
artifacts/connect-load/connect-load-results.json
artifacts/connect-load/connect-load-summary.md
```

Raw traces and token files must not be committed.

## Topology

```bash
docker compose -f docker-compose.connect-load.yml up -d
docker compose -f docker-compose.connect-load.yml --profile load run --rm load-generator
```

The topology contains NGINX without affinity, two backend instances, Redis, PostgreSQL,
MeiliSearch, Azurite, Toxiproxy and the load generator. Backend containers are limited to 2 CPU /
1 GiB and Redis to 1 CPU / 512 MiB. Local results remain dependent on Docker Desktop and host
hardware.

The intended identity topology is:

- 70% one user / one device / one connection;
- 20% one device with two tabs;
- 10% three to five devices for one user.

## Initial SLO gates

These are unconfirmed rollout gates:

| Measurement | Target |
|---|---:|
| GetSnapshot p95 | < 300 ms |
| mutation acknowledgement p95 | < 250 ms |
| cross-instance event delivery p95 | < 500 ms |
| heartbeat p95 | < 200 ms |
| command error rate | < 1% |
| unexpected disconnect rate | < 0.5% |
| reconnect recovery p95 | < 10 s |

Production capacity remains **not confirmed** until connection-ramp and 30-minute steady runs
complete on representative infrastructure with CPU, working set, GC heap, allocation rate,
thread-pool queue and Redis connection/latency telemetry attached.

## Enforced limits

- Connect Hub request message: 64 KiB maximum.
- Queue: 1,000 items maximum.
- Device name: trimmed, 1–100 characters, no control characters.
- Heartbeat: token bucket capacity 1, refill one token per 5 seconds.
- Position and volume: burst 10, sustained 4/second.
- Other mutations: burst 20, sustained 5/second.
- Registration and disconnect are exempt so reconnect and cleanup remain possible.
- Frontend heartbeat: 15 seconds ±10%.
- Reconnect delays: randomized 0–2, 2–5, 5–10, then 10–30 seconds.
- WebSocket compression remains disabled.

These rate limits are safe starting points, not final capacity claims.

## Command and contention profiles

The default mix is 60% heartbeat, 12% position, 8% volume, 6% play/pause and the remainder
snapshots. Queue commands require seeded valid queue IDs and belong in a scenario-specific input
set.

Additional required nightly profiles:

- CAS contention: 10 connections of one user, 100 volume commands;
- coordinated contention: simultaneous Next/Previous and Select;
- deduplication: one command ID and payload sent repeatedly;
- collision: one command ID with differing payloads;
- cleanup scale: 10,000 session-index members and thousands of expired leases;
- backend failure and reconnect storm;
- persistence Redis latency/outage;
- backplane-only outage followed by recovery.

Acceptance requires one mutation/broadcast for duplicates, `CommandCollision` without a second
mutation, monotonic versions, and no lost update beyond documented last-writer semantics.

## Chaos matrix

| Fault | Expected safety and recovery |
|---|---|
| Redis latency | Commands slow/fail safely; no optimistic state |
| Persistence Redis outage | Readiness unhealthy; state retained client-side; snapshot after recovery |
| Backplane outage | Commit may succeed; delivery-unconfirmed triggers snapshot |
| Redis restart | Official clients reconnect; readiness recovers without app restart |
| Backend kill | WebSocket reconnects through other instance; stable DeviceId/new connection ID |
| Proxy restart | Local engine pauses; heartbeat stops; snapshot/register after reconnect |
| Packet loss/DNS fault | Bounded reconnect jitter; no tight loop |
| Kill after commit | Same command ID deduplicates; snapshot exposes committed state |

Use Toxiproxy or Docker network controls only against the load topology, never production Redis.

## Session-index diagnostics

Automatic repair was intentionally not added. During maintenance:

1. incrementally `SCAN` `connect:v2:presence:*`;
2. compare each user with the session index;
3. report missing index membership;
4. confirm an indexed user's Presence key is absent before removing a stale member;
5. record counts and rerun after cleanup.

Do not mutate Presence JSON or infer device ownership from lease keys. This procedure is
diagnostic and any repair must be explicitly approved.

## Correctness validator

`tests/Connect.ValidationTests` checks:

- non-negative versions;
- current queue item belongs to the queue;
- unique queue item IDs;
- active device exists or is null;
- audio owner references an online connection or is null;
- transport requests cannot submit server-owned identity/time/ownership fields;
- serialized queue and snapshot sizes at 100 and 1,000 items.

At the current 1,000-item limit, the snapshot budget is 250 KiB. The 64 KiB limit applies to
client-to-server requests, not responses. If snapshots approach proxy/client limits, the next
design step is pagination or queue delta events, not a larger command limit.

## Security

- Hub identity comes exclusively from authenticated claims.
- Requests contain no user ID, connection ID, server time or audio-owner field.
- Browser Origin must match configured `Cors:AllowedOrigins`.
- JWT, Redis credentials, full snapshots/queues and fingerprints are not metric tags.
- Allowed metric labels are command type, status and event type only.
- Production cookies and trusted-proxy forwarded-header configuration require deployment review;
  development's non-secure cookie settings are not a production baseline.

## Metrics and profiling

Backend meters:

```text
connect.connections.current
connect.connections.started
connect.connections.closed
connect.commands.total
connect.commands.duration
connect.commands.conflicts
connect.commands.duplicates
connect.commands.failures
connect.broadcast.duration
connect.broadcast.failures
connect.snapshots.duration
connect.reconnect.registrations
connect.rate_limited
```

Before rollout, capture `dotnet-counters` for baseline, 100 connections, 500 connections and a
command burst. Record CPU, working set, GC heap, allocation rate, thread-pool queue and exception
rate alongside Redis `INFO clients/memory/stats`.

Frontend telemetry aggregates transport/recovery event counts and strips identifiers, tokens,
snapshots and queue contents.

## Alerts and rollback

Alert on readiness failures, broadcast failures, delivery-unconfirmed rate, CAS conflicts,
command p95/p99, snapshot recovery failures, reconnect storms, cleanup cycle duration, expired
connection backlog, Redis latency and WebSocket drops.

Rollback or halt rollout when:

- any correctness invariant fails;
- unexpected error rate is at least 1%;
- reconnect recovery p95 is at least 10 seconds;
- readiness flaps after Redis recovery;
- memory grows without stabilizing during steady state;
- queue/snapshot payload exceeds the documented budget;
- cross-user event delivery is observed.

## Known limitations

- No transactional outbox; uncertain delivery uses snapshot recovery.
- The load topology uses a single Redis deployment.
- Session-index reconciliation is diagnostic-only.
- Full capacity, resource and 30-minute latency baselines require a separately executed nightly
  run with valid per-user credentials.
