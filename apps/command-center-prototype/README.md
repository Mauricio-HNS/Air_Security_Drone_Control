# Command Center Prototype (Visual)

Protótipo visual para validar produto, operação e fluxo de decisão antes da implementação completa de backend.

## Como executar

Basta abrir o arquivo:

- `apps/command-center-prototype/index.html`

Ou servir localmente:

```bash
cd apps/command-center-prototype
python3 -m http.server 4173
```

Acesse:

- `http://127.0.0.1:4173`

## O que já valida

- Mapa tático com zonas e trilhas simuladas.
- Painel de incidentes com severidade.
- Estado dos sensores e qualidade de sinal.
- Replay de evolução do incidente.
- Pipeline modular fim a fim (Gateway -> Fusion -> Threat -> Rules -> Incident -> Command Center).
- Modo Live opcional consumindo APIs reais do backend local.

## Decisão para backend (derivada da UI)

Este protótipo fixa os contratos mínimos que o backend precisa fornecer:

1. `GET /api/overview`
2. `GET /api/incidents`
3. `GET /api/tracks`
4. `GET /api/sensors/status`
5. `GET /api/replay/{incidentId}`
6. `POST /api/rules/simulate` (para testes operacionais)

## Modo Live

Com o backend local rodando na porta `5105`, clique em `Modo Live: OFF` para alternar para `Modo Live: ON`.

O prototipo passa a consultar:

- `GET /api/tracks`
- `GET /api/incidents`
- `GET /api/sensors/status`
