# Local Runbook (P0)

## Fluxo rapido

1. `dotnet build AirSecurityDroneControl.sln`
2. `./scripts/run-p0-stack.sh`
3. `./scripts/p0-e2e-demo.sh`
4. `./scripts/stop-p0-stack.sh`

## Endpoints locais

- Sensor Gateway: `http://127.0.0.1:5101`
- Fusion: `http://127.0.0.1:5102`
- Threat Scoring: `http://127.0.0.1:5103`
- Incidents: `http://127.0.0.1:5104`
- Command Center: `http://127.0.0.1:5105`

## Logs e processos

- Logs: `.runtime/logs/`
- PIDs: `.runtime/pids.txt`

## Troubleshooting

### Servico nao sobe

- Verifique log do servico em `.runtime/logs/<service>.log`
- Rode build novamente:
  - `dotnet build AirSecurityDroneControl.sln`

### Porta ocupada

- Mate o processo que ocupa a porta e rode de novo
- Ou altere a porta no script `scripts/run-p0-stack.sh`

### Demo falha em algum passo

- Rode health checks manuais:
  - `curl -s http://127.0.0.1:5101/health`
  - `curl -s http://127.0.0.1:5102/health`
  - `curl -s http://127.0.0.1:5103/health`
  - `curl -s http://127.0.0.1:5104/health`
  - `curl -s http://127.0.0.1:5105/health`
