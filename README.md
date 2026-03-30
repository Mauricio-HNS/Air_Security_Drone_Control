# Air Security Drone Control

Air Security Drone Control is a modular situational-awareness platform for low-altitude urban airspace monitoring, threat assessment and operational response against drone-related incidents.

## Product positioning

This repository is framed as an enterprise-grade command-and-control system for:

- multisensor detection ingestion
- detection fusion and false-positive reduction
- dynamic threat scoring
- incident lifecycle management
- command center visibility

## Why this repo is strong

- clear domain split across services
- product-first command center prototype
- strong security and observability narrative
- realistic roadmap toward distributed event-driven architecture

## Current structure

```text
apps/
  command-center-prototype/
src/
  BuildingBlocks/
  Services/
  CommandCenter/
docs/
scripts/
docker-compose.yml
```

## MVP scope

- ingest multisensor detections
- fuse tracks
- calculate threat score
- create and update incidents
- expose a consolidated command center view

## Command Center prototype

The visual prototype currently validates the operational flow before the full backend is finalized:

- tactical map
- incidents view
- sensor overview
- replay mode
- modular live-mode integration path

Run locally:

```bash
cd apps/command-center-prototype
python3 -m http.server 4173
```

Open:

- `http://127.0.0.1:4173`

## Local backend run

```bash
dotnet restore
dotnet build AirSecurityDroneControl.sln
./scripts/run-p0-stack.sh
```

Useful scripts:

- `./scripts/run-p0-stack.sh`
- `./scripts/stop-p0-stack.sh`
- `./scripts/p0-e2e-demo.sh`
- `./scripts/p0-smoke-test.sh`

## Documentation

- `docs/architecture.md`
- `docs/drone-xone-airspace-blueprint.md`
- `docs/feature-matrix.md`
- `docs/p0-execution-backlog.md`
- `docs/local-runbook.md`
- `docs/event-flow.md`

## Security and observability

- mutable endpoints require `X-API-Key` and role headers in local dev
- each service exposes `GET /metrics/basic`
- local persistent runtime data is written to `.runtime/data/<service>`

## Next upgrades

1. add durable database persistence
2. introduce Kafka-based eventing
3. improve command center live integrations
4. harden auth and tenant-aware operational roles
