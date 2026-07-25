## Task-001 — Refactor Connect module synchronization

### Goal

Refactor the existing Connect module so that playback state, active-device state, and queue state are synchronized consistently between all devices belonging to the same user.

The task affects the existing implementation. Do not design the Connect module from scratch.

### Functional requirements

1. Audio may physically play on only one device at a time.

2. Any connected device may select the current track.

3. Any connected device may start or pause playback.

4. Any connected device may change the current playback position.

5. Any connected device may select which device is the active playback device.

6. Any connected device may change the playback volume.

7. Any connected device may add tracks to the shared playback queue.

8. Any connected device may remove tracks from the shared playback queue.

9. Changes made from one device must be propagated to all other connected devices of the same user.

10. Inactive devices must display synchronized player state but must not produce audible playback.

### State ownership

The server-side Connect state must be treated as the shared source of truth for:

* current track;
* play/pause state;
* playback position;
* volume;
* active device;
* playback queue.

The active device is responsible for actual audio playback.

A persistent device ID must remain separate from the transient SignalR connection ID.

### Synchronization requirements

The implementation must account for:

* multiple devices;
* multiple browser tabs;
* SignalR reconnects;
* stale Redis connection state;
* backend restarts;
* active-device disconnection;
* duplicate or repeated SignalR commands;
* feedback loops between local audio events and remote state updates.

Remote state application should be idempotent where practical.

### Expected behavior

When a command is issued from any connected device:

1. The server validates and updates the shared state.
2. The updated state is broadcast to all connected devices.
3. All devices update their UI.
4. Only the active device applies commands to the real audio element.
5. Applying server state must not cause the same command to be sent back to the server repeatedly.

### Analysis scope

Do not implement anything yet.

Inspect the current backend and frontend implementation and determine:

* the current Connect module architecture;
* the current Redis models and repositories;
* existing SignalR hub methods and events;
* device registration and reconnect behavior;
* active-device selection behavior;
* player-state synchronization;
* queue synchronization;
* frontend state and audio-engine responsibilities;
* existing feedback-loop risks;
* stale-state and concurrency risks;
* missing tests.

### Required task breakdown

Split this task into smaller implementation tasks.

For every resulting task provide:

* task ID;
* title;
* goal;
* affected repository;
* affected projects or frontend areas;
* dependencies;
* complexity: small, medium, large;
* whether it can run in parallel;
* acceptance criteria;
* required verification;
* main risks.

Separate backend and frontend work where possible.

Do not create one large implementation task covering the entire Connect module.

### Roadmap requirements

Propose an implementation order that keeps the project buildable after every task.

Identify:

* foundational tasks;
* backend-only tasks;
* frontend-only tasks;
* tasks that require coordinated backend and frontend contract changes;
* tasks that can be implemented in parallel;
* tasks that require integration or multi-device testing.

Do not modify source files, configuration, tests, or documentation during this analysis.
