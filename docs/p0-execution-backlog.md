# P0 Execution Backlog

Backlog tecnico para completar el MVP operativo de Air Security Drone Control.

## Objetivo P0

Entregar flujo completo:
`deteccion -> fusion -> threat -> incidente -> command center`
com 2 tipos de sensor y visualizacion en tiempo real.

## Workstreams

### 1. SensorGateway.Api
- [ ] Endpoint de estado de sensores
- [ ] Atualizacao de status por ultima deteccion
- [ ] Mapeamento basico de tipo de sensor por node
- [ ] Contrato para envio de status ao Command Center

Criterio de aceptacion:
- [ ] `GET /api/sensors/status` retorna estado consistente
- [ ] Sensor muda para online apos ingesta de deteccion

### 2. Fusion.Api
- [ ] Correlacion por lote con validacion minima
- [ ] Persistencia en memoria de tracks ativos
- [ ] Endpoint de listagem para consumo operativo

Criterio de aceptacion:
- [ ] `POST /api/fusion/fuse` retorna track valido
- [ ] `GET /api/fusion/tracks` retorna tracks recentes

### 3. ThreatScoring.Api
- [ ] Score dinamico con fatores explicitos
- [ ] Nivel de ameaca padronizado
- [ ] Endpoint historico de assessments

Criterio de aceptacion:
- [ ] `POST /api/threat/assess` classifica risco corretamente

### 4. Incidents.Api
- [ ] Apertura de incidente por assessment
- [ ] Atualizacao de status de incidente
- [ ] Listagem para vision operativo

Criterio de aceptacion:
- [ ] Incidente abre con datos de track y zona
- [ ] Workflow de status funciona sem erro

### 5. CommandCenter.Api
- [ ] Projection de tracks, threats y incidents
- [ ] Projection de status de sensores
- [ ] Endpoints diretos para frontend:
  - [ ] `GET /api/overview`
  - [ ] `GET /api/tracks`
  - [ ] `GET /api/incidents`
  - [ ] `GET /api/sensors/status`
  - [ ] `GET /api/replay/{incidentId}`

Criterio de aceptacion:
- [ ] Frontend pode consumir datos sem mock local

### 6. Operacion Local
- [ ] Script de inicio de la stack P0
- [ ] Script de parada de la stack P0
- [ ] Script de demo end-to-end con cURL

Criterio de aceptacion:
- [ ] Operador levanta la stack con 1 comando
- [ ] Demo genera incidente y overview automaticamente

### 7. Documentacao
- [ ] README con comandos oficiales
- [ ] Contratos OpenAPI sincronizados con endpoints reales
- [ ] Runbook rapido de troubleshoot

Criterio de aceptacion:
- [ ] Qualquer dev consegue reproduzir o flujo en maquina limpa

## Definition of Done P0

- [ ] Build de la solucion en verde (`dotnet build AirSecurityDroneControl.sln`)
- [ ] Demo end-to-end completada con incidente abierto y proyectado
- [ ] Command Center recebendo datos reales via API
- [ ] Documentacao atualizada y versionada
