#!/usr/bin/env bash
set -euo pipefail

base_sensor="http://127.0.0.1:5101"
base_fusion="http://127.0.0.1:5102"
base_threat="http://127.0.0.1:5103"
base_incidents="http://127.0.0.1:5104"
base_command="http://127.0.0.1:5105"
base_rules="http://127.0.0.1:5106"
base_notifications="http://127.0.0.1:5107"
base_evidence="http://127.0.0.1:5108"

check_health() {
  local name="$1"
  local url="$2"
  curl -fsS "$url/health" >/dev/null
  echo "[OK] $name is healthy"
}

check_health "sensor-gateway" "$base_sensor"
check_health "fusion" "$base_fusion"
check_health "threat-scoring" "$base_threat"
check_health "incidents" "$base_incidents"
check_health "command-center" "$base_command"
check_health "rules-engine" "$base_rules"
check_health "notifications" "$base_notifications"
check_health "evidence" "$base_evidence"

echo "[1/8] Ingest detections"
d1=$(curl -fsS -X POST "$base_sensor/api/sensors/detections" \
  -H "Content-Type: application/json" \
  -d '{"sensorNodeId":"CAM-01","sensorType":"Camera","classification":"Drone","latitude":-23.55052,"longitude":-46.633308,"altitudeMeters":120,"confidence":0.86,"headingDegrees":45,"speedMps":12.5}')

d2=$(curl -fsS -X POST "$base_sensor/api/sensors/detections" \
  -H "Content-Type: application/json" \
  -d '{"sensorNodeId":"RF-02","sensorType":"Rf","classification":"Drone","latitude":-23.55074,"longitude":-46.632901,"altitudeMeters":118,"confidence":0.81,"headingDegrees":47,"speedMps":13.1}')

echo "[2/8] Fuse detections into track"
fuse_payload=$(python3 - "$d1" "$d2" <<'PY'
import json,sys
print(json.dumps({"detections":[json.loads(sys.argv[1]), json.loads(sys.argv[2])]}))
PY
)
track=$(curl -fsS -X POST "$base_fusion/api/fusion/fuse" -H "Content-Type: application/json" -d "$fuse_payload")

echo "[3/8] Assess threat"
assess_payload=$(python3 - "$track" <<'PY'
import json,sys
track=json.loads(sys.argv[1])
zone={
  "zoneId":"11111111-1111-1111-1111-111111111111",
  "name":"Zona A",
  "center":{"latitude":-23.5506,"longitude":-46.6330,"altitudeMeters":100},
  "radiusMeters":450,
  "sensitive":True
}
print(json.dumps({"track":track,"zone":zone}))
PY
)
threat=$(curl -fsS -X POST "$base_threat/api/threat/assess" -H "Content-Type: application/json" -d "$assess_payload")

echo "[4/8] Open incident"
incident_payload=$(python3 - "$track" "$threat" <<'PY'
import json,sys
track=json.loads(sys.argv[1])
threat=json.loads(sys.argv[2])
print(json.dumps({
  "trackId":track["trackId"],
  "threatAssessmentId":threat["assessmentId"],
  "zone":"Zona A"
}))
PY
)
incident=$(curl -fsS -X POST "$base_incidents/api/incidents/open" -H "Content-Type: application/json" -d "$incident_payload")

echo "[5/10] Evaluate rules and project to command center"
rule=$(curl -fsS -X POST "$base_rules/api/rules/simulate" -H "Content-Type: application/json" -d "$(python3 - "$threat" <<'PY'
import json,sys
t=json.loads(sys.argv[1])
print(json.dumps({"zone":"Zona A","threatScore":t["score"],"detectionCount":2}))
PY
)")

curl -fsS -X POST "$base_command/api/projections/tracks" -H "Content-Type: application/json" -d "$track" >/dev/null
curl -fsS -X POST "$base_command/api/projections/threats" -H "Content-Type: application/json" -d "$threat" >/dev/null
curl -fsS -X POST "$base_command/api/projections/incidents" -H "Content-Type: application/json" -d "$incident" >/dev/null

echo "[6/10] Project sensor status"
sensor_status=$(curl -fsS "$base_sensor/api/sensors/status")
python3 - "$sensor_status" "$base_command" <<'PY'
import json,sys,urllib.request
sensors=json.loads(sys.argv[1])
base=sys.argv[2]
for sensor in sensors:
  data=json.dumps(sensor).encode("utf-8")
  req=urllib.request.Request(
    f"{base}/api/projections/sensors",
    data=data,
    headers={"Content-Type":"application/json"},
    method="POST"
  )
  urllib.request.urlopen(req).read()
print("projected", len(sensors), "sensor status entries")
PY

echo "[7/10] Store evidence"
evidence=$(curl -fsS -X POST "$base_evidence/api/evidence" -H "Content-Type: application/json" -d "$(python3 - "$incident" "$threat" <<'PY'
import json,sys
inc=json.loads(sys.argv[1]); thr=json.loads(sys.argv[2])
print(json.dumps({
  "incidentId": inc["incidentId"],
  "evidenceType": "telemetry",
  "content": f"Threat score={thr['score']} summary={thr['summary']}"
}))
PY
)")

echo "[8/10] Send notification"
notification=$(curl -fsS -X POST "$base_notifications/api/notifications/send" -H "Content-Type: application/json" -d "$(python3 - "$incident" "$rule" <<'PY'
import json,sys
inc=json.loads(sys.argv[1]); rule=json.loads(sys.argv[2])
print(json.dumps({
  "incidentId": inc["incidentId"],
  "channel": "webhook",
  "severity": "HIGH",
  "target": "soc://default",
  "message": f\"Incident {inc['incidentId']} action {rule['action']} - {rule['reason']}\"
}))
PY
)")

echo "[9/10] Fetch overview"
overview=$(curl -fsS "$base_command/api/overview")

incident_id=$(python3 - "$incident" <<'PY'
import json,sys
print(json.loads(sys.argv[1])["incidentId"])
PY
)

echo "[10/10] Fetch replay"
replay=$(curl -fsS "$base_command/api/replay/${incident_id}")

echo ""
echo "=== TRACK ==="
echo "$track"
echo ""
echo "=== THREAT ==="
echo "$threat"
echo ""
echo "=== INCIDENT ==="
echo "$incident"
echo ""
echo "=== RULE DECISION ==="
echo "$rule"
echo ""
echo "=== EVIDENCE ==="
echo "$evidence"
echo ""
echo "=== NOTIFICATION ==="
echo "$notification"
echo ""
echo "=== OVERVIEW ==="
echo "$overview"
echo ""
echo "=== REPLAY ==="
echo "$replay"
