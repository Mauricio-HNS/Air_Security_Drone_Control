#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PID_FILE="$ROOT_DIR/.runtime/pids.txt"

if [[ ! -f "$PID_FILE" ]]; then
  echo "No se encontro archivo PID. Probablemente la stack no esta en ejecucion."
  exit 0
fi

while IFS=: read -r name pid port; do
  if [[ -n "${pid:-}" ]] && kill -0 "$pid" 2>/dev/null; then
    kill "$pid" || true
    echo "[OK] stopped ${name} (pid ${pid})"
  else
    echo "[SKIP] ${name} already stopped"
  fi
done < "$PID_FILE"

rm -f "$PID_FILE"
echo "Pila detenida."
