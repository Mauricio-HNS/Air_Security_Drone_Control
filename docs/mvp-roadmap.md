# Hoja de Ruta MVP

## Fase 1 - Bootstrap (completada)

- Solucion .NET y separacao por servicios.
- Contratos de dominio compartilhados.
- Endpoints iniciales para ingesta, fusion, scoring y incidentes.
- API de consolidacao para Command Center.

## Fase 2 - Integracion real

- Kafka para `DetectionReceived`, `TrackFused`, `ThreatAssessed`, `IncidentOpened`.
- Persistencia en Postgres/PostGIS.
- Cache de estado de objetivo en Redis.

## Fase 3 - Operacion

- Rules Engine con politicas configurables por zona.
- Notificacao multicanal (webhook, SMS, e-mail).
- Replay tatico con timeline y evidencias.

## Fase 4 - Inteligencia historica

- Timeseries para comportamento y densidade de eventos.
- Deteccao de anomalia por local/hora.
- Ajuste de limiares por perfil regional (aprendizado local).
