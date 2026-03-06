# Arquitetura - Air Security Drone Control

## Servicos MVP

- `SensorGateway.Api`: ingestao de deteccoes de camera, RF, audio e radar.
- `Fusion.Api`: fusao de multiplas deteccoes em um track unificado.
- `ThreatScoring.Api`: pontuacao de risco dinamica por track e zona.
- `Incidents.Api`: abertura e ciclo de vida de incidentes.
- `CommandCenter.Api`: visao consolidada para painel operacional.

## Building Blocks

Contratos compartilhados em `AirSecurityCity.BuildingBlocks`:

- `DetectionEvent`
- `FusedTrack`
- `ThreatAssessment`
- `IncidentCase`
- `ProtectedZone`
- enums de classificacao, nivel de ameaca e status.

## Principios de projeto

- Design orientado a eventos.
- Servicos stateless sempre que possivel.
- Estado operacional curto em cache distribuido.
- Persistencia geoespacial para trilhas e zonas protegidas.
- Observabilidade como requisito de primeira classe.

## Evolucao recomendada (producao)

1. Substituir chamadas diretas por Kafka (topicos por bounded context).
2. Introduzir Postgres + PostGIS em servicos de tracking/incidentes.
3. Adicionar autenticacao (OIDC/JWT) e RBAC.
4. Incluir trilha de auditoria imutavel para evidencias.
5. Expor telemetria com OpenTelemetry + Prometheus.
