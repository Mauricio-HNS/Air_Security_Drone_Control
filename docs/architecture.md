# Arquitectura - Air Security Drone Control

## Servicos MVP

- `SensorGateway.Api`: ingesta de detecciones de camara, RF, audio y radar.
- `Fusion.Api`: fusion de multiplas detecciones en um track unificado.
- `ThreatScoring.Api`: pontuacao de risco dinamica por track y zona.
- `Incidents.Api`: apertura y ciclo de vida de incidentes.
- `CommandCenter.Api`: vision consolidada para painel operativo.

## Building Blocks

Contratos compartilhados en `AirSecurityDroneControl.BuildingBlocks`:

- `DetectionEvent`
- `FusedTrack`
- `ThreatAssessment`
- `IncidentCase`
- `ProtectedZone`
- enums de clasificacion, nivel de ameaca y status.

## Principios de projeto

- Design orientado a eventos.
- Servicos stateless sempre que possivel.
- Estado operativo curto en cache distribuido.
- Persistencia geoespacial para trayectorias y zonas protegidas.
- Observabilidad como requisito de primera clase.

## Evolucao recomendada (produccion)

1. Substituir chamadas diretas por Kafka (topicos por bounded context).
2. Introducir Postgres + PostGIS en servicios de tracking/incidentes.
3. Adicionar autenticacion (OIDC/JWT) y RBAC.
4. Incluir trazabilidad de auditoria inmutable para evidencias.
5. Expor telemetria con OpenTelemetry + Prometheus.
