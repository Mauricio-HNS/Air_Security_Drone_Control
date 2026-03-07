const API_BASE = "http://127.0.0.1:5105";

const state = {
  liveMode: false,
  tracks: [
    { id: "T-101", x: 22, y: 48, threat: "MEDIUM", confidence: 0.73, speed: 8.2 },
    { id: "T-102", x: 52, y: 30, threat: "LOW", confidence: 0.89, speed: 3.4 },
    { id: "T-103", x: 66, y: 44, threat: "HIGH", confidence: 0.84, speed: 12.7 }
  ],
  incidents: [
    {
      id: "INC-00045",
      zone: "Zona A",
      level: "HIGH",
      createdAt: "04:02:12",
      resumen: "Track T-103 con aproximacion persistente y confirmacion multisensor."
    }
  ],
  sensors: [
    { id: "CAM-SP-01", type: "Camera", status: "online", quality: 91 },
    { id: "RF-SP-03", type: "RF Sensor", status: "online", quality: 88 },
    { id: "MIC-SP-02", type: "Acoustic", status: "warn", quality: 64 },
    { id: "RAD-SP-01", type: "Radar", status: "online", quality: 86 },
    { id: "API-CITY-01", type: "External API", status: "online", quality: 94 },
    { id: "CAM-SP-05", type: "Camera", status: "offline", quality: 0 }
  ],
  replay: [
    "T+00s: RF sensor detecta firma en el borde de la Zona A.",
    "T+08s: Camara confirma objeto pequeno en movimiento constante.",
    "T+14s: Fusion agrupa detecciones en T-103 (confianza 0.81).",
    "T+20s: Threat scoring sube a HIGH por proximidad y persistencia.",
    "T+27s: Incident service abre INC-00045 y notifica al operador."
  ]
};

const views = document.querySelectorAll(".view");
const navBtns = document.querySelectorAll(".nav-btn");
const mapCanvas = document.getElementById("map-canvas");
const incidentList = document.getElementById("incident-list");
const sensorGrid = document.getElementById("sensor-grid");
const replaySlider = document.getElementById("replay-slider");
const replayOutput = document.getElementById("replay-output");
const simulateBtn = document.getElementById("simulate-btn");
const connectBtn = document.getElementById("connect-btn");
const radarCanvas = document.getElementById("radar-canvas");
const radarTrackCount = document.getElementById("radar-track-count");
const radarMode = document.getElementById("radar-mode");

const title = document.getElementById("view-title");
const subtitle = document.getElementById("view-subtitle");
const opsStatus = document.getElementById("ops-status");
const opsTime = document.getElementById("ops-time");
const radarState = { angle: 0 };

const titles = {
  overview: ["Mapa Tactico", "Vista en tiempo real de tracks, zonas y amenaza"],
  incidents: ["Gestion de Incidentes", "Casos activos, evidencias y estado operativo"],
  sensors: ["Panel de Sensores", "Salud y calidad de la red de deteccion"],
  replay: ["Replay Tactico", "Analisis temporal para post-incidente y auditoria"],
  architecture: ["Arquitectura Modular", "Pipeline de decision operativa por capas"]
};

function setClock() {
  const now = new Date();
  opsTime.textContent = now.toLocaleTimeString("pt-BR");
}

function clampToMap(value, min, max, fallback) {
  if (max <= min) return fallback;
  const pct = ((value - min) / (max - min)) * 70 + 15;
  return Math.min(90, Math.max(10, pct));
}

function normalizeTrackPosition(track, bounds) {
  if (typeof track.x === "number" && typeof track.y === "number") {
    return { x: track.x, y: track.y };
  }

  if (!track.position) {
    return { x: 50, y: 50 };
  }

  const x = clampToMap(track.position.longitude, bounds.minLon, bounds.maxLon, 50);
  const y = clampToMap(track.position.latitude, bounds.minLat, bounds.maxLat, 50);
  return { x, y };
}

function renderMap() {
  mapCanvas.querySelectorAll(".track").forEach((node) => node.remove());

  const bounds = computeBounds(state.tracks);

  state.tracks.forEach((track) => {
    const pos = normalizeTrackPosition(track, bounds);
    const el = document.createElement("div");
    el.className = `track ${track.threat === "HIGH" || track.threat === "CRITICAL" ? "high" : ""}`;
    el.style.left = `${pos.x}%`;
    el.style.top = `${pos.y}%`;
    el.title = `${track.id} | ${track.threat} | conf ${Math.round(track.confidence * 100)}%`;
    mapCanvas.appendChild(el);
  });

  const avgConfidence = state.tracks.length
    ? Math.round((state.tracks.reduce((a, b) => a + b.confidence, 0) / state.tracks.length) * 100)
    : 0;

  document.getElementById("kpi-tracks").textContent = String(state.tracks.length);
  document.getElementById("kpi-incidents").textContent = String(state.incidents.length);
  document.getElementById("kpi-threat").textContent = state.tracks.some((t) => t.threat === "HIGH" || t.threat === "CRITICAL")
    ? "HIGH"
    : "MEDIUM";
  document.getElementById("kpi-confidence").textContent = `${avgConfidence}%`;

  renderRadar(bounds);
}

function renderIncidents() {
  incidentList.innerHTML = "";

  state.incidents.forEach((item) => {
    const el = document.createElement("article");
    el.className = `incident ${item.level === "HIGH" || item.level === "CRITICAL" ? "high" : ""}`;
    el.innerHTML = `
      <strong>${item.id} • ${item.level}</strong>
      <p>${item.resumen}</p>
      <small>Zona: ${item.zone} • Hora: ${item.createdAt}</small>
    `;
    incidentList.appendChild(el);
  });
}

function renderSensors() {
  sensorGrid.innerHTML = "";

  state.sensors.forEach((sensor) => {
    const el = document.createElement("article");
    el.className = "sensor";
    el.innerHTML = `
      <strong><span class="dot ${sensor.status}"></span>${sensor.id}</strong>
      <p>${sensor.type}</p>
      <small>Calidad de sinal: ${sensor.quality}%</small>
    `;
    sensorGrid.appendChild(el);
  });
}

function renderReplay() {
  const idx = Math.max(0, Math.min(state.replay.length - 1, Math.floor(replaySlider.value / 25)));

  replayOutput.innerHTML = state.replay
    .map((line, i) => `<div style="opacity:${i <= idx ? 1 : 0.33}">${line}</div>`)
    .join("");
}

function renderPipeline() {
  const pipeline = document.getElementById("pipeline");
  const steps = [
    ["Sensor Gateway", "Recibe camara, RF, acustico, radar y API externa."],
    ["Detection + Classification", "Clasifica dron, pajaro, helicoptero, ruido o desconocido."],
    ["Fusion Engine", "Correlaciona señales para confirmar objetivo y reducir falsos positivos."],
    ["Tracking", "Mantiene trayectoria con posicion, velocidad y ruta probable."],
    ["Threat Scoring", "Calcula score dinamico por proximidad, persistencia y zona sensible."],
    ["Rules Engine", "Define alerta amarilla/roja y disparador de incidente."],
    ["Gestion de Incidentes", "Abre, actualiza y cierra casos con evidencias y auditoria."],
    ["Command Center", "Presenta mapa, timeline, incidentes y replay para operacion."]
  ];

  pipeline.innerHTML = steps
    .map((item, i) => `<div class="step"><strong>${i + 1}. ${item[0]}</strong><p>${item[1]}</p></div>`)
    .join("");
}

function computeBounds(tracks) {
  const coords = tracks.map((t) => t.position).filter(Boolean);
  return {
    minLat: Math.min(...coords.map((c) => c.latitude), -23.7),
    maxLat: Math.max(...coords.map((c) => c.latitude), -23.4),
    minLon: Math.min(...coords.map((c) => c.longitude), -46.8),
    maxLon: Math.max(...coords.map((c) => c.longitude), -46.4)
  };
}

function getTrackPlot(track, bounds) {
  if (typeof track.x === "number" && typeof track.y === "number") {
    return { xPct: track.x, yPct: track.y };
  }

  const pos = normalizeTrackPosition(track, bounds);
  return { xPct: pos.x, yPct: pos.y };
}

function renderRadar(bounds) {
  if (!radarCanvas) {
    return;
  }

  const ctx = radarCanvas.getContext("2d");
  const w = radarCanvas.width;
  const h = radarCanvas.height;
  const cx = w / 2;
  const cy = h / 2;
  const radius = Math.min(w, h) * 0.42;

  ctx.clearRect(0, 0, w, h);

  ctx.fillStyle = "rgba(2, 18, 30, 0.92)";
  ctx.fillRect(0, 0, w, h);

  for (let i = 1; i <= 4; i += 1) {
    ctx.beginPath();
    ctx.strokeStyle = `rgba(120, 220, 255, ${0.12 + i * 0.06})`;
    ctx.lineWidth = 1;
    ctx.arc(cx, cy, (radius / 4) * i, 0, Math.PI * 2);
    ctx.stroke();
  }

  ctx.beginPath();
  ctx.strokeStyle = "rgba(120, 220, 255, 0.2)";
  ctx.moveTo(cx - radius, cy);
  ctx.lineTo(cx + radius, cy);
  ctx.moveTo(cx, cy - radius);
  ctx.lineTo(cx, cy + radius);
  ctx.stroke();

  const sweepStart = radarState.angle;
  const sweepEnd = sweepStart + Math.PI / 3.4;
  const gradient = ctx.createRadialGradient(cx, cy, 0, cx, cy, radius);
  gradient.addColorStop(0, "rgba(60, 255, 188, 0.32)");
  gradient.addColorStop(1, "rgba(60, 255, 188, 0)");
  ctx.fillStyle = gradient;
  ctx.beginPath();
  ctx.moveTo(cx, cy);
  ctx.arc(cx, cy, radius, sweepStart, sweepEnd);
  ctx.closePath();
  ctx.fill();

  const pulse = 2.6 + Math.sin(Date.now() / 220) * 1.1;
  state.tracks.forEach((track) => {
    const { xPct, yPct } = getTrackPlot(track, bounds);
    const tx = ((xPct / 100) - 0.5) * (radius * 2) + cx;
    const ty = ((yPct / 100) - 0.5) * (radius * 2) + cy;
    const high = track.threat === "HIGH" || track.threat === "CRITICAL";

    ctx.beginPath();
    ctx.fillStyle = high ? "rgba(255, 108, 64, 0.95)" : "rgba(63, 235, 255, 0.95)";
    ctx.arc(tx, ty, high ? 4.6 : 3.8, 0, Math.PI * 2);
    ctx.fill();

    ctx.beginPath();
    ctx.strokeStyle = high ? "rgba(255, 108, 64, 0.44)" : "rgba(63, 235, 255, 0.38)";
    ctx.lineWidth = 1.25;
    ctx.arc(tx, ty, 9 + pulse, 0, Math.PI * 2);
    ctx.stroke();
  });

  radarTrackCount.textContent = `${state.tracks.length} tracks`;
  radarMode.textContent = state.liveMode ? "Modo: Seguimiento en vivo" : "Modo: Simulacion tactica";
}

function radarTick() {
  radarState.angle += 0.024;
  if (radarState.angle > Math.PI * 2) {
    radarState.angle = 0;
  }
  renderRadar(computeBounds(state.tracks));
  window.requestAnimationFrame(radarTick);
}

function switchView(target) {
  views.forEach((view) => view.classList.toggle("active", view.id === target));
  navBtns.forEach((btn) => btn.classList.toggle("active", btn.dataset.view === target));
  title.textContent = titles[target][0];
  subtitle.textContent = titles[target][1];
}

function simulateEvent() {
  if (state.liveMode) {
    return;
  }

  const newTrack = {
    id: `T-${Math.floor(Math.random() * 900 + 100)}`,
    x: Math.random() * 78 + 10,
    y: Math.random() * 72 + 12,
    threat: Math.random() > 0.65 ? "HIGH" : "MEDIUM",
    confidence: Math.random() * 0.22 + 0.73,
    speed: Math.random() * 10 + 4
  };

  state.tracks.unshift(newTrack);
  state.tracks = state.tracks.slice(0, 6);

  if (newTrack.threat === "HIGH") {
    state.incidents.unshift({
      id: `INC-${Math.floor(Math.random() * 90000 + 10000)}`,
      zone: "Zona A",
      level: "HIGH",
      createdAt: new Date().toLocaleTimeString("pt-BR"),
      resumen: `${newTrack.id} detectado con aproximacion critica y ruta convergente.`
    });
    opsStatus.textContent = "ALERTA VERMELHO";
    opsStatus.style.color = "#f05d23";
  } else {
    opsStatus.textContent = "ALERTA AMARILLA";
    opsStatus.style.color = "#f2b134";
  }

  state.incidents = state.incidents.slice(0, 8);

  renderMap();
  renderIncidents();
}

function mapHealthToDot(health) {
  const value = (health || "").toLowerCase();
  if (value === "online") return "online";
  if (value === "warning") return "warn";
  return "offline";
}

async function refreshFromApi() {
  const [tracksRes, incidentsRes, sensorsRes] = await Promise.all([
    fetch(`${API_BASE}/api/tracks?limit=30`),
    fetch(`${API_BASE}/api/incidents?limit=30`),
    fetch(`${API_BASE}/api/sensors/status`)
  ]);

  if (!tracksRes.ok || !incidentsRes.ok || !sensorsRes.ok) {
    throw new Error("Failed to fetch live data from API.");
  }

  const tracksData = await tracksRes.json();
  const incidentsData = await incidentsRes.json();
  const sensorsData = await sensorsRes.json();

  state.tracks = tracksData.map((t) => ({
    id: t.trackId?.slice(0, 8) || "TRACK",
    threat: "MEDIUM",
    confidence: t.confidence ?? 0,
    speed: t.estimatedSpeedMps ?? 0,
    position: t.estimatedPosition
      ? {
          latitude: t.estimatedPosition.latitude,
          longitude: t.estimatedPosition.longitude
        }
      : null
  }));

  state.incidents = incidentsData.map((i) => ({
    id: i.incidentId?.slice(0, 8) || "INCIDENT",
    zone: i.zone || "Unknown",
    level: i.status === "Open" ? "HIGH" : "MEDIUM",
    createdAt: new Date(i.createdAtUtc).toLocaleTimeString("pt-BR"),
    resumen: `Track ${String(i.trackId || "").slice(0, 8)} en ${i.zone || "zona"} (${i.status}).`
  }));

  state.sensors = sensorsData.map((s) => ({
    id: s.sensorNodeId,
    type: s.sensorType,
    status: mapHealthToDot(s.health),
    quality: Math.round(s.signalQuality ?? 0)
  }));

  if (state.incidents.length > 0) {
    opsStatus.textContent = "ALERTA VERMELHO";
    opsStatus.style.color = "#f05d23";
  } else {
    opsStatus.textContent = "MONITORAMENTO";
    opsStatus.style.color = "#20bf6b";
  }

  renderMap();
  renderIncidents();
  renderSensors();
}

async function toggleLiveMode() {
  state.liveMode = !state.liveMode;
  connectBtn.textContent = state.liveMode ? "Modo Live: ON" : "Modo Live: OFF";
  simulateBtn.disabled = state.liveMode;

  if (!state.liveMode) {
    return;
  }

  try {
    await refreshFromApi();
  } catch {
    state.liveMode = false;
    connectBtn.textContent = "Modo Live: OFF";
    simulateBtn.disabled = false;
  }
}

navBtns.forEach((btn) => {
  btn.addEventListener("click", () => switchView(btn.dataset.view));
});

simulateBtn.addEventListener("click", simulateEvent);
connectBtn.addEventListener("click", toggleLiveMode);
replaySlider.addEventListener("input", renderReplay);

setClock();
setInterval(setClock, 1000);
setInterval(async () => {
  if (!state.liveMode) {
    return;
  }

  try {
    await refreshFromApi();
  } catch {
    state.liveMode = false;
    connectBtn.textContent = "Modo Live: OFF";
    simulateBtn.disabled = false;
  }
}, 4000);

renderMap();
renderIncidents();
renderSensors();
renderReplay();
renderPipeline();
window.requestAnimationFrame(radarTick);
