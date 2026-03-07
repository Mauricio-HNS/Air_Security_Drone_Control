# Local Runbook (P0)

## Flujo rapido

1. `dotnet build AirSecurityDroneControl.sln`
2. `./scripts/run-p0-stack.sh`
3. `./scripts/p0-e2e-demo.sh`
4. `./scripts/stop-p0-stack.sh`

## Headers para endpoints mutables

- `X-API-Key: dev-local-key`
- `X-Role: operator` (o `admin` para rutas administrativas)

## Endpoints locais

- Sensor Gateway: `http://127.0.0.1:5101`
- Fusion: `http://127.0.0.1:5102`
- Threat Scoring: `http://127.0.0.1:5103`
- Incidents: `http://127.0.0.1:5104`
- Command Center: `http://127.0.0.1:5105`
- Rules Engine: `http://127.0.0.1:5106`
- Notifications: `http://127.0.0.1:5107`
- Evidence: `http://127.0.0.1:5108`

## Logs y processos

- Logs: `.runtime/logs/`
- PIDs: `.runtime/pids.txt`

## Troubleshooting

### Servico no sube

- Verifica el log del servicio en `.runtime/logs/<service>.log`
- Ejecuta build nuevamente:
  - `dotnet build AirSecurityDroneControl.sln`

### Puerto ocupado

- Mata el proceso que ocupa el puerto y ejecuta de nuevo
- Ou altere a porta no script `scripts/run-p0-stack.sh`

### La demo falla en algun paso

- Ejecuta health checks manuales:
  - `curl -s http://127.0.0.1:5101/health`
  - `curl -s http://127.0.0.1:5102/health`
  - `curl -s http://127.0.0.1:5103/health`
  - `curl -s http://127.0.0.1:5104/health`
  - `curl -s http://127.0.0.1:5105/health`
  - `curl -s http://127.0.0.1:5106/health`
  - `curl -s http://127.0.0.1:5107/health`
  - `curl -s http://127.0.0.1:5108/health`

### Inspecionar metrica basica

- Exemplo:
  - `curl -s http://127.0.0.1:5105/metrics/basic`
