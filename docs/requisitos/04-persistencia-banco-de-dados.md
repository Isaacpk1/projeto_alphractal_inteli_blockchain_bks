[← Índice](./README.md)

# 04 — Persistência e Banco de Dados (ClickHouse)

> **Banco definido pela Alphractal.** A análise anterior — que recomendava SQLite — está preservada na seção 6 como registro da decisão, mas não vale mais. Contexto da stack completa em [09 — Arquitetura](./09-arquitetura-e-stack.md).

---

## 1. O que muda ao usar ClickHouse

ClickHouse é um banco **colunar OLAP**. Ele foi construído para varrer bilhões de linhas em consultas analíticas, não para servir uma linha por vez com baixa latência. As três consequências abaixo mudam o desenho do código — não são detalhes de configuração.

### 1.1 Nunca inserir linha a linha ⚠️

Cada `INSERT` cria um *part* (um conjunto de arquivos em disco). Um processo em segundo plano funde esses parts continuamente. Inserindo **um bloco a cada 12 segundos**, você gera 7.200 parts por dia e o merge não acompanha — a tabela para de aceitar escrita com `TOO_MANY_PARTS`.

É o erro mais comum de quem chega vindo de Postgres/MySQL, e ele não aparece no primeiro dia de teste: aparece depois de algumas horas rodando, tipicamente na véspera da apresentação.

**Como resolvemos:** o spool de arquivos NDJSON descrito em [09 §2](./09-arquitetura-e-stack.md) agrupa naturalmente ~5 blocos por lote. Alternativas, se necessário:

| Técnica | Quando usar |
|---|---|
| Lote na aplicação (nossa escolha) | Padrão — controle total sobre o agrupamento |
| `async_insert=1` | Se algum ponto precisar inserir direto sem lote |
| Tabela com engine `Buffer` | Última opção — mantém dados em RAM, perde tudo se o processo cair |

### 1.2 `PRIMARY KEY` não garante unicidade ⚠️

No ClickHouse a chave primária é um **índice esparso**, não uma restrição. Inserir o mesmo `block_number` duas vezes gera **duas linhas** — não há erro nem substituição.

Como a entrega do spool é *at-least-once* (um arquivo reprocessado após uma falha reenvia os mesmos blocos), a idempotência exigida pela **RN-08** precisa vir do engine:

- `ReplacingMergeTree(ingested_at)` mantém apenas a linha de maior `ingested_at` por chave de ordenação.
- A deduplicação é **assíncrona** — ocorre na fusão dos parts, em momento indeterminado.
- Consultas que exigem garantia usam `FINAL` (mais lento) ou agregação com `argMax()`.

### 1.3 Não existe `UPDATE` barato ⚠️

`ALTER TABLE ... UPDATE/DELETE` é uma *mutation*: assíncrona, cara, reescreve partes inteiras. Tratamento de reorg (**RN-16**) passa a ser **"inserir uma nova versão da linha"**, jamais "atualizar a existente".

---

## 2. O que fica melhor

| Ganho | Detalhe |
|---|---|
| **Tipos nativos para wei** | `UInt64` e `UInt256` existem. Some a necessidade de guardar wei como `TEXT` — a gambiarra que a versão anterior desta especificação exigia |
| **Retenção declarativa** | `TTL block_time + INTERVAL 90 DAY` na definição da tabela. A **RN-15** vira configuração, não job agendado |
| **Agregação automática** | *Materialized views* com `AggregatingMergeTree` mantêm os rollups horários sozinhas. O **RF-37** deixa de ser código |
| **Percentis nativos** | `quantile()` e `quantiles()` são funções de primeira classe e rápidas. **D-02** (percentil histórico) e **D-04** (heatmap) — os diferenciais mais caros do backlog — ficam quase triviais |

Esses dois últimos são o motivo pelo qual o backlog de diferenciais ficou mais barato com esta stack, não mais caro.

---

## 3. Modelo de dados

### 3.1 Tabela bruta

```sql
CREATE TABLE blocks
(
    block_number        UInt64,
    block_time          DateTime,        -- timestamp do bloco
    ingested_at         DateTime64(3),   -- quando o .NET recebeu → versão do ReplacingMergeTree + métrica do RNF-01
    base_fee_per_gas    UInt64,          -- wei (cabe com folga: máximo histórico ~1e13)
    gas_used            UInt64,
    gas_limit           UInt64,
    gas_used_ratio      Float32,
    priority_fee_p25    UInt64,
    priority_fee_p50    UInt64,
    priority_fee_p90    UInt64,
    eth_usd             Decimal(18, 8),  -- cotação NO MOMENTO do bloco; Decimal, não Float
    eth_usd_source      LowCardinality(String),
    congestion_level    LowCardinality(String)   -- 'low' | 'normal' | 'high' | 'extreme'
)
ENGINE = ReplacingMergeTree(ingested_at)
PARTITION BY toYYYYMM(block_time)
ORDER BY block_number
TTL block_time + INTERVAL 90 DAY;
```

**Por que cada escolha:**

| Escolha | Motivo |
|---|---|
| `ReplacingMergeTree(ingested_at)` | Idempotência (RN-08) e reorg (RN-16): a versão mais recente do bloco vence |
| `ORDER BY block_number` | Consultas do painel são sempre por janela de blocos ou tempo, que são monotônicos juntos |
| `PARTITION BY toYYYYMM` | Uma partição por mês. Com ~216 k linhas/mês, partições diárias seriam pequenas demais e criariam parts em excesso |
| `LowCardinality(String)` | Colunas com poucos valores distintos ficam dicionarizadas — menor e mais rápido que `String` |
| `Decimal(18,8)` para USD | `Float64` em valor monetário reintroduz o erro de arredondamento que a RN-06 existe para evitar |
| `TTL 90 DAY` | Retenção da RN-15 aplicada pelo próprio banco |

### 3.2 Agregados horários (materialized view)

Alimenta D-02 (percentil) e D-04 (heatmap) sem varrer a tabela bruta, e sobrevive ao TTL dos dados brutos.

```sql
CREATE TABLE fee_stats_hourly
(
    bucket          DateTime,
    avg_base_fee    AggregateFunction(avg, UInt64),
    min_base_fee    AggregateFunction(min, UInt64),
    max_base_fee    AggregateFunction(max, UInt64),
    quantiles_state AggregateFunction(quantiles(0.25, 0.5, 0.9), UInt64),
    avg_eth_usd     AggregateFunction(avg, Decimal(18, 8)),
    block_count     AggregateFunction(count)
)
ENGINE = AggregatingMergeTree()
ORDER BY bucket;

CREATE MATERIALIZED VIEW fee_stats_hourly_mv TO fee_stats_hourly AS
SELECT
    toStartOfHour(block_time)                        AS bucket,
    avgState(base_fee_per_gas)                       AS avg_base_fee,
    minState(base_fee_per_gas)                       AS min_base_fee,
    maxState(base_fee_per_gas)                       AS max_base_fee,
    quantilesState(0.25, 0.5, 0.9)(base_fee_per_gas) AS quantiles_state,
    avgState(eth_usd)                                AS avg_eth_usd,
    countState()                                     AS block_count
FROM blocks
GROUP BY bucket;
```

Consulta (note o sufixo `Merge`, obrigatório ao ler estados agregados):

```sql
SELECT bucket, avgMerge(avg_base_fee) AS media
FROM fee_stats_hourly
WHERE bucket >= now() - INTERVAL 7 DAY
GROUP BY bucket ORDER BY bucket;
```

> ⚠️ **Materialized view no ClickHouse é um gatilho de inserção, não uma view.** Ela só enxerga as linhas **inseridas depois de sua criação** — não faz backfill do que já existe. Se a tabela `blocks` for populada antes da MV existir, é preciso um `INSERT ... SELECT` manual para preencher o histórico. Criar a MV **antes** da primeira carga.

### 3.3 Consulta do percentil (D-02)

O que antes exigiria código de aplicação vira uma linha:

```sql
SELECT round(100 * countIf(base_fee_per_gas <= {atual:UInt64}) / count(), 1) AS percentil
FROM blocks
WHERE block_time >= now() - INTERVAL 30 DAY;
```

---

## 4. Divisão de responsabilidades

| Quem | Faz o quê |
|---|---|
| **.NET** | **Só lê** o ClickHouse, para os endpoints de histórico longo. Nunca escreve |
| **Python ETL** | Único componente que escreve: consome o spool, valida, insere em lote |
| **Memória do .NET** | Serve todo o tempo real — janela de 300 blocos, `/current`, stream SSE (**RN-14**) |

Um único escritor simplifica a idempotência e elimina disputa de escrita.

---

## 5. Requisitos revisados

| ID | Texto revisado |
|---|---|
| RF-34 | Persistir blocos **em lote** (mínimo ~5 registros ou 60 s por `INSERT`), nunca linha a linha |
| RF-35 | Ao iniciar, o .NET recarrega a janela quente consultando o ClickHouse com `FINAL` ou `argMax()` |
| RF-36 | *Backfill*: script Python dedicado, executado sob demanda, com controle de consumo de CU |
| RF-37 | Agregados horários mantidos por **materialized view** — não por código |
| RF-38 | Endpoint de histórico longo lê `fee_stats_hourly`; janelas curtas vêm da memória |
| RN-06 | Wei em `UInt64`/`UInt256` no banco e `System.Numerics.BigInteger` no .NET. **A regra de guardar wei como `TEXT` foi revogada** |
| RN-08 / RN-16 | Idempotência e reorg via `ReplacingMergeTree(ingested_at)`, com dedup assíncrona; consultas críticas usam `FINAL`/`argMax()` |
| RN-15 | Retenção por `TTL` declarativo |
| RNF-22 | `docker-compose` obrigatório — ClickHouse exige container |

## 6. Registro: por que a recomendação anterior era SQLite

Antes de a stack ser definida pelo parceiro, a recomendação era **SQLite**, pelo volume: ~216 mil linhas/mês, ~2,6 M/ano. ClickHouse é projetado para bilhões de linhas — para este volume é desproporcional.

A decisão do parceiro continua sendo a correta, por um critério que não era técnico-de-volume: **alinhamento com a infraestrutura de produção deles**, o que aumenta a chance de o código ser absorvido — um dos benefícios declarados no TAP. E há um ganho técnico real: D-02 e D-04 ficam significativamente mais baratos.

O registro fica aqui porque, numa banca, "por que ClickHouse para 200 mil linhas?" é uma pergunta previsível, e a resposta honesta é melhor do que uma justificativa inventada de performance.

## 7. Pendências

Dúvidas **21** (instância de ClickHouse: deles ou nossa?) e **25** (padrão de ETL em Python) em [06 — Dúvidas](./06-duvidas-kickoff.md).
