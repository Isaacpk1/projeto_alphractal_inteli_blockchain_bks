[← Índice](./README.md)

# 04 — Persistência e Banco de Dados

---

## 1. O banco é mesmo necessário?

O TAP não menciona banco de dados, e o coração do projeto — o stream ao vivo — funciona **sem nenhum**: a janela de ~300 blocos (RN-10) cabe em memória. A Alphractal, inclusive, **já possui o histórico**: a aba "Fees" atual é justamente baseada em médias históricas. O valor do projeto é a camada ao vivo, não o arquivo histórico.

Ainda assim, persistir se justifica por três motivos concretos:

1. **Sobreviver a reinício.** Sem banco, cada restart do backend zera o gráfico e o painel fica vazio por vários minutos.
2. **Janelas maiores que a memória.** Gráficos de 24 h / 7 d e a média de referência do índice de congestionamento (RN-04) e do percentil histórico (D-02).
3. **Demo Day previsível.** Poder exibir um período de volatilidade já gravado, em vez de depender de a rede oscilar ao vivo em 05/10.

**Decisão: SQLite** (`better-sqlite3`, modo WAL), atrás de uma interface de *repository* (RNF-27). Se o parceiro pedir algo mais institucional, troca-se para PostgreSQL sem tocar na lógica de negócio.

## 2. Comparação das opções

| Opção | Quando faz sentido | Custo/atrito | Veredito |
|---|---|---|---|
| **Só memória (ring buffer)** | Se o escopo ficar restrito à janela de 1 h | Zero | Base obrigatória, mesmo com banco |
| **SQLite** (`better-sqlite3`) | MVP acadêmico, uma instância de backend | Zero infra, arquivo em volume | ✅ **Escolhido para o MVP** |
| **PostgreSQL** (+ TimescaleDB) | Prova de conceito escalável / multi-instância | Docker ou free tier (Neon, Supabase, Railway) | Alternativa se o parceiro pedir |
| **Redis** | Cache do snapshot, cotação USD, *pub/sub* entre instâncias | Mais um serviço para subir | Complemento — **não** substitui o histórico |
| **InfluxDB / ClickHouse** | Séries temporais em escala real | Curva de aprendizado alta | ❌ Overkill para 4 semanas |

**Volume esperado:** 1 bloco a cada ~12 s → ~7.200 registros/dia, ~2,6 M/ano, na casa de centenas de MB por ano. Qualquer opção acima aguenta com folga — a escolha é sobre **atrito operacional**, não sobre escala.

## 3. Modelo de dados

```sql
-- Granularidade bloco a bloco (retenção curta: 7 dias, configurável)
CREATE TABLE blocks (
  block_number       INTEGER PRIMARY KEY,   -- idempotência e proteção contra reorg
  block_timestamp    INTEGER NOT NULL,      -- unix, vindo do próprio bloco
  ingested_at        INTEGER NOT NULL,      -- unix ms, quando o backend recebeu → mede a latência do RNF-01
  base_fee_per_gas   TEXT    NOT NULL,      -- wei como string; NUNCA float
  gas_used           TEXT    NOT NULL,
  gas_limit          TEXT    NOT NULL,
  gas_used_ratio     REAL    NOT NULL,      -- derivado, para consulta rápida
  priority_fee_p25   TEXT,
  priority_fee_p50   TEXT,
  priority_fee_p90   TEXT,
  eth_usd            REAL,                  -- cotação NO MOMENTO do bloco
  eth_usd_source     TEXT,
  congestion_level   TEXT                   -- 'low' | 'normal' | 'high' | 'extreme'
);

CREATE INDEX idx_blocks_ts ON blocks(block_timestamp DESC);

-- Agregados horários (retenção longa: alimenta gráficos de 7/30 dias e o percentil histórico)
CREATE TABLE fee_stats_hourly (
  bucket_start   INTEGER PRIMARY KEY,       -- unix, início da hora
  avg_base_fee   TEXT    NOT NULL,
  min_base_fee   TEXT    NOT NULL,
  max_base_fee   TEXT    NOT NULL,
  p50_base_fee   TEXT    NOT NULL,
  avg_eth_usd    REAL,
  block_count    INTEGER NOT NULL
);
```

### Decisões de modelagem que importam

| Decisão | Motivo |
|---|---|
| Wei como `TEXT` (ou `NUMERIC(78,0)` no Postgres), convertido para `bigint` na aplicação | `REAL`/`FLOAT` perde precisão em valores de wei e o erro se propaga para o custo em USD (RN-06) |
| Cotação ETH/USD gravada **junto com o bloco** | Sem isso é impossível reconstruir o custo histórico em dólar — a cotação de hoje não vale para um bloco de ontem |
| `ingested_at` separado de `block_timestamp` | A diferença entre os dois é a métrica de latência que comprova o RNF-01 na apresentação |
| `block_number` como chave primária | Dá idempotência de graça: reconexão, backfill e reorg viram `UPSERT`, sem linha duplicada (RN-08) |
| Agregados horários em tabela separada | Permite descartar dados brutos sem perder o histórico longo (RN-15) |

## 4. Requisitos relacionados

**Funcionais:** RF-15, RF-34 a RF-38 → ver [01 — Requisitos Funcionais](./01-requisitos-funcionais.md)
**Não funcionais:** RNF-25 a RNF-29 → ver [02 — Requisitos Não Funcionais](./02-requisitos-nao-funcionais.md)
**Regras:** RN-14 (banco não serve tempo real), RN-15 (retenção), RN-16 (reorg) → ver [03 — Regras de Negócio](./03-regras-de-negocio.md)

## 5. Pendência

A dúvida nº 5 do [kick-off](./06-duvidas-kickoff.md) pode invalidar esta decisão: se a Alphractal preferir que o painel consuma o histórico longo do backend **deles**, o banco local se reduz a um cache de curta duração — o que simplifica bastante o projeto.
