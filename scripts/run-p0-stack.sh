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
  local project="$2"
  local port="$3"
  local log_file="$LOG_DIR/${name}.log"

  nohup dotnet run --no-build --no-launch-profile --project "$ROOT_DIR/$project" --urls "http://127.0.0.1:${port}" >"$log_file" 2>&1 &
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

start_service "sensor-gateway" "src/Services/SensorGateway.Api/SensorGateway.Api.csproj" 5101
start_service "fusion" "src/Services/Fusion.Api/Fusion.Api.csproj" 5102
start_service "threat-scoring" "src/Services/ThreatScoring.Api/ThreatScoring.Api.csproj" 5103
start_service "incidents" "src/Services/Incidents.Api/Incidents.Api.csproj" 5104
start_service "command-center" "src/CommandCenter/CommandCenter.Api/CommandCenter.Api.csproj" 5105
start_service "rules-engine" "src/Services/RulesEngine.Api/RulesEngine.Api.csproj" 5106
start_service "notifications" "src/Services/Notifications.Api/Notifications.Api.csproj" 5107
start_service "evidence" "src/Services/Evidence.Api/Evidence.Api.csproj" 5108

echo ""
echo "P0 stack is running."
echo "PID file: $PID_FILE"
echo "Logs: $LOG_DIR"
