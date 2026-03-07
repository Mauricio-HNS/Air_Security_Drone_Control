# Air Security Drone Control - Feature Matrix

Matriz de priorizacao para transformar o blueprint en entregas incrementais.

## Legenda
- P0: essencial para MVP operativo
- P1: necessario para operacion robusta
- P2: expansao enterprise y escala multi-site

## P0 - MVP Operativo

### Sensor Integration Layer
- Ingestao de camara + RF
- Normalizacao en `DetectionEvent`
- Chequeo de salud basico por sensor

### AI Detection & Classification
- Classificacao inicial: drone, passaro, desconhecido
- Confidence score por deteccion

### Multi-Sensor Fusion
- Correlacion tiemporal basica
- Deduplicacao simples
- Generacion de `FusedTrack`

### Tracking & Prediction
- Posicao, velocidade, direcao
- ETA basico para zona protegida

### Threat Intelligence
- Threat score con fatores: proximidade + confianza + persistencia
- Niveis LOW/MEDIUM/HIGH/CRITICAL

### Rules Engine
- Regras por zona
- Escalonamento amarelo/vermelho
- Supressao por confidence minima

### Incident Management
- Abrir incidente automatico/manual
- Workflow: Open, Investigating, Resolved, Dismissed

### Tactical Command Center
- Mapa tatico en tiempo real
- Lista de incidentes
- Painel de sensores
- Replay basico

### APIs
- REST para ingesta, fusion, threat, incidentes y  overview

## P1 - Operacion Robusta

### Sensor Integration
- Radar y acustico
- Provisionamento remoto
- Sincronizacao tiemporal multi-fonte

### AI & Fusion
- Fusao multimodal completa (EO/IR + RF + radar + audio)
- Deteccion de anomalia de comportamento
- Reconhecimento de swarm

### Incident & Evidence
- Evidencias con hash
- Timeline forense
- Exportacao de pacote de incidente

### Alerting & Notification
- Webhook, email, SMS
- Deduplicacao y rate limiting
- Confirmacao de recebimento

### Security
- OIDC/OAuth2 + RBAC
- Auditoria inmutable

### Observability
- Logs estruturados
- Metricas
- Tracing distribuido

## P2 - Enterprise & Escala

### Inteligencia Historica
- Heatmap de risco
- Recomendacoes automaticas de tuning
- MTTD/MTTR y KPIs executivos

### Edge-Cloud Hibrido
- Operacion offline en edge
- Sincronizacao eventual multi-site
- Gestao por instalacao

### Compliance
- Framework de controles para GDPR/ISO 27001
- Cadeia de custodia estendida y politica legal por tenant

### Integracoes Avancadas
- Conectores para SOC/VMS/PSIM/SIEM
- Event bus con contratos versionados

## Criterio de listo por fase

### Pronto P0
- Flujo end-to-end: deteccion -> fusion -> threat -> incidente -> command center
- 2 tipos de sensor no pipeline
- Alertas por zona ativos

### Pronto P1
- Multi-sensor completo en operacion
- Evidencia forense y notificacao multi-canal
- Seguranca y observabilidade de produccion

### Pronto P2
- Operacion multi-site en edge+cloud
- Inteligencia historica activa con recomendaciones
- Integracoes enterprise estabilizadas
