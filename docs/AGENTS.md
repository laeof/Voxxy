# Repository overview

This repository contains the .NET backend. The solution is `Voxxy.slnx`; production projects are under `src/` and tests are under `tests/`.

The main application uses Clean Architecture. The backend is gradually moving toward a modular monolith, but only `src/Connect` is currently a separate module. Do not describe the whole backend as a completed modular monolith or move existing features into modules unless explicitly requested.

# Current architecture

- `SharedKernel` contains shared primitives and cross-cutting contracts.
- `Domain` contains DDD-style entities, value-oriented domain types, and domain events.
- `Application` contains use cases, CQRS handlers, validation, and abstractions.
- `Infrastructure` implements Application abstractions and integrates external systems.
- `Web.Api` contains Minimal API presentation and is the composition root.
- `Connect` is a separately layered realtime player module.

Preserve existing feature boundaries and naming unless a task explicitly changes the architecture.

# Dependency direction

Keep dependencies pointing inward:

```text
Web.Api -> Infrastructure -> Application -> Domain -> SharedKernel
```

- Domain must not depend on Application, Infrastructure, or Web.Api.
- Application must not depend on Infrastructure or Web.Api.
- Infrastructure implements abstractions owned by Application.
- Web.Api wires dependencies and modules together.
- Do not expose EF Core, Redis, Azure SDK, MeiliSearch, SignalR, or ASP.NET Core types through Domain or Application contracts.
- Standard .NET types such as `Stream`, `Guid`, `CancellationToken`, and `DateTimeOffset` are valid contract types.

Update architecture tests when establishing a new enforceable boundary.

# Clean Architecture conventions

- Keep business rules in Domain and use-case orchestration in Application.
- Define external capabilities as Application abstractions and implement them in Infrastructure.
- Keep infrastructure configuration and SDK construction out of Domain and Application.
- Use existing domain-event patterns for side effects originating from domain changes.
- Do not introduce a new framework or cross-cutting abstraction when an established repository pattern already covers the use case.

# CQRS, Result, validation and endpoint conventions

- Model use cases with the existing `ICommand`, `ICommand<T>`, `IQuery<T>`, and handler interfaces.
- Return the existing `Result` type for expected business and validation failures. Do not use exceptions for expected control flow.
- Add FluentValidation validators following the existing command-validation pipeline.
- Keep Minimal API endpoints thin: bind and map input, invoke one handler, and map its `Result`.
- New endpoints must follow the existing `IEndpoint` discovery approach.
- Apply authorization explicitly and consistently with neighboring endpoints.
- Pass `CancellationToken` through asynchronous calls.

# Connect module boundaries

`Connect` is the only current modular-monolith subsystem. Keep its Domain, Application, Infrastructure, Presentation, Shared, and Contracts responsibilities distinct.

- `Connect.Domain` must remain independent of other Connect layers.
- `Connect.Application` coordinates use cases through abstractions and must not contain Redis or SignalR implementation details.
- `Connect.Infrastructure` owns Redis repositories and related registration.
- `Connect.Presentation` owns SignalR hubs and endpoints.
- Use `Connect.Contracts` for intentional cross-module contracts; do not couple modules through implementation types.
- Do not migrate non-Connect features into modules without an explicit request.

# EF Core and database rules

- Use `ApplicationDbContext` through the existing Application abstraction where appropriate.
- Do not use a single `DbContext` concurrently; await each operation before starting another on the same instance.
- Use `AsNoTracking()` for read-only queries when tracking is unnecessary.
- Preserve PostgreSQL snake_case conventions and the configured migrations history schema.
- Generate migrations through EF tooling. Do not manually edit generated migrations unless explicitly instructed.
- Keep transactions and `SaveChanges` boundaries clear, especially around domain-event side effects.
- Avoid N+1 queries and unbounded materialization; project only required data.

# Redis and SignalR rules

- Redis persistence and repositories belong to `Connect.Infrastructure`.
- SignalR hubs and client-facing realtime behavior belong to `Connect.Presentation`.
- Never confuse a persistent device ID with a transient SignalR connection ID.
- Connect changes must account for reconnects, stale Redis state, multiple devices, and multiple browser tabs.
- Reconnection must restore the required device, player, and queue registrations safely.
- Define cleanup and expiration behavior for connection-scoped Redis state.
- Avoid feedback loops between locally generated audio events and SignalR-originated state changes.

# Media streaming and Azure Blob rules

- Access Blob Storage through Application-owned abstractions implemented by Infrastructure.
- Do not expose Azure SDK types through Domain or Application contracts.
- Preserve streaming range requests and `206 Partial Content` behavior.
- Avoid buffering complete audio files when streaming can remain incremental.
- Validate media identifiers, content types, and file-size assumptions at the appropriate boundary.
- Keep media URL generation consistent between local Azurite and deployed Blob Storage.

# Infrastructure rules

- PostgreSQL, Redis, Azurite, MeiliSearch, SignalR, and Docker are existing infrastructure.
- Add configuration validation and health checks when introducing a required external dependency.
- Keep secrets out of committed configuration; use environment variables or .NET user secrets.
- RabbitMQ is not implemented. Do not add RabbitMQ, MassTransit, or another broker without an explicit architectural requirement.
- Do not infer that the existing `OutboxMessage` model implies a configured message broker.
- Keep Docker service names, ports, networks, and application connection strings aligned.

# Verification commands

Run from this repository:

```bash
dotnet restore Voxxy.slnx
dotnet build Voxxy.slnx
dotnet test Voxxy.slnx
docker compose config --quiet
```

The solution currently contains `tests/ArchitectureTests`. Treat an existing failure separately from a regression introduced by the task, and report both clearly.

There is no repository-defined formatting or lint command. Do not invent one in change reports.

# Git and change discipline

- Inspect `git status --short` before editing.
- Preserve unrelated staged, unstaged, and untracked work.
- Do not revert, overwrite, or reformat unrelated files.
- Keep changes scoped to the requested backend task.
- Do not create commits, push branches, or open pull requests unless explicitly requested.
- Do not change frontend files from this repository task.
- Report generated files and migrations explicitly.

# Code review priorities

Review changes in this order:

1. Layer and Connect-module boundary violations.
2. Data loss, authorization, secret exposure, and unsafe file handling.
3. Incorrect `DbContext` lifetime/concurrency or inefficient queries.
4. Streaming regressions, especially range and partial-content behavior.
5. Redis/SignalR identity, reconnect, stale-state, and multi-device issues.
6. CQRS, validation, `Result`, and thin-endpoint convention violations.
7. Missing or misleading verification and infrastructure configuration.
