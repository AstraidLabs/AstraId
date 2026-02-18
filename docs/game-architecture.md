# AstraID Idle Stellaris-like Engine

## Module summary

- `AppServer.Modules.Game.Domain`: entity model + game enums.
- `AppServer.Modules.Game.Application`: tick engine, command pipeline, deterministic procgen, state orchestration.
- `AppServer.Modules.Game.Infrastructure`: EF Core `GameDbContext`, module DI wiring.
- `AppServer.Modules.Game.Contracts`: API DTO contracts.
- `AppServer.Modules.Game.Api`: authenticated, rate-limited endpoints under `/api/game/*`.

## Authoritative simulation

- Tick engine runs server-side only (`IGameTickEngine`) and is applied lazily on `GET /api/game/state` and `POST /api/game/commands`.
- Commands are validated, deduped by `commandId`, persisted for audit and replay tracking.
- UTC timestamps are mandatory and tick deltas are clamped by a configurable cap.

## Procedural generation

- Uses deterministic `Xoshiro256**` RNG (no `System.Random`) and explicit seeds.
- Starter systems guarantee 6-12 planets with >=2 habitable worlds.
- Shared shard generation emits spiral coordinates, hyperlanes, and special stars (black hole/neutron).
- Physics plausibility pass blocks impossible core black-hole placement.

## Onboarding & graduation

- New users start in `ProtectedSystem` phase with private-only visibility.
- Graduation requires FTL research completion + 2 colonized planets + >=75% surveyed.
- On graduation, player is placed in shared shard and gets a 48h emerging shield.
