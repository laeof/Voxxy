# Task-001 — Refactor Connect module synchronization

## Summary

**Overall complexity:** large.

The task spans the `Voxxy` backend and `Voxxy.Web` frontend and changes a live
SignalR contract. It should not be implemented as one change. The safest path is
to establish the shared state/command contract first, harden backend persistence
and device lifecycle second, migrate the frontend third, and finish with
multi-client integration tests.

The existing Connect module should be evolved, not replaced.

## Current implementation

### Backend

The Connect module currently contains:

- `Connect.Domain`: `PlayerState`, `Device`, `DeviceItem`, `QueuePlayback`, and
  `QueueTrack`.
- `Connect.Application`: player, device, and queue services plus Redis repository
  abstractions.
- `Connect.Infrastructure`: JSON Redis repositories with seven-day key expiry.
- `Connect.Presentation`: authenticated `PlayerHub`, SignalR groups, client events,
  and temporary test endpoints.
- `Connect.Shared`: play, position, and device transport models.
- `Connect.Contracts`: present but currently empty.

State is keyed by user:

- `player_session:{userId}`;
- `queue_playback:{userId}`;
- `device:{userId}`.

The hub broadcasts player and device events to user-specific groups. Queue
registration currently responds only to the caller, while later queue mutations
are broadcast to the queue group.

### Frontend

The relevant frontend areas are:

- `PlayerHubService`: SignalR connection, registration, commands, and incoming
  events.
- `MediaPlayerStateService`: local player and queue `BehaviorSubject` state.
- `MediaPlayerSyncService`: mapping between server state, local state, track
  loading, and outgoing commands.
- `MediaPlayerEngineService`: owns the real `HTMLAudioElement`.
- `DeviceService`: persistent browser device ID and device selection state.
- player bar, device picker, play buttons, and track-list components.

SignalR handlers are registered before `connection.start()`, and reconnect calls
registration again. The persistent device ID is stored in local storage, but
active-device comparisons and selection currently use SignalR connection IDs.

## Main gaps and risks

1. `PlayerState.ActiveDeviceId` is documented and used as a SignalR connection ID,
   not a persistent device ID.
2. Selecting a device sends its connection ID. A reconnect therefore changes the
   identity of the selected device.
3. Multiple tabs with the same persistent device ID overwrite one
   `DeviceItem.ConnectionId`; the model cannot represent all live connections.
4. Disconnect handling can clear or reassign the active device based on stale
   connection state.
5. Redis changes use non-atomic read-modify-write of complete JSON values. Concurrent
   commands can overwrite each other.
6. No command ID, state version, or server ordering mechanism exists. Client
   timestamps are accepted directly.
7. Missing Redis values are handled inconsistently and can cause null dereferences.
8. Player, device, and queue state are separate keys without a consistency boundary.
9. Backend restart removes SignalR groups while Redis retains state; clients must
   rebuild presence and group membership.
10. Queue mutation methods exist on the hub, but the frontend currently does not
    apply `QueuePlaybackChanged`.
11. Frontend queue state contains hydrated `Track` objects while the server queue
    contains track IDs; hydration and ordering are not defined.
12. Server state application calls the same local setters used by user actions.
    This makes feedback loops easy to introduce as audio event handling grows.
13. Position is updated by a local interval and audio `timeupdate` events, while
    remote position changes also update the same state.
14. Track hydration creates an inner subscription without lifecycle cancellation
    and can apply an obsolete response after a newer state arrives.
15. The audio engine may dereference a missing current track when a device becomes
    active.
16. Volume, position, repeat, shuffle, previous/next, and queue responsibilities are
    split inconsistently between local state and server state.
17. Temporary Connect test endpoints use hard-coded user data and group names that
    do not match the hub groups.
18. There are no focused backend unit tests, Redis concurrency tests, SignalR
    integration tests, or frontend player synchronization tests.

## Dependency graph

```text
T001-01 Contract and invariants
├── T001-02 Backend state model
│   ├── T001-03 Atomic Redis persistence
│   └── T001-04 Device presence lifecycle
├── T001-05 Backend player commands
├── T001-06 Backend queue commands
└── T001-07 SignalR protocol and orchestration
    ├── T001-08 Frontend hub adapter
    ├── T001-09 Frontend state application
    │   ├── T001-10 Active-device audio engine
    │   └── T001-11 Queue synchronization
    └── T001-12 Integration and multi-client verification
```

`T001-03` and `T001-04` build on `T001-02`. After the contract is stable,
backend player and queue work can proceed in parallel. Frontend state and audio
work can begin against agreed contract fixtures while backend work continues, but
integration waits for `T001-07`.

## Implementation tasks

### T001-01 — Define synchronization contract and invariants

- **Goal:** specify the authoritative state, stable identities, command semantics,
  event envelopes, ordering, idempotency, and reconnect behavior before code
  changes.
- **Affected repository:** `Voxxy` and `Voxxy.Web`.
- **Affected areas:** `Connect.Contracts`, `Connect.Shared`, hub/client contracts,
  frontend player DTOs and SignalR constants.
- **Dependencies:** none; foundational.
- **Complexity:** medium.
- **Parallel:** no. It blocks contract-dependent backend and frontend work.
- **Acceptance criteria:**
  - Active device is identified by persistent `deviceId`; connection IDs represent
    presence only.
  - Multiple connections per device are defined explicitly.
  - One authoritative snapshot includes or references player, queue, active device,
    state version, and server update time.
  - Command identity/version rules define duplicate and stale-command handling.
  - Rules for play, pause, seek, volume, select track/device, add/remove queue item,
    disconnect, reconnect, and backend restart are documented.
  - Event names and payload compatibility/migration strategy are agreed.
- **Required verification:** contract review using scenarios for two users, two
  devices, two tabs, reconnect, duplicate command, and stale command.
- **Main risks:** choosing connection ID as identity again; breaking the frontend
  and backend simultaneously; over-designing distributed consistency.

### T001-02 — Refine backend domain state and invariants

- **Goal:** represent the agreed stable device identity, connection presence,
  authoritative player state, queue linkage, and versioning in Connect domain
  models.
- **Affected repository:** `Voxxy`.
- **Affected projects:** `Connect.Domain`, `Connect.Application`,
  `Connect.Contracts`/`Connect.Shared`.
- **Dependencies:** T001-01.
- **Complexity:** large.
- **Parallel:** limited. Player and device model work can be developed separately
  after the contract is fixed, but must be integrated together.
- **Acceptance criteria:**
  - `ActiveDeviceId` means persistent device ID, never connection ID.
  - A device can own multiple live SignalR connections.
  - Domain operations validate volume, position, queue indices, and missing state.
  - State has an explicit monotonic version or equivalent ordering token.
  - Initialization after missing/expired Redis state is deterministic.
  - Domain/Application contracts contain no Redis or SignalR types.
- **Required verification:** focused unit tests for state transitions, invalid
  operations, duplicate commands, and multiple connections.
- **Main risks:** incompatible serialized Redis data; ambiguous migration of old
  keys; placing transport concerns in Domain.

### T001-03 — Make Redis state updates concurrency-safe

- **Goal:** prevent lost updates and define expiry/recovery behavior for player,
  device, and queue state.
- **Affected repository:** `Voxxy`.
- **Affected projects:** `Connect.Application`, `Connect.Infrastructure`.
- **Dependencies:** T001-02.
- **Complexity:** large.
- **Parallel:** yes, with T001-04 after the new state shape is fixed.
- **Acceptance criteria:**
  - Concurrent commands cannot silently overwrite a newer state.
  - Writes use an explicit optimistic version check, transaction, Lua script, or
    another atomic Redis mechanism.
  - Duplicate command handling is atomic with state mutation.
  - Missing, expired, malformed, and legacy state have defined outcomes.
  - TTL refresh and cleanup rules are consistent across related keys.
  - Repository abstractions express atomic operations without leaking
    `StackExchange.Redis`.
- **Required verification:** repository integration tests against Redis covering
  competing updates, duplicates, expiry, and backend restart.
- **Main risks:** partial consistency across three keys; retry storms; losing
  existing Redis state during schema migration.

### T001-04 — Rework device registration, presence, reconnect, and failover

- **Goal:** track stable devices and transient connections correctly and safely
  choose a new active device after disconnect.
- **Affected repository:** `Voxxy`.
- **Affected projects:** `Connect.Domain`, `Connect.Application`,
  `Connect.Presentation`.
- **Dependencies:** T001-02; uses T001-03 atomic operations for final integration.
- **Complexity:** large.
- **Parallel:** yes, with T001-03, T001-05, and T001-06 after shared contracts are
  stable.
- **Acceptance criteria:**
  - Registering a tab adds a connection to a stable device instead of replacing
    device identity.
  - Reconnect removes stale connection IDs and preserves the active device when
    that physical/logical device remains online.
  - Disconnecting one tab does not mark another tab of the same device offline.
  - Active-device disconnect uses a deterministic failover policy or explicitly
    leaves no active device.
  - Backend restart and repeated registration are idempotent.
  - Only connections belonging to the authenticated user can be selected.
- **Required verification:** hub/service tests for two tabs on one device, two
  devices, reconnect with a new connection ID, stale presence, and active-device
  loss.
- **Main risks:** `OnDisconnectedAsync` is not guaranteed to run; racing reconnect
  and disconnect callbacks; selecting spoofed connection/device IDs.

### T001-05 — Harden authoritative backend player commands

- **Goal:** validate and serialize play, pause, seek, volume, track selection, and
  active-device selection against server state.
- **Affected repository:** `Voxxy`.
- **Affected projects:** `Connect.Domain`, `Connect.Application`.
- **Dependencies:** T001-01 and T001-02; final concurrency behavior depends on
  T001-03.
- **Complexity:** large.
- **Parallel:** yes, with T001-04 and T001-06.
- **Acceptance criteria:**
  - Any authorized device may issue player commands.
  - Commands update the shared snapshot and increment its version atomically.
  - Stale/duplicate commands are rejected or return the current state
    idempotently.
  - Volume and position are validated and normalized.
  - Track selection, play state, position, and queue reference remain coherent.
  - Server time/order is authoritative; client timestamps are not blindly trusted.
- **Required verification:** domain/service tests for all transitions, boundaries,
  duplicates, stale versions, and concurrent commands.
- **Main risks:** visible jumps from timestamp reconciliation; incompatible command
  semantics; accepting nonexistent track/queue IDs.

### T001-06 — Harden authoritative backend queue commands

- **Goal:** make add, remove, reorder, shuffle, repeat, and current-track behavior
  consistent with the shared player state.
- **Affected repository:** `Voxxy`.
- **Affected projects:** `Connect.Domain`, `Connect.Application`.
- **Dependencies:** T001-01 and T001-02; final concurrency behavior depends on
  T001-03.
- **Complexity:** large.
- **Parallel:** yes, with T001-04 and T001-05.
- **Acceptance criteria:**
  - Queue commands are validated, ordered, versioned, and idempotent.
  - Duplicate track semantics are explicit; queue entries have stable identity if
    track IDs alone are insufficient.
  - Removing or moving the current item has deterministic behavior.
  - Shuffle/unshuffle preserves an intentional canonical order.
  - Repeat and next/previous behavior are server-defined.
  - Player track/position and queue current index cannot contradict each other.
- **Required verification:** domain/service tests for empty queue, duplicates,
  current-item removal, reorder bounds, shuffle/unshuffle, repeat, and concurrent
  edits.
- **Main risks:** using track ID as queue-entry identity; conflicting player and
  queue writes; unstable shuffle restoration.

### T001-07 — Consolidate SignalR command and event orchestration

- **Goal:** expose the authoritative backend behavior through a coherent,
  authenticated SignalR API and broadcast complete ordered updates.
- **Affected repository:** `Voxxy`.
- **Affected projects:** `Connect.Presentation`, `Connect.Contracts`,
  `Web.Api`.
- **Dependencies:** T001-03, T001-04, T001-05, T001-06.
- **Complexity:** large.
- **Parallel:** partially; hub method wiring can be divided by player/queue after
  the contract is fixed.
- **Acceptance criteria:**
  - Hub methods delegate to Application services and do not implement domain or
    Redis logic.
  - Every accepted command results in an ordered versioned state event to all
    current user connections, including the caller.
  - Initial registration returns a coherent snapshot rather than independent
    contradictory partial events.
  - Caller identity comes only from the authenticated hub context.
  - Reconnect registration is idempotent.
  - Temporary hard-coded test endpoints are removed or isolated from production
    mapping.
  - Transitional event compatibility follows T001-01.
- **Required verification:** in-process SignalR integration tests with multiple
  authenticated clients and an integration Redis instance.
- **Main risks:** event reordering; partial broadcasts after persistence succeeds;
  leaking one user's state to another.

### T001-08 — Refactor frontend SignalR adapter and contracts

- **Goal:** isolate transport DTOs and expose typed connection lifecycle,
  authoritative snapshots, and command results to frontend state coordination.
- **Affected repository:** `Voxxy.Web`.
- **Affected areas:** `PlayerHubService`, SignalR constants/policy, DTOs, startup
  initializer, device service.
- **Dependencies:** T001-01; integration depends on T001-07.
- **Complexity:** medium.
- **Parallel:** yes, against agreed contract fixtures while T001-03 through
  T001-07 are implemented.
- **Acceptance criteria:**
  - Handlers are registered before connection start.
  - Reconnect repeats idempotent registration using persistent device ID.
  - Connection ID is exposed only as transport diagnostics/presence, not device
    identity.
  - Queue and full-state events are typed and no longer logged without being
    applied.
  - Connection start/stop/reconnect errors have explicit observable state.
  - Duplicate/out-of-order snapshots can be filtered by version.
- **Required verification:** unit tests with a mocked HubConnection for initial
  connection, reconnect, duplicate events, ordering, and disconnect.
- **Main risks:** tight coupling to `HubConnection`; registering handlers twice;
  losing events during reconnect.

### T001-09 — Introduce one-way frontend server-state application

- **Goal:** separate authoritative server-state application from user intents and
  eliminate synchronization feedback loops.
- **Affected repository:** `Voxxy.Web`.
- **Affected areas:** `MediaPlayerStateService`, `MediaPlayerSyncService`, player
  DTOs, track hydration.
- **Dependencies:** T001-01 and T001-08.
- **Complexity:** large.
- **Parallel:** yes, with backend implementation using versioned fixture snapshots.
- **Acceptance criteria:**
  - Incoming snapshots are reduced into local UI state without emitting outgoing
    commands.
  - User intent methods are the only path that sends SignalR commands.
  - Older or duplicate state versions are ignored.
  - Track hydration is cancellable and cannot apply a stale response.
  - Null current track and empty queue are valid handled states.
  - Position display derives from authoritative position/time without continuously
    mutating command state.
- **Required verification:** service tests for server apply versus user intent,
  stale hydration, duplicate state, null track, and position calculation.
- **Main risks:** accidental local setter subscriptions sending commands; position
  drift; API hydration racing SignalR updates.

### T001-10 — Enforce active-device-only audio playback

- **Goal:** ensure only the persistent active device controls the real audio element
  while all clients show synchronized UI state.
- **Affected repository:** `Voxxy.Web`.
- **Affected areas:** `MediaPlayerEngineService`, `MediaPlayerSyncService`,
  `DeviceService`, player bar.
- **Dependencies:** T001-04, T001-08, T001-09.
- **Complexity:** large.
- **Parallel:** no with T001-09 in the same files; it may run in parallel with
  T001-11 after state interfaces stabilize.
- **Acceptance criteria:**
  - Active status compares persistent device IDs.
  - Inactive devices never call audible `play()` and promptly pause on handoff.
  - Becoming active loads the current track, seeks, applies volume, and follows
    play/pause safely.
  - Programmatic server application does not send play/pause/seek/volume back.
  - Audio `ended`, `timeupdate`, autoplay rejection, and missing current track are
    handled explicitly.
  - Safari and missing browser APIs have safe fallbacks.
- **Required verification:** audio adapter unit tests plus manual two-tab/two-device
  checks for handoff, reconnect, background tabs, and autoplay restrictions.
- **Main risks:** both devices playing during handoff; browser autoplay policy;
  feedback from audio DOM events.

### T001-11 — Implement frontend shared queue synchronization

- **Goal:** render and manipulate the authoritative shared queue from every client.
- **Affected repository:** `Voxxy.Web`.
- **Affected areas:** `PlayerHubService`, `MediaPlayerStateService`,
  `MediaPlayerSyncService`, track service, track lists, player controls.
- **Dependencies:** T001-06, T001-08, T001-09.
- **Complexity:** large.
- **Parallel:** yes, with T001-10 after frontend state contracts stabilize.
- **Acceptance criteria:**
  - Initial and changed queues are applied on all clients.
  - Track-ID hydration preserves server queue order and duplicate queue entries.
  - Add, remove, reorder, next/previous, repeat, and shuffle send intents rather
    than mutate authoritative local state optimistically without reconciliation.
  - Current queue entry and player track stay coherent.
  - Stale hydration and stale queue versions are ignored.
- **Required verification:** service/component tests for initial queue, add/remove,
  duplicates, reorder, repeat, shuffle, next/previous, and concurrent remote edits.
- **Main risks:** batch track API returning a different order; track ID not uniquely
  identifying duplicate entries; conflicts with current local queue behavior.

### T001-12 — Add integration, resilience, and multi-client coverage

- **Goal:** verify the complete synchronization behavior and make regressions
  reproducible.
- **Affected repository:** `Voxxy` and `Voxxy.Web`.
- **Affected projects/areas:** new Connect test projects or test areas,
  ArchitectureTests if boundaries change, Angular service/component specs, Docker
  test infrastructure where needed.
- **Dependencies:** T001-07, T001-08, T001-09, T001-10, T001-11.
- **Complexity:** large.
- **Parallel:** backend and frontend test suites can be authored in parallel;
  end-to-end scenarios require both.
- **Acceptance criteria:**
  - Automated tests cover two users, two devices, multiple tabs, concurrent
    commands, duplicates, reconnect, stale Redis state, backend restart, and
    active-device disconnect.
  - Tests prove only one active device drives audio.
  - Tests prove all clients converge on the same player and queue versions.
  - Tests prove no feedback loop repeats commands.
  - Existing backend architecture tests and frontend tests are accounted for
    separately from new regressions.
- **Required verification:**
  - `dotnet build Voxxy.slnx`;
  - focused backend unit/integration tests;
  - `dotnet test Voxxy.slnx`;
  - `npm run build`;
  - `npm test -- --watch=false`;
  - scripted or manual multi-tab/multi-device scenario matrix.
- **Main risks:** flaky timing-based SignalR tests; browser audio differences;
  tests masking Redis race conditions with sequential execution.

## Parallel execution plan

| Work stream | Tasks | When it can start |
|---|---|---|
| Contract | T001-01 | Immediately |
| Backend state | T001-02 | After T001-01 |
| Backend persistence/presence | T001-03, T001-04 | In parallel after T001-02 |
| Backend behavior | T001-05, T001-06 | In parallel after T001-02 |
| Frontend transport/state | T001-08, then T001-09 | After T001-01 using fixtures |
| Backend protocol | T001-07 | After backend persistence and behavior |
| Frontend playback/queue | T001-10, T001-11 | In parallel after T001-09 and relevant backend contracts |
| Tests | backend and frontend parts of T001-12 | Alongside each completed area; full integration last |

Avoid parallel edits to the same hotspot files:

- backend `PlayerHub`, `PlayerState`, and Redis repository abstractions;
- frontend `MediaPlayerStateService`, `MediaPlayerSyncService`, and
  `PlayerHubService`.

## Roadmap

### Phase 0 — Baseline

1. Record current build/test results without fixing unrelated failures.
2. Create a scenario matrix and capture the existing SignalR payloads and Redis
   JSON shapes.
3. Complete T001-01.

**Milestone:** stable reviewed contract and migration strategy; no runtime behavior
changed.

### Phase 1 — Backend foundations

1. Complete T001-02.
2. Implement T001-03 and T001-04 in parallel.
3. Keep compatibility readers or explicitly clear development Redis data according
   to the migration decision.

**Milestone:** stable device identity, connection presence, versioned state, and
atomic persistence with the project buildable.

### Phase 2 — Backend behavior and protocol

1. Implement T001-05 and T001-06 in parallel.
2. Complete T001-07 after both command families and device lifecycle are ready.
3. Run backend unit, Redis integration, architecture, and SignalR integration tests.

**Milestone:** backend is the authoritative source of truth and supports old or
new frontend compatibility as decided in T001-01.

### Phase 3 — Frontend convergence

1. T001-08 may be prepared earlier against contract fixtures, then integrated with
   T001-07.
2. Complete T001-09.
3. Implement T001-10 and T001-11 in parallel where file ownership permits.
4. Preserve a buildable frontend after each task; avoid a partial migration where
   old setters and new intents both send commands.

**Milestone:** all clients display authoritative state, only the active persistent
device plays audio, and queue state converges.

### Phase 4 — Integration and rollout

1. Complete T001-12.
2. Run the full two-user/two-device/multi-tab matrix.
3. Test Redis loss/expiry, backend restart, reconnect, active-device disconnect,
   duplicate commands, and rapid competing commands.
4. Remove transitional contracts only after both repositories use the new protocol.

**Milestone:** automated and manual evidence covers convergence, single-device
audio, idempotency, resilience, and absence of feedback loops.

## Recommended delivery order

1. T001-01 — contract and invariants.
2. T001-02 — backend domain state.
3. T001-03 and T001-04 — atomic persistence and presence in parallel.
4. T001-05 and T001-06 — player and queue commands in parallel.
5. T001-08 — frontend adapter can proceed against fixtures during backend work.
6. T001-07 — integrated backend SignalR protocol.
7. T001-09 — one-way frontend state application.
8. T001-10 and T001-11 — active audio and queue synchronization in parallel.
9. T001-12 — final resilience and multi-client integration.

This order keeps contract changes explicit, allows backend and frontend work to
overlap safely, and avoids depending on incomplete local-state behavior for final
integration.
