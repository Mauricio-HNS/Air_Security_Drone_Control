# Air Security Drone Control

Plataforma modular de consciencia situacional aerea para deteccao, classificacao, fusao, tracking e resposta operacional contra ameacas em baixo espaco aereo urbano.

> Este README e a fonte oficial do estado do produto e sera atualizado continuamente a cada evolucao relevante.

## Objetivo do MVP

- Ingerir deteccoes multi-sensor.
- Fundir deteccoes para reduzir falso positivo.
- Calcular threat score dinamico.
- Abrir e atualizar incidentes.
- Expor visao consolidada para centro de comando.

## Stack inicial

- ASP.NET Core (.NET 9) para APIs de servico.
- C# para dominio e orquestracao.
- PostgreSQL + PostGIS (planejado).
- Redis para estado/cache rapido (planejado).
- Kafka para eventos (planejado).

## Estrutura do repositorio

```text
apps/
  command-center-prototype/
src/
  BuildingBlocks/
    AirSecurityDroneControl.BuildingBlocks/
  Services/
    SensorGateway.Api/
    Fusion.Api/
    ThreatScoring.Api/
    Incidents.Api/
  CommandCenter/
    CommandCenter.Api/
docs/
  architecture.md
  mvp-roadmap.md
  event-flow.md
docker-compose.yml
```

## Protótipo visual (produto primeiro)

Antes de fechar backend completo, o fluxo operacional esta sendo validado visualmente no Command Center:

- `apps/command-center-prototype/index.html`
- Mapa tatico, incidentes, sensores, replay e fluxo modular.

Execucao:

```bash
cd apps/command-center-prototype
python3 -m http.server 4173
```

Acesso:

- `http://127.0.0.1:4173`

## Contratos de API (OpenAPI)

Especificacoes iniciais dos modulos MVP:

- `docs/api/sensor-gateway.openapi.yaml`
- `docs/api/fusion.openapi.yaml`
- `docs/api/threat-scoring.openapi.yaml`
- `docs/api/incidents.openapi.yaml`
- `docs/api/command-center.openapi.yaml`

## Como rodar localmente

1. Restaurar e compilar:

```bash
dotnet restore
dotnet build AirSecurityDroneControl.sln
```

2. Subir servicos (terminais separados):

```bash
dotnet run --project src/Services/SensorGateway.Api --urls http://127.0.0.1:5101
dotnet run --project src/Services/Fusion.Api --urls http://127.0.0.1:5102
dotnet run --project src/Services/ThreatScoring.Api --urls http://127.0.0.1:5103
dotnet run --project src/Services/Incidents.Api --urls http://127.0.0.1:5104
dotnet run --project src/CommandCenter/CommandCenter.Api --urls http://127.0.0.1:5105
```

3. Verificacao rapida:

```bash
curl -s http://127.0.0.1:5101/health
curl -s http://127.0.0.1:5102/health
curl -s http://127.0.0.1:5103/health
curl -s http://127.0.0.1:5104/health
curl -s http://127.0.0.1:5105/health
```

## Estado atual

Este repositorio contem o bootstrap de arquitetura e endpoints de dominio para:

- `DetectionEvent`
- `FusedTrack`
- `ThreatAssessment`
- `IncidentCase`

Persistencia, mensageria e autenticacao estao planejadas para as proximas iteracoes.

## Evolucao continua (compromisso)

Cada nova iteracao deve:

1. Implementar uma capacidade real (nao apenas mock).
2. Atualizar este README com:
   - o que entrou,
   - como executar,
   - como validar,
   - o que ainda falta.
3. Manter rastreabilidade entre UI, contratos e servicos.

## Fases e criterio de pronto

### Fase 1 - Visual + Contratos (em andamento)
- [x] Prototipo de Command Center navegavel.
- [x] Contratos de dominio iniciais.
- [ ] Contrato HTTP formal (OpenAPI) por modulo.

### Fase 2 - Backend operacional MVP
- [ ] Pipeline real: ingestao -> fusao -> threat -> incidente.
- [ ] Persistencia de incidentes e tracks.
- [ ] Regras de alerta por zona configuravel.
- [ ] Endpoint de overview alimentando a UI sem dados simulados.

### Fase 3 - Integracao multisensor
- [ ] Adaptadores para camera, RF, radar e audio.
- [ ] Normalizacao de deteccoes por tipo de sensor.
- [ ] Correlacao temporal e geoespacial multi-fonte.

### Fase 4 - Producao
- [ ] Kafka (ou equivalente) para eventos.
- [ ] Observabilidade (OpenTelemetry + Prometheus + Grafana).
- [ ] Autenticacao, autorizacao e trilha de auditoria.
- [ ] Deploy conteinerizado e runbook operacional.

## Definicao de funcional (para este projeto)

O sistema sera considerado funcional quando:

1. Detectar eventos de pelo menos 2 tipos de sensor reais.
2. Confirmar alvo com fusao multi-sensor e confianca rastreavel.
3. Gerar threat score e incidente automaticamente por regra.
4. Exibir tudo em tempo real no Command Center.
5. Permitir replay completo de incidente com timeline e evidencias.
