# Mobile Connect playback command ordering and reconciliation

## Status

Deferred. Investigated on 2026-09-13; implementation intentionally postponed.

## Context

The React Native/Expo client does not currently invoke the backend
`SelectQueueItem` command. A tap on a track follows this path:

```text
PlaylistItem
  -> PlaybackController.playContext
  -> ConnectCoordinator.playContext
  -> StartPlaybackContext
```

Consequently, the backend `PlayerState.Version` double-increment defect in
`SelectQueueItem` is not directly reproduced by the current mobile track-selection
path. The mobile implementation nevertheless has two related ordering problems.

## Problem 1: local-to-Connect reconciliation is not atomic

When local state is adopted by Connect, `PlaybackCoordinatorRouter` can issue three
separate server commands:

```text
StartPlaybackContext
ChangePosition
Pause
```

For a locally paused player, `StartPlaybackContext` temporarily creates a playing
server state. That intermediate state can be broadcast and projected before the
later `Pause` command arrives. Possible symptoms are a short audio start, active-track
flicker, or other clients briefly observing an incorrect playback state.

### Intended direction

Introduce one atomic server operation that reconciles context, selected item,
position, and desired play/pause state. An alternative is extending
`StartPlaybackContext` with explicit initial position and playback intent. Clients
must not observe an intermediate playing state when the source snapshot is paused.

## Problem 2: mobile user commands are not serialized

Rapid taps can start multiple `StartPlaybackContext` calls concurrently. Each call
has a different command ID, so backend command deduplication does not collapse them.
Optimistic concurrency protects Redis from lost writes, but it does not guarantee
that completion order matches tap order. An older selection may finish after a newer
selection and become authoritative.

### Intended direction

Add a mobile playback-command arbiter with these rules:

- `playContext`, `next`, and `previous` are ordered through one command pipeline;
- a newer track selection supersedes an older selection that has not completed;
- results and errors from superseded operations cannot update UI or trigger fallback;
- uncertain SignalR delivery is reconciled through a snapshot and is never retried
  with a new command ID;
- local-only playback keeps the existing audio load generation guard.

## Acceptance criteria

1. Switching from a paused local player to Connect never emits an intermediate
   playing state and never produces audible playback.
2. After rapid selection of tracks A, B, and C, track C is always authoritative.
3. A late response or error from A or B cannot change the active track, show an
   obsolete error, or disable Connect.
4. Only one physical audio source can remain active.
5. The behavior is covered for online, reconnecting, Redis unavailable, and
   offline-to-online reconciliation scenarios.

## Required tests

- Mobile coordinator test with delayed responses completing out of order.
- Mobile reconciliation test starting from a paused snapshot with a non-zero
  position.
- Backend test for an atomic context/position/playback-intent mutation.
- Multi-client test proving that no intermediate playing event is broadcast.
- Regression test for rapid taps during SignalR reconnect and snapshot recovery.
