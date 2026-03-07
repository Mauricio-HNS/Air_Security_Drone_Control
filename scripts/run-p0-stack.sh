#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUNTIME_DIR="$ROOT_DIR/.runtime"
LOG_DIR="$RUNTIME_DIR/logs"
PID_FILE="$RUNTIME_DIR/pids.txt"

mkdir -p "$LOG_DIR"
: > "$PID_FILE"

start_service() {
  local name="$1"
  local dll_path="$2"
  local port="$3"
  local log_file="$LOG_DIR/${name}.log"

  if [[ ! -f "$ROOT_DIR/$dll_path" ]]; then
    echo "[ERROR] Missing binary: $dll_path"
    echo "Build first: dotnet build AirSecurityDroneControl.sln"
    exit 1
  fi

  nohup dotnet "$ROOT_DIR/$dll_path" --urls "http://127.0.0.1:${port}" >"$log_file" 2>&1 &
  local pid=$!

  echo "${name}:${pid}:${port}" >> "$PID_FILE"
  for _ in {1..20}; do
    if ! kill -0 "$pid" 2>/dev/null; then
      echo "[ERROR] ${name} crashed during startup. Check log: ${log_file}"
      exit 1
    fi

    if curl -fsS "http://127.0.0.1:${port}/health" >/dev/null 2>&1; then
      echo "[OK] ${name} started on http://127.0.0.1:${port} (pid ${pid})"
      return
    fi

    sleep 0.5
  done

  echo "[ERROR] ${name} did not become healthy in time. Check log: ${log_file}"
  exit 1
}

start_service "sensor-gateway" "src/Services/SensorGateway.Api/bin/Debug/net9.0/SensorGateway.Api.dll" 5101
start_service "fusion" "src/Services/Fusion.Api/bin/Debug/net9.0/Fusion.Api.dll" 5102
start_service "threat-scoring" "src/Services/ThreatScoring.Api/bin/Debug/net9.0/ThreatScoring.Api.dll" 5103
start_service "incidents" "src/Services/Incidents.Api/bin/Debug/net9.0/Incidents.Api.dll" 5104
start_service "command-center" "src/CommandCenter/CommandCenter.Api/bin/Debug/net9.0/CommandCenter.Api.dll" 5105
start_service "rules-engine" "src/Services/RulesEngine.Api/bin/Debug/net9.0/RulesEngine.Api.dll" 5106
start_service "notifications" "src/Services/Notifications.Api/bin/Debug/net9.0/Notifications.Api.dll" 5107
start_service "evidence" "src/Services/Evidence.Api/bin/Debug/net9.0/Evidence.Api.dll" 5108

echo ""
echo "P0 stack is running."
echo "PID file: $PID_FILE"
echo "Logs: $LOG_DIR"
