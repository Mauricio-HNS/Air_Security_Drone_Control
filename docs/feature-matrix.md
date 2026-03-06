# Air Security Drone Control - Feature Matrix

Matriz de priorizacao para transformar o blueprint em entregas incrementais.

## Legenda
- P0: essencial para MVP operacional
- P1: necessario para operacao robusta
- P2: expansao enterprise e escala multi-site

## P0 - MVP Operacional

### Sensor Integration Layer
- Ingestao de camera + RF
- Normalizacao em `DetectionEvent`
- Health check basico por sensor

### AI Detection & Classification
- Classificacao inicial: drone, passaro, desconhecido
- Confidence score por deteccao

### Multi-Sensor Fusion
- Correlacao temporal basica
- Deduplicacao simples
- Geração de `FusedTrack`

### Tracking & Prediction
- Posicao, velocidade, direcao
- ETA basico para zona protegida

### Threat Intelligence
- Threat score com fatores: proximidade + confianca + persistencia
- Niveis LOW/MEDIUM/HIGH/CRITICAL

### Rules Engine
- Regras por zona
- Escalonamento amarelo/vermelho
- Supressao por confidence minima

### Incident Management
- Abrir incidente automatico/manual
- Workflow: Open, Investigating, Resolved, Dismissed

### Tactical Command Center
- Mapa tatico em tempo real
- Lista de incidentes
- Painel de sensores
- Replay basico

### APIs
- REST para ingestao, fusao, threat, incidentes e overview

## P1 - Operacao Robusta

### Sensor Integration
- Radar e acustico
- Provisionamento remoto
- Sincronizacao temporal multi-fonte

### AI & Fusion
- Fusao multimodal completa (EO/IR + RF + radar + audio)
- Detecção de anomalia de comportamento
- Reconhecimento de swarm

### Incident & Evidence
- Evidencias com hash
- Timeline forense
- Exportacao de pacote de incidente

### Alerting & Notification
- Webhook, email, SMS
- Deduplicacao e rate limiting
- Confirmacao de recebimento

### Security
- OIDC/OAuth2 + RBAC
- Auditoria imutavel

### Observability
- Logs estruturados
- Metricas
- Tracing distribuido

## P2 - Enterprise & Escala

### Inteligencia Historica
- Heatmap de risco
- Recomendacoes automaticas de tuning
- MTTD/MTTR e KPIs executivos

### Edge-Cloud Hibrido
- Operacao offline em edge
- Sincronizacao eventual multi-site
- Gestao por instalacao

### Compliance
- Framework de controles para GDPR/ISO 27001
- Cadeia de custodia estendida e politica legal por tenant

### Integracoes Avancadas
- Conectores para SOC/VMS/PSIM/SIEM
- Event bus com contratos versionados

## Criterio de pronto por fase

### Pronto P0
- Fluxo fim a fim: deteccao -> fusao -> threat -> incidente -> command center
- 2 tipos de sensor no pipeline
- Alertas por zona ativos

### Pronto P1
- Multi-sensor completo em operacao
- Evidencia forense e notificacao multi-canal
- Seguranca e observabilidade de producao

### Pronto P2
- Operacao multi-site em edge+cloud
- Inteligencia historica ativa com recomendacoes
- Integracoes enterprise estabilizadas
