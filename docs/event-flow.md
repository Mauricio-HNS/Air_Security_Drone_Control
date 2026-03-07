# Flujo de Eventos (MVP)

## Pipeline operativo

1. El sensor detecta un objeto y lo envia a `SensorGateway.Api`.
2. Gateway genera `DetectionEvent`.
3. `Fusion.Api` recibe un lote de detecciones y crea `FusedTrack`.
4. `ThreatScoring.Api` calcula score y nivel con base en track + zona.
5. `Incidents.Api` abre un incidente para riesgo relevante.
6. `CommandCenter.Api` consolida proyecciones para visualizacion.

## Contratos usados

- Input inicial: `DetectionEvent`
- Correlacion: `FusedTrack`
- Decision: `ThreatAssessment`
- Accion operativo: `IncidentCase`

## Criterios de reglas sugeridos

- Si `ThreatLevel >= High`, abrir incidente automaticamente.
- Si confianza del track < 0.35, mantener como observacion.
- Si multiples sensores confirman y la persistencia > X segundos, elevar prioridad.
