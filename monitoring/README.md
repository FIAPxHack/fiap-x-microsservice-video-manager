# Monitoramento – FIAP X Video Manager

Stack de observabilidade completa usando **Prometheus + Loki + Promtail + Grafana**.

---

## O que foi implementado

| Componente | Função | Porta |
|---|---|---|
| **prometheus-net** | Expõe `/metrics` na API .NET | 8080/metrics |
| **Prometheus** | Raspa métricas da API e do PostgreSQL | 9090 |
| **postgres_exporter** | Exporta métricas do PostgreSQL | 9187 (interno) |
| **Loki** | Agrega logs dos containers | 3100 |
| **Promtail** | Coleta logs via Docker socket → Loki | — |
| **Grafana** | Dashboard visual (métricas + logs) | 3000 |

### O que é monitorado automaticamente

- **HTTP**: taxa de requisições por status code, latência (p50/p95/p99), throughput
- **.NET runtime**: uso de memória, GC collections, threads ativas, heap size
- **PostgreSQL**: conexões ativas, queries, locks, tamanho do banco
- **Logs**: todos os logs dos containers `videomanager_api` e `videomanager_postgres`
- **Alertas**: API down, alta taxa de erro 5xx (>5%), latência p95 >2s, Postgres down

---

## Como rodar localmente

```bash
# Subir toda a stack (app + monitoramento)
docker compose up -d

# Verificar se todos os containers subiram
docker compose ps
```

### Acessos

| Serviço | URL | Credenciais |
|---|---|---|
| **Grafana** | http://localhost:3000 | admin / fiapx@123 |
| **Prometheus** | http://localhost:9090 | — |
| **Loki** | http://localhost:3100 | — |
| **API /metrics** | http://localhost:5002/metrics | — |
| **API /health** | http://localhost:5002/health | — |

---

## Estrutura de arquivos

```
monitoring/
├── prometheus/
│   ├── prometheus.yml       # configuração de scrape jobs
│   └── alert_rules.yml      # regras de alertas
├── loki/
│   └── loki.yml             # configuração do Loki
├── promtail/
│   └── promtail.yml         # configuração de coleta de logs
├── grafana/
│   ├── provisioning/
│   │   ├── datasources/
│   │   │   └── datasources.yml   # Prometheus + Loki auto-provisionados
│   │   └── dashboards/
│   │       └── dashboards.yml    # pasta de dashboards auto-provisionada
│   └── dashboards/
│       └── videomanager-overview.json  # dashboard principal
└── TERRAFORM_GUIDE.md       # guia de deploy em produção com Terraform
```

---

## Dashboard Grafana

O dashboard **"FIAP X - Video Manager"** é carregado automaticamente com:

1. **Taxa de Requisições** – req/s por status code (2xx, 4xx, 5xx)
2. **Latência p50/p95/p99** – em segundos
3. **Memória** – Working Set e GC Heap do processo .NET
4. **Threads** – número de threads ativas
5. **Status dos Serviços** – API e PostgreSQL UP/DOWN
6. **Taxa de Erros 5xx** – percentual com threshold visual
7. **GC Collections** – por geração (Gen0, Gen1, Gen2)
8. **Logs** – painel de logs em tempo real via Loki

---

## Adicionar novos alertas

Edite `monitoring/prometheus/alert_rules.yml` e recarregue o Prometheus:

```bash
curl -X POST http://localhost:9090/-/reload
```

---

## Deploy em produção

Consulte [`TERRAFORM_GUIDE.md`](./TERRAFORM_GUIDE.md) para o passo a passo
completo de provisionamento na AWS com Terraform (ECS Fargate + EFS).

