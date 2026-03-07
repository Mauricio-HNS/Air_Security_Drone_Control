#!/usr/bin/env bash
set -euo pipefail

API_KEY="dev-local-key"
ROLE="operator"

for url in \
  http://127.0.0.1:5101 \
  http://127.0.0.1:5102 \
  http://127.0.0.1:5103 \
  http://127.0.0.1:5104 \
  http://127.0.0.1:5105 \
  http://127.0.0.1:5106 \
  http://127.0.0.1:5107 \
  http://127.0.0.1:5108; do
  curl -fsS "$url/health" >/dev/null
  echo "[OK] health: $url"
done

code=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://127.0.0.1:5102/api/fusion/fuse -H "Content-Type: application/json" -d '{"detections":[]}')
if [[ "$code" != "401" ]]; then
  echo "[ERROR] expected 401 without API key, got $code"
  exit 1
fi
echo "[OK] seguridad bloquea POST sin autenticacion"

code=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://127.0.0.1:5102/api/fusion/fuse \
  -H "X-API-Key: $API_KEY" -H "X-Role: $ROLE" -H "Content-Type: application/json" -d '{"detections":[]}')
if [[ "$code" != "400" ]]; then
  echo "[ERROR] expected 400 with valid auth and invalid payload, got $code"
  exit 1
fi
echo "[OK] seguridad permite solicitud autenticada"

echo "Smoke test superado."
