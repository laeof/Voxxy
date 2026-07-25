# T001-10 — Production SignalR transport hardening

## Production transport policy

Connect v2 uses:

- WebSockets only;
- `skipNegotiation: true`;
- Redis SignalR backplane enabled;
- no session affinity requirement for the Connect hub.

This conclusion applies only to `/api/hubs/connect`. Other application routes have not been
audited for affinity requirements.

The backend also restricts this endpoint to `HttpTransportType.WebSockets`. SSE and Long Polling
remain available to other SignalR hubs if they need them.

## Reverse proxy

The proxy must preserve `/api/hubs/connect`, use HTTP/1.1 upgrade (or equivalent HTTP/2 WebSocket
semantics), forward `Upgrade` and `Connection`, and keep the connection idle for at least 75
seconds. The frontend NGINX configuration contains a dedicated exact-match location so it does
not conflict with the general `/api/` route.

## Delivery recovery

`connect_delivery_unconfirmed` means the authoritative mutation may already be committed.
The client never retries it with a new command ID. It requests one coherent snapshot, applies it
through the existing version-guarded store, clears the pending command, and returns a neutral
duplicate acknowledgement. If reconciliation fails, existing Player/Queue/Presence state is
retained and sync status becomes `OutOfSync`; reconciliation runs again after reconnect.

## Health endpoints

- `/health/live` excludes the backplane and remains healthy during a Redis outage.
- `/health/ready` includes `signalr-backplane`.
- `/health` retains the aggregate compatibility endpoint.

The backplane check creates a bounded, independent connection using the exact configured
`SignalR:Backplane:ConnectionString`. It does not use the Connect persistence multiplexer.
Invalid Redis syntax and namespace overlap fail startup validation. A valid but unreachable
endpoint makes readiness unhealthy without failing startup.

## Proxy smoke

Start the proxy, frontend, Redis and backend, obtain a development JWT, then run:

```bash
cd Voxxy.Web
CONNECT_PROXY_URL=http://localhost/api/hubs/connect \
CONNECT_ACCESS_TOKEN='<development JWT>' \
node scripts/smoke-connect-proxy.mjs
```

The smoke performs a WebSocket upgrade through NGINX, invokes `GetSnapshot`, invokes a mutation,
observes its SignalR event through the proxy, and keeps the socket alive longer than the 15-second
heartbeat interval.

## Multi-instance manual acceptance

1. Run shared Redis, two backend instances with the same channel prefix, NGINX without affinity,
   and the frontend through NGINX.
2. Confirm the browser Network panel shows a WebSocket for `/api/hubs/connect`, with no negotiate,
   SSE, or Long Polling request.
3. Open two authenticated tabs for one user and verify volume, seek and queue events cross
   instances.
4. Stop the instance serving the first tab. Confirm local audio pauses, the tab reconnects through
   the same public URL, keeps its persistent device ID, receives a new connection ID, reconciles a
   snapshot, registers, and only resumes when new authoritative Presence grants ownership.
5. Stop Redis. Confirm `/health/ready` is unhealthy and `/health/live` remains healthy. Restart
   Redis and confirm readiness recovers.
6. Inject a broadcaster failure after commit. Confirm the client requests a snapshot and does not
   repeat the mutation with a new command ID.

## Remaining limitations

- There is no transactional outbox; delivery uncertainty is recovered through snapshots.
- The smoke requires an externally issued development JWT and is therefore not run as an
  unauthenticated CI test.
- Certificate termination and non-Connect affinity policy remain deployment responsibilities.
