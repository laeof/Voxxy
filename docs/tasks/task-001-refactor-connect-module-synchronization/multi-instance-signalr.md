# Connect multi-instance SignalR

Connect uses the official ASP.NET Core SignalR Redis backplane. Authoritative state remains in
`connect:v2:*`; SignalR channels use the separate `voxxy:signalr` namespace.

## Configuration

```json
{
  "SignalR": {
    "Backplane": {
      "Enabled": true,
      "ChannelPrefix": "voxxy:signalr"
    }
  }
}
```

The backplane reuses `ConnectionStrings:Redis` unless
`SignalR:Backplane:ConnectionString` is set explicitly. An enabled backplane without a connection
string fails during startup configuration. Set `Enabled` to `false` only for an intentional
single-instance environment.

## Local two-instance verification

1. Start Redis on `localhost:6379`.
2. Start backend instance 1 on port 5001.
3. Start backend instance 2 on port 5002 with the same Redis connection and channel prefix.
4. Connect two clients for the same user, one to each backend.
5. Change volume through instance 1 and verify a Player event arrives through instance 2.
6. Select a queue item through instance 2 and verify a combined Player/Queue event arrives through
   instance 1.
7. Verify another user receives neither event.
8. Stop instance 1, reconnect its client to instance 2, and request a snapshot.

Example environment overrides:

```text
ConnectionStrings__Redis=localhost:6379
SignalR__Backplane__Enabled=true
SignalR__Backplane__ChannelPrefix=voxxy:signalr
ASPNETCORE_URLS=http://localhost:5001
```

Use port 5002 for the second process.

## Delivery semantics

The Redis backplane is not a transactional outbox. If state commit succeeds and event delivery
fails, the state remains authoritative and the Hub reports `connect_delivery_unconfirmed`.
Retrying the same command ID is safe and snapshot reconciliation restores the client.

The load balancer must support WebSockets. Because the current clients negotiate transports and
may fall back to Server-Sent Events or long polling, deployment affinity should remain enabled
unless the deployment enforces WebSockets with negotiation skipped.
