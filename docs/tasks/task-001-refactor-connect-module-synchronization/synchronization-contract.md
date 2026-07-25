# T001-01 — Connect synchronization contract and invariants

Status: proposed contract for approval, amended to use a coordinated breaking v2
rollout with no backward compatibility. No production implementation is included.

## 1. Current implementation map

### Backend

| Responsibility | Current implementation |
|---|---|
| Player state | `src/Connect/Domain/Player/PlayerState.cs`: `PlayerState` stores `TrackId`, `QueueId`, `ActiveDeviceId`, play/pause, position, volume, and client-supplied `UpdatedAt`. `ActiveDeviceId` currently contains a SignalR connection ID. |
| Devices | `src/Connect/Domain/Devices/Device.cs`: `Device` contains one `DeviceItem` per persistent device ID; each item has only one nullable `ConnectionId`. |
| Queue | `src/Connect/Domain/Queue/QueuePlayback.cs` and `QueueTrack.cs`: queue items contain only `TrackId`; reorder/removal therefore identify every occurrence of the same track together. |
| Redis access | `src/Connect/Infrastructure/Redis/RedisCacheRepository.cs`: complete values are serialized as JSON under seven-day keys. Operations are independent `GET` and `SET`, without CAS, transactions, revision checks, or command deduplication. |
| Redis keys | `TableConstants`: `player_session:{userId}`, `queue_playback:{userId}`, and `device:{userId}`. |
| Player use cases | `PlayerSessionService`: `GetStateAsync`, `CreateSessionAsync`, `PlayAsync`, `PauseAsync`, `ChangePositionAsync`, `ConnectToDeviceAsync`, and `ChangeVolumeAsync`. |
| Device use cases | `DeviceService`: get/add/remove devices, attach/detach one connection, and return online devices. |
| Queue use cases | `QueuePlaybackService`: create/get, add/remove/reorder, shuffle/unshuffle, and repeat. |
| Hub | `src/Connect/Presentation/Hubs/ConnectHub.cs`: authenticated `PlayerHub` joins three groups in `OnConnectedAsync` and mutates state through Application services. |
| Client interface | `IPlayerClient`: `PlayerStateChanged`, `VolumeChanged`, `PositionChanged`, `QueuePlaybackChanged`, `ActiveDeviceChanged`, and `DeviceListChanged`. Payloads expose Domain models directly. |
| Registration | `RegisterPlayer`, `RegisterQueue`, and `RegisterDevice` are three separate calls. Player registration broadcasts to the group, queue registration replies to the caller, and device registration broadcasts device/active-device events. |
| Reconnect | SignalR creates a new connection and calls normal registration from the frontend. There is no server-side resume token or explicit replacement of an old connection. |
| Disconnect | `OnDisconnectedAsync` clears a matching `ConnectionId`, selects the first remaining online connection if the disconnected connection was active, and broadcasts devices. |
| Active selection | `ConnectToDevice(string connectionId)` stores and broadcasts the supplied connection ID without proving it belongs to the current user. |
| Player commands | `Play`, `Pause`, `ChangePosition`, and `ChangeVolume` update Redis and broadcast either the complete player or a partial event. |
| Queue commands | `AddTracksToQueue`, `RemoveTrackFromQueue`, `ChangeTrackIndexInQueue`, `ShuffleQueue`, `UnshuffleQueue`, and `ToggleRepeatQueue`. |
| Temporary endpoints | `Presentation/Endpoints/TestEndpoint.cs` uses a hard-coded user and a group name inconsistent with the hub groups. |

### Frontend

| Responsibility | Current implementation |
|---|---|
| SignalR adapter | `src/app/common/services/player-hub.service.ts`: `PlayerHubService` creates the connection, registers handlers before start, invokes commands, and registers player/queue/device. |
| Reconnect | `PlayerHubService.onreconnected` repeats the three registrations using the persistent device ID from `DeviceService`. |
| Transport state | `src/app/common/entities/PlayerState.ts` and `PlayRequest.ts`; state has no version, command ID, queue item ID, or error/result envelope. |
| Local player state | `MediaPlayerStateService`: independent `BehaviorSubject`s for playing, position, volume, queue, index, current track, repeat, and active status. |
| Coordination | `MediaPlayerSyncService`: subscribes to hub state/position/volume, hydrates tracks, updates local state, calculates displayed position, and sends play/pause/seek/volume commands. |
| Real audio | `MediaPlayerEngineService`: owns one `HTMLAudioElement`, applies local state, hydrates a stream URL, and handles `timeupdate` and `ended`. |
| Persistent device identity | `DeviceService.tryCreateDeviceId()` stores a UUID in local storage and has a fallback when `crypto.randomUUID` is unavailable. |
| Current active comparison | `MediaPlayerSyncService` and device UI compare selection with `PlayerHubService.connectionId`, not the persistent device ID. |
| Queue events | `PlayerHubService` logs `QueuePlaybackChanged` but does not expose or apply it. Local next/previous/repeat behavior remains authoritative on the client. |
| Outgoing commands | `MediaPlayerSyncService` optimistically mutates local state, then invokes the hub. Queue command methods are not exposed by `PlayerHubService`. |
| Track hydration | A batch HTTP request is followed by a nested subscription. An older response can apply after a newer server event. |
| Audio events | `timeupdate` updates local position; `ended` advances the local queue. DOM events currently do not directly send SignalR commands, but the same local state setters are shared by user and server paths. |

## 2. Confirmed problems

1. Persistent device identity and transient connection identity are conflated.
2. One device cannot safely represent multiple tabs/connections.
3. Reconnect changes the current active identity.
4. Active-device selection accepts an unverified connection ID.
5. Disconnect/failover depends on unordered Redis device items and can race reconnect.
6. Complete JSON read-modify-write operations lose concurrent updates.
7. There is no state version, command ID, idempotency record, or authoritative ordering.
8. Client timestamps are accepted as ordering/state timestamps.
9. Three independent registration calls can produce a mixed initial view.
10. Complete and partial player events can arrive out of order and cannot be compared.
11. Queue items have no identity distinct from `trackId`.
12. Player state and queue state can contradict each other.
13. Queue events are not consumed by the frontend.
14. Server-state application and user intent share mutable frontend methods, enabling feedback loops.
15. Position calculation mutates local command state every second.
16. Track hydration is not cancelled when a newer state arrives.
17. A null current track can reach the audio engine.
18. Redis expiry/backend restart behavior is implicit rather than contractual.
19. Domain models are used directly as SignalR contracts.
20. No focused automated coverage exists for these synchronization cases.

## 3. Terminology

- **User:** authenticated owner of one independent Connect session. `userId` is
  taken only from the authenticated hub context.
- **Device:** logical playback target identified by a persistent UUID `deviceId`.
  A browser profile is a device; all its tabs normally share the same `deviceId`.
- **Connection:** one temporary SignalR connection identified by `connectionId`.
- **Presence:** server knowledge of live connections grouped under devices.
- **Active device:** the one logical device allowed to produce audible playback.
- **Authoritative state:** state accepted and versioned by the server.
- **Player state:** play state, position anchor, shared volume, and its version.
- **Queue state:** ordered queue items, current item, shuffle/repeat configuration,
  and its version.
- **Presence state:** devices, live connections, active device, audio owner, and
  its version.
- **Snapshot:** all three authoritative states returned together for recovery.
- **Command:** authenticated request to mutate state.
- **Event:** versioned server notification containing changed authoritative state.
- **Tab/client:** frontend instance owning one connection. It is not a device.

## 4. Authoritative State Ownership

The server owns three separate authoritative states. A field has exactly one
owner; other states and frontend models may derive or reference it but must not
independently redefine it.

| State | Authoritative fields | Not owned here |
|---|---|---|
| **Player** | `isPlaying`, `positionMs`, `positionUpdatedAt`, `volumePercent`, `version` | Queue items/current selection, devices/connections, active device |
| **Queue** | ordered `items`, each `queueItemId` and `trackId`, `currentQueueItemId`, canonical order, shuffle state, repeat mode, `version` | Play/pause, position, volume, device presence |
| **Presence** | registered devices, live connection leases, online status, `activeDeviceId`, `audioOwnerConnectionId`, `version` | Player transport state and queue contents |

Consequences:

- The current track is derived from Queue's `currentQueueItemId`; Player does not
  own a second independently mutable `trackId`.
- Selecting a queue item changes Queue and resets Player position atomically.
- Selecting the active device changes Presence. Disconnecting the final connection
  of the active device changes Presence and pauses Player atomically.
- Frontend hydrated `Track` objects, display position, loading state, and
  `HTMLAudioElement` state are projections, not additional authoritative states.

## 5. Domain and synchronization invariants

1. For each user, at most one persistent `deviceId` is active.
2. Audible playback is permitted only when the local persistent `deviceId` equals
   authoritative `activeDeviceId`, the local connection equals authoritative
   `audioOwnerConnectionId`, and authoritative `isPlaying` is true.
3. Every authorized online device of the user may issue supported player, queue,
   and active-device commands. Being inactive limits audio output, not control.
4. User identity is derived from authentication, never from a command payload.
5. A user's command and event channel cannot read or mutate another user's state.
6. `deviceId` is stable across reconnect; `connectionId` is unique to one live
   connection and may change.
7. A device has zero or more connections. A device is online when it has at least
   one non-stale connection.
8. At most one live connection of the active device is the audio owner.
9. Server Player, Queue, and Presence states own the fields listed in section 4.
10. The active device's audio-owner connection is responsible only for applying
    state to real audio and reporting explicit user/media intents. Its audio clock
    is not authoritative state by itself.
11. Applying a server event updates local projection/audio only and never invokes
    the corresponding hub command.
12. Each authoritative mutation increments the version of every changed state
    exactly once.
13. Duplicate commands and duplicate events do not produce additional mutation.
14. An event cannot replace a local projection whose corresponding state version
    is newer.
15. Client timestamps are telemetry only. Server versions and server timestamps
    determine order.
16. Queue `currentQueueItemId`, when present, identifies exactly one existing queue
    item; the current track is derived from that item's `trackId`.
17. An empty queue has no current item, no current track, position zero, and is
    paused.
18. Queue item identity is independent of track identity, so one track may occur
    multiple times.
19. Redis absence/expiry produces deterministic initial state, not null failures.
20. A backend restart does not make persisted connections live; presence is rebuilt.
21. No automatic failover may start audible playback on a device the user did not
    explicitly select.

## 6. Recommended state model

Use **separate versioned player, queue, and presence states carried in one common
event/snapshot envelope**.

This is not one Redis JSON snapshot and not one global CAS version.

```csharp
record ConnectSnapshot(
    Guid UserId,
    PlayerStateDto Player,
    QueueStateDto Queue,
    PresenceStateDto Presence,
    DateTimeOffset ServerTime);

record PlayerStateDto(
    bool IsPlaying,
    long PositionMs,
    DateTimeOffset PositionUpdatedAt,
    int VolumePercent,
    long Version);

record QueueStateDto(
    IReadOnlyList<QueueItemDto> Items,
    Guid? CurrentQueueItemId,
    RepeatMode Repeat,
    bool IsShuffled,
    long Version);

record QueueItemDto(
    Guid QueueItemId,
    Guid TrackId,
    long CanonicalOrder);

record PresenceStateDto(
    IReadOnlyList<DevicePresenceDto> Devices,
    Guid? ActiveDeviceId,
    string? AudioOwnerConnectionId,
    long Version);

record DevicePresenceDto(
    Guid DeviceId,
    string Name,
    IReadOnlyList<ConnectionPresenceDto> Connections,
    bool IsOnline);
```

The event envelope may include one or more changed states:

```csharp
record ConnectStateChanged(
    Guid EventId,
    Guid? CausationCommandId,
    DateTimeOffset ServerTime,
    PlayerStateDto? Player,
    QueueStateDto? Queue,
    PresenceStateDto? Presence);
```

Reasons:

- player updates do not contend with routine connection presence updates;
- queue mutations do not invalidate unrelated volume/position commands;
- events can contain player and queue together when one command changes both;
- frontend has one reducer entry point and compares each included state version;
- a complete snapshot is available for registration/reconnect/recovery without
  forcing all Redis writes into one large document.

Cross-state commands such as remove-current, next, previous, or select-track must
atomically change both player and queue state. The persistence strategy is deferred
to T001-03, but the contract requires all-or-nothing visibility.

## 7. Alternative state models and tradeoffs

### A. One snapshot for player, queue, and devices

**Advantages**

- one version and straightforward frontend replacement;
- one Redis CAS operation;
- initial read is naturally coherent.

**Disadvantages**

- frequent position/player updates rewrite queue and presence;
- every reconnect competes with every play/seek/queue mutation;
- large payloads and high conflict/retry rate;
- connection churn can delay playback commands.

**Redis implications:** simple atomic CAS on one key, but hot-key contention and
large serialization cost.

**Conflict frequency:** highest, because all command families share one version.

**Frontend complexity:** lowest.

**Migration risk:** high; three existing JSON documents must become one and legacy
partial writes cannot coexist cleanly.

**Decision:** rejected for normal operation. It couples unrelated high-frequency
state solely to simplify reads.

### B. Separate versioned states and separate events

**Advantages**

- lowest contention;
- small writes/events;
- ownership and TTL can differ.

**Disadvantages**

- frontend can observe player/queue combinations that never formed one valid state;
- cross-state commands need multi-key atomicity but emit independently;
- reconnect and cross-state recovery require coordinating three streams.

**Redis implications:** independent CAS is easy; cross-state consistency is hard.

**Conflict frequency:** lowest within individual states.

**Frontend complexity:** highest because partial events must be joined.

**Migration risk:** moderate; close to current keys, but current partial events
cannot supply versions.

**Decision:** rejected as the public synchronization contract because it cannot
represent one atomic player-plus-queue result reliably.

### C. Separate versioned states with common event envelope

**Advantages**

- low contention for unrelated operations;
- an event can atomically describe every state changed by one command;
- per-state versions support stale/duplicate detection without a separate vector;
- snapshot and event share DTOs;
- current Redis keys can be migrated incrementally.

**Disadvantages**

- persistence must provide atomicity for cross-state commands;
- version-vector comparison is more complex than one scalar;
- coherent snapshot reads require a transaction, revision check, or retry.

**Redis implications:** per-state CAS for isolated changes; transaction/Lua or
equivalent for commands changing multiple states.

**Conflict frequency:** lower than a single snapshot; conflicts occur only on
states actually changed by both commands.

**Frontend complexity:** medium; one reducer compares versions per included state.

**Migration risk:** lowest of the viable choices because isolated v2 keys do not
need to read, convert, or mutate legacy values.

**Decision:** recommended.

## 8. Device and connection identity

```text
User
  Device(deviceId, name)
    Connection(connectionId, connectedAt, lastSeenAt)
```

- `deviceId` is generated once by the client and persisted in local storage.
- `connectionId` comes only from `HubCallerContext.ConnectionId`; clients must not
  be allowed to assert it in registration.
- Registration payload contains `deviceId`, display name, and protocol version.
- The server attaches the current context connection to that device.
- Repeated registration of the same connection is a no-op except safe metadata
  refresh.
- Reconnect attaches the new connection and eventually expires/removes the stale
  connection without changing `deviceId`.
- Disconnect removes only the current connection.
- A device remains online while at least one connection is live.
- Stale presence is removed by heartbeat/lease expiry, not only
  `OnDisconnectedAsync`.
- Selecting an active device accepts `deviceId`; the server verifies it belongs to
  the authenticated user and is online.
- Selecting another user's device returns `NotFound` rather than revealing it.
- Selecting an offline owned device returns `DeviceOffline`.
- For the active device, the server retains its current live
  `audioOwnerConnectionId`. If none exists, it selects the earliest still-live
  connection by server `connectedAt`, breaking ties by connection ID.
- If the audio-owner connection disconnects while another connection of the same
  active device remains, the server elects the next owner with the same rule;
  `activeDeviceId` and play state remain unchanged.

### Active-device failover policy

If the last live connection of the active device disappears:

1. `activeDeviceId` becomes `null`;
2. `isPlaying` becomes `false`;
3. the last authoritative track/current item, volume, and position anchor remain;
4. the server emits one event containing updated player and presence state;
5. no other device is promoted automatically.

Rationale: choosing the first Redis/list entry is not stable, and automatic
promotion can unexpectedly start audio. Explicit user selection is deterministic
and respects autoplay/privacy constraints.

If one of several tabs on the active device disconnects, the active device and
play state remain unchanged.

After backend restart, all persisted connections are treated as stale/offline.
Clients re-register and rebuild presence. Persisted `activeDeviceId` may remain
selected only after that device re-registers within a short recovery grace period;
until then `isPlaying` is false. If the grace period expires, `activeDeviceId`
becomes null.

## 9. Command envelope

```csharp
record ConnectCommand<TPayload>(
    Guid CommandId,
    Guid DeviceId,
    TPayload Payload);

record PlayPayload(long ExpectedPlayerVersion);
record SelectActiveDevicePayload(
    Guid TargetDeviceId,
    long ExpectedPresenceVersion);
record SelectTrackPayload(
    Guid QueueItemId,
    long ExpectedQueueVersion,
    long ExpectedPlayerVersion);
```

Fields:

| Field | Decision |
|---|---|
| `commandId` | Required UUID. Deduplication key scoped to authenticated user. |
| `deviceId` | Required for every mutation. Must be registered to the caller's current connection. |
| expected version | Not part of the common envelope. Each typed payload carries only versions of states that command can change. |
| payload | Required typed payload; may be an empty object for commands without parameters. |
| client timestamp | Not included in the mutation contract. It may be separate telemetry but never controls order or position anchor. |
| correlation ID | Not needed. `commandId` is also the correlation/causation ID. Distributed tracing may use transport headers/activity IDs separately. |
| `userId` | Forbidden in payload. Derived from authentication. |
| `connectionId` | Forbidden in payload. Derived from the hub context. |

Expected-version ownership by command:

| Commands | Expected versions carried by their typed payload |
|---|---|
| `Play`, `Pause`, `Seek`, `ChangeVolume` | `expectedPlayerVersion` |
| `SelectActiveDevice` | `expectedPresenceVersion` |
| `AddQueueItem`, `RemoveQueueItem`, `ReorderQueueItem`, `ShuffleQueue`, `UnshuffleQueue`, `SetRepeatMode` | `expectedQueueVersion`; also `expectedPlayerVersion` only for a variant that can change Player |
| `SelectTrack`, `Next`, `Previous` | `expectedQueueVersion` and `expectedPlayerVersion` |

No command carries `expectedPresenceVersion` unless it changes Presence, and no
command carries Player or Queue versions merely for diagnostics.

Registration is lifecycle negotiation rather than a state command and uses a
separate `RegisterConnectionRequest`.

Successful commands return a `CommandResult` immediately and also produce a
`ConnectStateChanged` event. The event is authoritative; the result lets the caller
detect conflict/error without waiting indefinitely.

## 10. State/event versioning

- Versions are separate monotonically increasing 64-bit integers per user and
  state: player, queue, and presence.
- The server increments versions; clients never propose new versions.
- One command increments each changed state exactly once.
- Snapshot and event envelopes contain no separate aggregate-versions object.
  Each included state carries its own version.
- An event carries the complete value of every changed state, not an unversioned
  delta.
- A client applies an included state only when `eventState.version > localVersion`.
- Equal version is a duplicate and is ignored.
- Lower version is stale and is ignored.
- If an included version is greater than `localVersion + 1`, the client applies it
  immediately because the event contains the complete state. A skipped intermediate
  version does not require a snapshot.
- A snapshot is requested only on registration/reconnect, explicit
  `ResyncRequired`, unsupported/corrupt state, or detection of an impossible
  cross-state invariant—not merely because a version number was skipped.
- After reconnect, the client always requests/registers for a complete snapshot
  rather than assuming event replay.
- SignalR delivery order is not treated as durable or sufficient for correctness.
- `eventId` supports diagnostics/event duplicate detection, but state versions are
  the application rule.

## 11. Player command semantics

All commands may be invoked by any registered connection/device belonging to the
authenticated user. Invalid data returns a typed failure without mutation or
version increment.

| Command | Changed state | Duplicate | Stale expected version | Invalid data |
|---|---|---|---|---|
| `SelectTrack` | queue + player | Return original result/current versions; no mutation | `VersionConflict` with relevant current versions | `QueueItemNotFound` |
| `Play` | player | No second transition | Conflict | `NoCurrentTrack`, invalid device |
| `Pause` | player | No second transition | Conflict | Invalid device; pausing already paused is accepted idempotently without version change |
| `Seek` | player | No second transition | Conflict | Position outside `[0,duration]` when duration is known; otherwise negative rejected |
| `ChangeVolume` | player | No second transition | Conflict | Outside `0..100` |
| `SelectActiveDevice` | presence | No second transition | Presence conflict | `DeviceNotFound`/`DeviceOffline` |
| `Next` | player + queue | No second transition | Player or queue conflict | Empty queue; otherwise boundary follows repeat rules |
| `Previous` | player + queue | No second transition | Player or queue conflict | Empty queue; boundary follows repeat rules |

Specific rules:

- `SelectTrack` payload is `{ queueItemId }`, not a bare `trackId`. The item must
  exist in the current queue. Selecting a track outside the queue requires first
  adding a queue item.
- Selecting a track changes Queue `currentQueueItemId`, derives the track from that
  item, resets Player position to zero, and does not implicitly play unless the
  product explicitly approves that behavior.
- `Play` does not accept client position/timestamp as truth. It plays from the
  current authoritative position anchor.
- `Pause` and `Seek` use server receive time for `PositionUpdatedAt`.
- Position progress is derived as
  `positionMs + (serverNow - positionUpdatedAt)` only while playing.
- A user seek command provides a desired position, not an ordering timestamp.
- Volume is one shared user/session value and applies to the active audio device.

## 12. Queue command semantics

### Queue identity and ordering

- Every insertion creates a server-assigned `queueItemId`.
- `trackId` may repeat and is never used as queue-entry identity.
- Public order is the order of `items`.
- `canonicalOrder` preserves unshuffled order.
- `currentQueueItemId` identifies the current occurrence.
- Player and queue current item must match after every atomic command.

| Command | Changed state | Duplicate | Stale expected version | Invalid data |
|---|---|---|---|---|
| `AddQueueItem` | queue | Same `commandId` returns same created `queueItemId` | Conflict | Track not found/not playable |
| `RemoveQueueItem` | queue; player too when current is removed | No second removal | Conflict | `QueueItemNotFound` |
| `ReorderQueueItem` | queue | No second reorder | Conflict | Missing item or index outside valid range |
| `ShuffleQueue` | queue; player if current index representation changes | No second shuffle | Conflict | None; empty queue is accepted no-op |
| `UnshuffleQueue` | queue; player if needed | No second unshuffle | Conflict | Accepted no-op when already unshuffled |
| `SetRepeatMode` | queue | No second change | Conflict | Unsupported enum |
| `Next`/`Previous` | player + queue | No second movement | Conflict | Empty queue |

Detailed behavior:

- **Empty queue:** current item/track null, position zero, paused.
- **Remove non-current item:** order closes; player unchanged.
- **Remove current item:** choose the item now occupying the removed position; if
  none, choose the preceding last item. Reset position to zero and preserve
  `isPlaying` only when another item exists and an active device is online.
  Removing the last item clears and pauses player state.
- **Next:** advance one item. At end: repeat `Queue` wraps; repeat `Track` stays on
  the same item and resets position; repeat `None` pauses at the current final item.
- **Previous:** if current derived position is greater than three seconds, restart
  current item at zero. Otherwise move to the preceding item. At the beginning,
  repeat `Queue` wraps; other modes stay at the first item and reset position.
- **Shuffle:** preserves `queueItemId`s and canonical order, chooses one server-side
  permutation, and keeps the current item current.
- **Unshuffle:** restores canonical order and keeps the current item current.
- **Repeat:** `None`, `Queue`, or `Track`; the server owns its semantics.
- Queue commands return the created/affected `queueItemId` when relevant.

## 13. Device lifecycle and failover

### New tab

1. Client opens SignalR connection.
2. Client sends `RegisterConnection(deviceId, name, protocolVersion)`.
3. Server takes `connectionId` from context, validates `deviceId`, attaches it, and
   increments presence version only if presence changed.
4. Server returns a complete snapshot to caller and broadcasts changed presence.
5. Repeating step 2 on the same connection is idempotent.

### Reconnect

1. SignalR produces a new `connectionId`.
2. Client registers the same `deviceId`.
3. Server attaches the new connection.
4. Old connection is removed by disconnect or lease expiry.
5. Active selection stays on `deviceId`; it is not transferred or lost solely
   because the connection changed.
6. Client obtains a fresh snapshot and discards older versions.

### Disconnect

- Removing one of several connections leaves the device online.
- Removing the final connection marks it offline.
- Final active-device disconnect applies the null-and-pause policy from section 8.
- Disconnect and reconnect races are resolved by connection-specific leases and
  atomic presence/version updates.

### Stale connection

Each connection has a server-managed lease/last-seen value. Expiry performs the
same transition as disconnect. Client-supplied last-seen data is not trusted.

### Security

- Registration can attach only `Context.ConnectionId`.
- Commands require the issuing connection to be registered under envelope
  `deviceId`.
- Active selection validates an owned online device.
- Unknown/foreign device is reported as `DeviceNotFound`.

## 14. SignalR methods and events

### Proposed hub methods

```text
RegisterConnection(RegisterConnectionRequest) -> RegistrationResult
GetSnapshot() -> ConnectSnapshot
SelectTrack(ConnectCommand<SelectTrackPayload>) -> CommandResult
Play(ConnectCommand<EmptyPayload>) -> CommandResult
Pause(ConnectCommand<EmptyPayload>) -> CommandResult
Seek(ConnectCommand<SeekPayload>) -> CommandResult
ChangeVolume(ConnectCommand<ChangeVolumePayload>) -> CommandResult
SelectActiveDevice(ConnectCommand<SelectActiveDevicePayload>) -> CommandResult
AddQueueItem(ConnectCommand<AddQueueItemPayload>) -> CommandResult
RemoveQueueItem(ConnectCommand<RemoveQueueItemPayload>) -> CommandResult
ReorderQueueItem(ConnectCommand<ReorderQueueItemPayload>) -> CommandResult
Next(ConnectCommand<EmptyPayload>) -> CommandResult
Previous(ConnectCommand<EmptyPayload>) -> CommandResult
ShuffleQueue(ConnectCommand<EmptyPayload>) -> CommandResult
UnshuffleQueue(ConnectCommand<EmptyPayload>) -> CommandResult
SetRepeatMode(ConnectCommand<SetRepeatModePayload>) -> CommandResult
```

`GetSnapshot` has no mutation envelope. `RegisterConnection` includes device and
protocol information but no asserted connection ID.

### Proposed server-to-client events

```text
ConnectStateChanged(ConnectStateChanged)
ResyncRequired(ResyncRequiredEvent)
```

`ConnectStateChanged` replaces all unversioned partial events. `ResyncRequired`
instructs clients to call `GetSnapshot` after an unsupported v2 state shape,
impossible cross-state invariant, or a recoverable server consistency failure.
A skipped version alone does not cause `ResyncRequired`.

### Results and errors

```csharp
record CommandResult(
    Guid CommandId,
    CommandStatus Status,
    ConnectError? Error);

enum CommandStatus { Applied, NoOp, Duplicate, Rejected, Conflict }

record ConnectError(
    string Code,
    string Message,
    long? CurrentPlayerVersion,
    long? CurrentQueueVersion,
    long? CurrentPresenceVersion,
    bool SnapshotRequired);
```

Expected errors are typed results, not raw `HubException`. Authentication failure
still uses the transport/authentication mechanism.

### Current-to-v2 breaking replacement table

| Current | Decision |
|---|---|
| `RegisterDevice` | Replace with `RegisterConnection`; remove the old method. |
| `RegisterPlayer` | Replace with `GetSnapshot`; remove the old method. |
| `RegisterQueue` | Replace with `GetSnapshot`; remove the old method. |
| `Play` | Keep name; replace payload/envelope and semantics. |
| `Pause` | Keep name; replace payload/envelope and semantics. |
| `ChangePosition` | Replace with `Seek`; remove the old method. |
| `ConnectToDevice` | Replace with `SelectActiveDevice(deviceId)`; remove the old connection-ID method. |
| `ChangeVolume` | Keep name; replace scalar payload with envelope. |
| `AddTracksToQueue` | Replace with singular `AddQueueItem`; batch may be added later with explicit atomic semantics. |
| `RemoveTrackFromQueue` | Replace with `RemoveQueueItem(queueItemId)`. |
| `ChangeTrackIndexInQueue` | Rename to `ReorderQueueItem`. |
| `ShuffleQueue` | Keep name; replace payload/envelope. |
| `UnshuffleQueue` | Keep name; replace payload/envelope. |
| `ToggleRepeatQueue` | Replace with explicit `SetRepeatMode`. |
| `PlayerStateChanged` | Replace with `ConnectStateChanged`; do not publish both. |
| `QueuePlaybackChanged` | Replace with `ConnectStateChanged`; do not publish both. |
| `ActiveDeviceChanged` | Replace with `ConnectStateChanged`; do not publish both. |
| `DeviceListChanged` | Replace with `ConnectStateChanged`; do not publish both. |
| `PositionChanged` | Replace with `ConnectStateChanged`; do not publish both. |
| `VolumeChanged` | Replace with `ConnectStateChanged`; do not publish both. |
| hard-coded test endpoints | Remove from production mapping after equivalent tests exist. |

## 15. Frontend one-way data flow

```text
User intent
  -> intent method creates commandId + only relevant expected version(s)
  -> PlayerHubService invokes SignalR command
  -> server validates and atomically mutates
  -> ConnectStateChanged
  -> version-aware frontend reducer
  -> local UI projection and track hydration
  -> active-device audio adapter
```

### Responsibilities

- **User intent methods:** only these create/send commands. They do not directly
  declare the command successful in authoritative local state.
- **Server-state reducer:** pure/version-aware application of player, queue, and
  presence DTOs. It never invokes the hub.
- **Derived display position:** calculated from player anchor plus elapsed local
  monotonic time adjusted from `serverTime`; it does not rewrite authoritative
  position every tick.
- **Track hydration:** maps `trackId` to display/media data. Use cancellation or
  switch-to-latest keyed by applied version. Hydration never changes authoritative
  ordering.
- **Queue hydration:** hydrates by `queueItemId`/`trackId`, preserves server item
  order and duplicates, and discards results for old queue versions.
- **Audio adapter:** receives already-reduced state and acts only if local
  `deviceId == activeDeviceId` and local connection ID equals
  `audioOwnerConnectionId`.

### Audio DOM events

| Event | Allowed behavior |
|---|---|
| `timeupdate` | Update ephemeral local display/telemetry only. Do not send seek commands. |
| `durationchange`/metadata | Update local media metadata only. |
| `playing` | Observe actual adapter status; do not send `Play` when caused by server application. |
| `pause` | Observe actual adapter status; do not send `Pause` when caused programmatically/server-side. |
| `volumechange` | Do not send a command when applying server volume. User slider intent sends `ChangeVolume` directly. |
| `seeking`/`seeked` | Do not send when applying server state. User seek UI sends one `Seek` intent. |
| `ended` | Active device may send one `Next` command with a new `commandId`; inactive devices do nothing. |
| `error`/autoplay rejection | Update local adapter/error state; do not alter authoritative play state automatically. Optionally expose an explicit user retry intent. |

The adapter needs an application guard/origin flag so programmatic audio changes
cannot be interpreted as user intents.

## 16. Error handling

Expected error codes:

- `VersionConflict`
- `DuplicateCommand`
- `DeviceNotRegistered`
- `DeviceNotFound`
- `DeviceOffline`
- `QueueItemNotFound`
- `TrackNotFound`
- `TrackNotPlayable`
- `NoCurrentTrack`
- `InvalidPosition`
- `InvalidVolume`
- `InvalidQueueIndex`
- `InvalidRepeatMode`
- `StateUnavailable`
- `SnapshotRequired`
- `UnsupportedProtocolVersion`

Rules:

- Validation/rejection does not mutate state or increment versions.
- Conflict includes current versions and normally sets `SnapshotRequired = true`.
- Duplicate returns the stored original outcome or an equivalent
  `Duplicate/NoOp` result with current versions.
- Unexpected server faults are logged with `commandId` but return a stable generic
  code without internal details.
- A client receiving `SnapshotRequired`, an unknown state shape, or an impossible
  cross-state combination calls `GetSnapshot`. A version gap alone is not an error
  because each event carries a complete changed state.
- Authentication/authorization failures terminate or reject the hub operation and
  never reveal another user's state.

## 17. Redis consistency expectations

This contract does not select the T001-03 implementation mechanism, but requires:

1. Per-user state versions are persisted with their state.
2. Compare-and-set is atomic for each isolated state mutation.
3. Player-plus-queue and player-plus-presence transitions are all-or-nothing.
4. Command deduplication record and state mutation commit atomically.
5. A successful command can reconstruct its result after duplicate delivery.
6. Snapshot read returns mutually consistent Player, Queue, and Presence states;
   if their versions change during the read, the server retries.
7. Version counters never move backward after ordinary restart.
8. Presence connections are leases and are not restored as online merely because
   JSON remains.
9. Persisted player/queue state may survive backend restart; presence is rebuilt.
10. Missing/expired player and queue state creates a deterministic empty session
    at version zero/initial version.
11. Malformed or unsupported v2 state is quarantined/ignored and causes a
    deterministic fresh v2 state plus `ResyncRequired`, not partial deserialization.
12. TTL policy must not allow one part of an active session to expire while other
    parts continue without a defined recovery.

RabbitMQ or another broker is not part of this contract.

## 18. Migration and rollout strategy

### Breaking-change decision

- The synchronization protocol has only the v2 contract defined in this document.
- Backend and frontend are migrated as a coordinated breaking change.
- Do not implement v1 adapters, parallel hub methods, duplicate legacy events,
  compatibility DTOs, or a one-release compatibility window.
- Old methods, events, DTOs, and constants are removed or replaced in their
  relevant implementation tasks.
- A protocol version may remain in registration for diagnostics and future
  evolution, but it does not negotiate v1 behavior.
- Existing tabs running the old frontend are unsupported after deployment and must
  reload or reconnect onto the new application version.

### Storage

- Use isolated versioned keys:

```text
connect:v2:player:{userId}
connect:v2:queue:{userId}
connect:v2:presence:{userId}
```

- The v2 implementation must never read or write legacy Connect keys.
- Do not implement a legacy-state reader or convert existing values.
- In particular, never convert legacy `ActiveDeviceId` connection IDs into v2
  persistent device IDs.
- Missing v2 state initializes deterministically as an empty paused session with
  no active device and no online presence.
- Do not mutate legacy JSON values in place.
- In development, operators may explicitly delete legacy Connect keys before or
  after rollout.
- Outside development, operators may delete legacy keys during deployment or
  leave them to expire under their existing TTL, provided v2 cannot read them.
- T001-03 must document the exact safe cleanup command and target key patterns;
  deployment automation is out of scope for this task.

### Coordinated deployment order

1. Build and verify the v2 backend and v2 frontend independently against contract
   fixtures and tests.
2. Prepare both deployable artifacts before changing either production side.
3. During the deployment boundary, stop or drain old Connect traffic where
   practical.
4. Deploy the v2 backend and v2 frontend as one coordinated release window.
5. Force old tabs to reload/reconnect through normal application deployment/cache
   invalidation behavior.
6. Verify v2 registration, snapshot, commands, events, Redis keys, and multi-device
   behavior.
7. Delete legacy keys explicitly or leave them isolated to expire.

### Temporary integration boundary

The old frontend and v2 backend are intentionally incompatible, as are the v2
frontend and old backend. A mixed-version environment may fail registration or hub
invocation and is not a supported operating mode.

Each repository should remain buildable and testable during its own migration.
Before the coordinated deployment, cross-repository integration uses frozen v2
contract fixtures or a matching branch/artifact. Merge/deployment sequencing must
not imply runtime backward compatibility.

## 19. Scenario matrix

| # | Scenario | Expected result |
|---|---|---|
| 1 | One user, one device, one tab | Registration creates one device/connection and returns a snapshot. User selects the device; accepted commands produce increasing player/queue versions and only this device plays. |
| 2 | One user, one device, two tabs | Both connections attach to one `deviceId`. Both UIs converge. If the device is active, only authoritative `audioOwnerConnectionId` may play; see approval item 8. |
| 3 | One user, two devices | Both control state; only the selected persistent device applies audio. Handoff pauses the old device before/while the new event is applied. |
| 4 | Two users | Separate auth-derived user keys/groups/vectors. Commands/events never cross users. Foreign device selection returns `DeviceNotFound`. |
| 5 | Reconnect active device with new connection ID | New connection registers the same device ID. Active selection remains that device; snapshot restores UI. Old connection expires without changing active device if another connection remains. |
| 6 | Disconnect one of two tabs on one device | Only that connection is removed. Device remains online and active; no player pause/failover occurs. |
| 7 | Disconnect active device | On its final connection loss, server atomically sets active null and paused, preserves current item/position, and broadcasts player+presence. No automatic promotion. |
| 8 | Backend restart with Redis state | Player/queue and versions survive. All persisted connections are offline. Clients re-register and receive snapshots; playback stays paused until valid active-device recovery/selection. |
| 9 | Redis state absent/expired | Server creates deterministic empty queue/player/presence. Snapshot is valid, paused, and versioned; no null dereference. |
| 10 | Two devices change position simultaneously | Same expected player version allows only one atomic winner. Loser receives `VersionConflict`, requests snapshot, and may retry only as a new explicit user intent. |
| 11 | Two devices change queue simultaneously | Same expected queue version allows one winner. Other command conflicts; no lost update or mixed player/queue state. |
| 12 | Same command delivered twice | Deduplication returns the first outcome. Versions increment once and at most one authoritative transition is broadcast. |
| 13 | Events arrive out of order | Reducer applies complete states with greater versions and ignores equal/lower versions. A gap is accepted without snapshot; impossible cross-state combinations trigger resync. |
| 14 | Same track added twice | Server creates two different `queueItemId`s with the same `trackId`; both remain independently removable/reorderable. |
| 15 | Current queue item removed | Server selects the item at the removed position or preceding last item, resets position, keeps play only if another item and online active device exist; last removal clears and pauses. |
| 16 | Inactive tab receives play event | It updates UI/derived position but never calls audible `audio.play()`. |
| 17 | Active-device handoff while playing | One event changes Presence `activeDeviceId` and `audioOwnerConnectionId`. Old owner pauses; new owner loads/seeks/sets volume and may play subject to browser permission. No client echoes `Play`. |
| 18 | Old track hydration completes after newer state | Switch-to-latest/version guard discards the old response; current track/audio remain on the newer state. |

### Multi-tab audio ownership note

Persistent `deviceId` intentionally groups tabs as one logical device, but the
single-audible-output invariant also needs a per-device tab owner. The recommended
v2 contract stores `audioOwnerConnectionId` in presence state for the active
device. The server retains a live owner or deterministically selects the earliest
live connection. This connection ID is presence-only and never replaces
`activeDeviceId`. Reconnect may elect a new owner within the same device. This
detail requires explicit approval because an alternative is browser-local leader
election.

## 20. Decisions still requiring human approval

The no-backward-compatibility protocol and isolated v2 Redis-key decisions are
approved and no longer appear below.

The following decisions block the final T001-02 domain model:

1. **State model:** approve separate versioned player/queue/presence states with a
   common event envelope and versions embedded only in their owning states.
2. **Failover:** approve `activeDeviceId = null` and pause after the active device's
   final connection disconnects, with no automatic promotion.
3. **Track selection:** approve selection by existing `queueItemId`; selecting a
   track outside the queue first creates a queue item.
4. **Select behavior:** approve that selecting a track resets position but does not
   implicitly start playback.
5. **Remove-current behavior:** approve deterministic next-at-same-index/fallback-
   previous behavior and whether playback remains active.
6. **Previous threshold:** approve the proposed three-second restart threshold.
7. **Concurrency:** approve conflict-and-resync rather than silent last-write-wins
   for simultaneous commands.
8. **Multi-tab audio owner:** choose server-selected `audioOwnerConnectionId`
   (recommended) or browser-local leader election.
9. **Restart recovery grace:** approve whether a previously active device gets a
   short chance to re-register while playback remains paused, and choose duration.

The following decision does not block the core T001-02 model, but must be resolved
before the corresponding frontend behavior:

10. **Position conflict UX:** approve whether a losing seek is dropped after resync
    or offered as a user-visible retry; automatic retry is not recommended.

## 21. Inputs for T001-02 through T001-11

### T001-02 — Backend state model

- Model persistent devices separately from connection leases.
- Replace active connection identity with persistent `deviceId`.
- Add queue item identity and per-state versions.
- Encode player/queue consistency and failover invariants in Domain operations.
- Keep transport DTOs in Contracts/Shared rather than exposing Domain models.

### T001-03 — Redis persistence

- Choose atomic CAS/transaction mechanism satisfying section 17.
- Define v2 keys, deduplication retention, lease expiry, snapshot read retry, and
  safe operational cleanup instructions for isolated legacy keys.
- Test conflicts, duplicates, multi-state atomicity, expiry, and restart.

### T001-04 — Device lifecycle

- Implement `User -> Device -> Connection[]`.
- Register context connection, manage leases, and enforce owned-online selection.
- Implement approved active-device disconnect and restart grace policies.

### T001-05 — Player commands

- Implement the envelope, expected player version, validation, server time anchors,
  deduplication result, and typed failures.
- Keep any registered device authorized to control player state.

### T001-06 — Queue commands

- Generate `queueItemId`, preserve duplicate tracks and canonical order, and
  atomically synchronize current item with player state.
- Implement approved remove/next/previous/shuffle/repeat semantics.

### T001-07 — SignalR orchestration

- Replace old methods/events with v2 registration/snapshot/command methods and the
  versioned state event.
- Derive user/connection identity from hub context.
- Do not add v1 groups, compatibility adapters, or duplicate events; remove
  hard-coded endpoints later.

### T001-08 — Frontend hub adapter

- Add protocol diagnostics, typed envelopes/results, persistent-device registration,
  complete-state application across version gaps, queue events, and reconnect
  snapshot.
- Do not expose connection ID as active device identity.

### T001-09 — Frontend reducer

- Separate user intents from pure server-state application.
- Track each state's embedded version, accept complete newer states across version
  gaps, cancel stale hydration, and derive display position without mutating
  authoritative state.

### T001-10 — Audio engine

- Enforce persistent active device plus approved per-device audio owner.
- Guard programmatic audio events from command emission.
- Handle null track, handoff, autoplay rejection, and Safari safely.

### T001-11 — Queue frontend

- Preserve `queueItemId`, duplicates, and server order through hydration.
- Send intents for queue actions and apply only authoritative queue/player events.
- Cover concurrent edits and stale hydration.

No T001-02 or later implementation should begin until the approval items affecting
its domain behavior are resolved.
