# P0 Execution Backlog

Backlog tecnico para concluir o MVP operacional do Air Security Drone Control.

## Objetivo P0

Entregar fluxo completo:
`deteccao -> fusao -> threat -> incidente -> command center`
com 2 tipos de sensor e visualizacao em tempo real.

## Workstreams

### 1. SensorGateway.Api
- [ ] Endpoint de status de sensores
- [ ] Atualizacao de status por ultima deteccao
- [ ] Mapeamento basico de tipo de sensor por node
- [ ] Contrato para envio de status ao Command Center

Criterio de aceite:
- [ ] `GET /api/sensors/status` retorna estado consistente
- [ ] Sensor muda para online apos ingestao de deteccao

### 2. Fusion.Api
- [ ] Correlacao por lote com validacao minima
- [ ] Persistencia em memoria de tracks ativos
- [ ] Endpoint de listagem para consumo operacional

Criterio de aceite:
- [ ] `POST /api/fusion/fuse` retorna track valido
- [ ] `GET /api/fusion/tracks` retorna tracks recentes

### 3. ThreatScoring.Api
- [ ] Score dinamico com fatores explicitos
- [ ] Nivel de ameaca padronizado
- [ ] Endpoint historico de assessments

Criterio de aceite:
- [ ] `POST /api/threat/assess` classifica risco corretamente

### 4. Incidents.Api
- [ ] Abertura de incidente por assessment
- [ ] Atualizacao de status de incidente
- [ ] Listagem para visao operacional

Criterio de aceite:
- [ ] Incidente abre com dados de track e zona
- [ ] Workflow de status funciona sem erro

### 5. CommandCenter.Api
- [ ] Projection de tracks, threats e incidents
- [ ] Projection de status de sensores
- [ ] Endpoints diretos para frontend:
  - [ ] `GET /api/overview`
  - [ ] `GET /api/tracks`
  - [ ] `GET /api/incidents`
  - [ ] `GET /api/sensors/status`
  - [ ] `GET /api/replay/{incidentId}`

Criterio de aceite:
- [ ] Frontend pode consumir dados sem mock local

### 6. Operacao Local
- [ ] Script de start da stack P0
- [ ] Script de stop da stack P0
- [ ] Script de demo fim a fim com cURL

Criterio de aceite:
- [ ] Operador sobe stack com 1 comando
- [ ] Demo gera incidente e overview automaticamente

### 7. Documentacao
- [ ] README com comandos oficiais
- [ ] Contratos OpenAPI sincronizados com endpoints reais
- [ ] Runbook rapido de troubleshoot

Criterio de aceite:
- [ ] Qualquer dev consegue reproduzir o fluxo em maquina limpa

## Definition of Done P0

- [ ] Build da solucao verde (`dotnet build AirSecurityDroneControl.sln`)
- [ ] Demo fim a fim concluida com incidente aberto e projetado
- [ ] Command Center recebendo dados reais via API
- [ ] Documentacao atualizada e versionada
