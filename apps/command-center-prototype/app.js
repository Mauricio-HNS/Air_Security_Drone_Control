const state = {
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
      summary: "Track T-103 com aproximação persistente e confirmação multisensor."
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
    "T+00s: RF sensor detecta assinatura na borda da Zona A.",
    "T+08s: Camera confirma objeto pequeno em movimento constante.",
    "T+14s: Fusion agrupa deteccoes em T-103 (confiança 0.81).",
    "T+20s: Threat scoring sobe para HIGH por proximidade e persistencia.",
    "T+27s: Incident service abre INC-00045 e notifica operador."
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

const title = document.getElementById("view-title");
const subtitle = document.getElementById("view-subtitle");
const opsStatus = document.getElementById("ops-status");
const opsTime = document.getElementById("ops-time");

const titles = {
  overview: ["Mapa Tático", "Visão em tempo real de tracks, zonas e ameaça"],
  incidents: ["Gestão de Incidentes", "Casos ativos, evidências e status operacional"],
  sensors: ["Painel de Sensores", "Saúde e qualidade da rede de detecção"],
  replay: ["Replay Tático", "Análise temporal para pós-incidente e auditoria"],
  architecture: ["Arquitetura Modular", "Pipeline de decisão operacional em camadas"]
};

function setClock() {
  const now = new Date();
  opsTime.textContent = now.toLocaleTimeString("pt-BR");
}

function renderMap() {
  mapCanvas.querySelectorAll(".track").forEach((node) => node.remove());

  state.tracks.forEach((track) => {
    const el = document.createElement("div");
    el.className = `track ${track.threat === "HIGH" ? "high" : ""}`;
    el.style.left = `${track.x}%`;
    el.style.top = `${track.y}%`;
    el.title = `${track.id} | ${track.threat} | conf ${Math.round(track.confidence * 100)}%`;
    mapCanvas.appendChild(el);
  });

  document.getElementById("kpi-tracks").textContent = String(state.tracks.length);
  document.getElementById("kpi-incidents").textContent = String(state.incidents.length);
  document.getElementById("kpi-threat").textContent = state.tracks.some((t) => t.threat === "HIGH") ? "HIGH" : "MEDIUM";
  document.getElementById("kpi-confidence").textContent = `${Math.round(state.tracks.reduce((a,b)=>a+b.confidence,0) / state.tracks.length * 100)}%`;
}

function renderIncidents() {
  incidentList.innerHTML = "";

  state.incidents.forEach((item) => {
    const el = document.createElement("article");
    el.className = `incident ${item.level === "HIGH" ? "high" : ""}`;
    el.innerHTML = `
      <strong>${item.id} • ${item.level}</strong>
      <p>${item.summary}</p>
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
      <small>Qualidade de sinal: ${sensor.quality}%</small>
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
    ["Sensor Gateway", "Recebe câmera, RF, acústico, radar e API externa."],
    ["Detection + Classification", "Classifica drone, pássaro, helicóptero, ruído ou desconhecido."],
    ["Fusion Engine", "Correlaciona sinais para confirmar alvo e reduzir falso positivo."],
    ["Tracking", "Mantém trilha com posição, velocidade e rota provável."],
    ["Threat Scoring", "Calcula score dinâmico por proximidade, persistência e zona sensível."],
    ["Rules Engine", "Define alerta amarelo/vermelho e gatilho de incidente."],
    ["Incident Management", "Abre, atualiza e fecha casos com evidências e auditoria."],
    ["Command Center", "Apresenta mapa, timeline, incidentes e replay para operação."]
  ];

  pipeline.innerHTML = steps
    .map((item, i) => `<div class="step"><strong>${i + 1}. ${item[0]}</strong><p>${item[1]}</p></div>`)
    .join("");
}

function switchView(target) {
  views.forEach((view) => view.classList.toggle("active", view.id === target));
  navBtns.forEach((btn) => btn.classList.toggle("active", btn.dataset.view === target));
  title.textContent = titles[target][0];
  subtitle.textContent = titles[target][1];
}

function simulateEvent() {
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
      summary: `${newTrack.id} detectado com aproximação crítica e rota convergente.`
    });
    opsStatus.textContent = "ALERTA VERMELHO";
    opsStatus.style.color = "#f05d23";
  } else {
    opsStatus.textContent = "ALERTA AMARELO";
    opsStatus.style.color = "#f2b134";
  }

  state.incidents = state.incidents.slice(0, 8);

  renderMap();
  renderIncidents();
}

navBtns.forEach((btn) => {
  btn.addEventListener("click", () => switchView(btn.dataset.view));
});

simulateBtn.addEventListener("click", simulateEvent);
replaySlider.addEventListener("input", renderReplay);

setClock();
setInterval(setClock, 1000);
renderMap();
renderIncidents();
renderSensors();
renderReplay();
renderPipeline();
