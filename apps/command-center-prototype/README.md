# Command Center Prototype (Visual)

Prototipo visual para validar producto, operacion y flujo de decision antes de la implementacion completa del backend.

## Como ejecutar

Abre el archivo:

- `apps/command-center-prototype/index.html`

O servirlo localmente:

```bash
cd apps/command-center-prototype
python3 -m http.server 4173
```

Accede:

- `http://127.0.0.1:4173`

## Lo que ya valida

- Mapa tactico con zonas y trayectorias simuladas.
- Panel de incidentes con severidad.
- Estado de sensores y calidad de señal.
- Replay de evolucion del incidente.
- Pipeline modular end-to-end (Gateway -> Fusion -> Threat -> Rules -> Incident -> Command Center).
- Modo Live opcional consumiendo APIs reales del backend local.

## Decision para backend (derivada de la UI)

Este prototipo define los contratos minimos que el backend debe proveer:

1. `GET /api/overview`
2. `GET /api/incidents`
3. `GET /api/tracks`
4. `GET /api/sensors/status`
5. `GET /api/replay/{incidentId}`
6. `POST /api/rules/simulate` (para pruebas operativas)

## Modo Live

Con el backend local ejecutandose en el puerto `5105`, haz clic en `Modo Live: OFF` para cambiar a `Modo Live: ON`.

El prototipo consulta:

- `GET /api/tracks`
- `GET /api/incidents`
- `GET /api/sensors/status`
