# Fluxo de Eventos (MVP)

## Pipeline operacional

1. Sensor detecta objeto e envia para `SensorGateway.Api`.
2. Gateway gera `DetectionEvent`.
3. `Fusion.Api` recebe lote de deteccoes e cria `FusedTrack`.
4. `ThreatScoring.Api` calcula score e nivel com base em track + zona.
5. `Incidents.Api` abre incidente para risco relevante.
6. `CommandCenter.Api` consolida projecoes para visualizacao.

## Contratos usados

- Input inicial: `DetectionEvent`
- Correlacao: `FusedTrack`
- Decisao: `ThreatAssessment`
- Acao operacional: `IncidentCase`

## Criterios de regra sugeridos

- Se `ThreatLevel >= High`, abrir incidente automaticamente.
- Se confianca de track < 0.35, manter como observacao.
- Se multiplos sensores confirmam e persistencia > X segundos, elevar prioridade.
