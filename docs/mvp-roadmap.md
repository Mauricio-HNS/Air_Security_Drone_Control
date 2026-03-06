# Roadmap MVP

## Fase 1 - Bootstrap (concluida)

- Solucao .NET e separacao por servicos.
- Contratos de dominio compartilhados.
- Endpoints iniciais para ingestao, fusao, scoring e incidentes.
- API de consolidacao para Command Center.

## Fase 2 - Integracao real

- Kafka para `DetectionReceived`, `TrackFused`, `ThreatAssessed`, `IncidentOpened`.
- Persistencia em Postgres/PostGIS.
- Cache de estado de alvo em Redis.

## Fase 3 - Operacao

- Rules Engine com politicas configuraveis por zona.
- Notificacao multicanal (webhook, SMS, e-mail).
- Replay tatico com timeline e evidencias.

## Fase 4 - Inteligencia historica

- Timeseries para comportamento e densidade de eventos.
- Deteccao de anomalia por local/hora.
- Ajuste de limiares por perfil regional (aprendizado local).
