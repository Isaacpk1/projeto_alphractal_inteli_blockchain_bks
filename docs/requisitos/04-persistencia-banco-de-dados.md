[← Índice](./README.md)

# 04 — Persistência e banco de dados

O caminho frio usa ClickHouse local via Docker. A API .NET não escreve no banco;
o Python ETL é o único escritor e a API lê somente views. O caminho quente e o
SSE continuam exclusivamente na memória do backend (RN-14).

## Fluxo de persistência

```text
.NET fecha NDJSON em spool/ready
  -> Python move atomicamente para processing
  -> valida o arquivo inteiro
  -> faz bulk insert com confirmação durável
  -> recalcula os buckets afetados
  -> move para processed
```

Erro de contrato envia o arquivo para `failed/` com um relatório. Erro transitório
de banco mantém o arquivo em `processing/`, retomado no ciclo seguinte.

## Tabelas de entrada

- `eth_blocks`: bloco, taxas percentis, gas, burn e preço ETH/USD.
- `mempool_samples`: amostras sub-bloco.
- `fee_estimates`: custo calculado pelo Service .NET por operação/velocidade.
- `eth_usd_prices`: série do preço utilizado nos cálculos.
- `ingestion_health`: heartbeat de `ws_listener`, `etl` e `api`.

Wei usa `UInt64`/`UInt128`; valores monetários usam `Decimal`. Todos os timestamps
incluem timezone UTC. O contrato Python rejeita campos extras, ausentes, valores
unsigned negativos e timestamps sem timezone antes de acessar o banco.

## Inserção em lote e confirmação

O spool agrupa blocos para evitar uma part por evento. O usuário ETL habilita
`async_insert=1`, porém mantém `wait_for_async_insert=1`: o ClickHouse só responde
sucesso depois do flush. Isso é obrigatório porque `processed/` significa dado
confirmado, não apenas recebido em memória.

## Idempotência e reorg

ClickHouse não oferece unicidade por primary key. As tabelas reprocessáveis usam
`ReplacingMergeTree(ingested_at)` e uma chave lógica no `ORDER BY`. Uma correção
de reorg é uma nova versão, nunca um `UPDATE`. Consultas que precisam de correção
imediata usam `FINAL`.

## Rollups corretos

Materialized views incrementais não são adequadas sobre uma fonte que recebe
reinserções: elas agregam o bloco novamente antes da deduplicação. O ETL recalcula
as horas e dias afetados a partir das tabelas `FINAL` e grava uma nova versão em:

- `eth_fees_rollup`, chave `(granularity, bucket)`;
- `fee_estimates_1d`, chave `(operation, speed, bucket)`.

Ambas usam `ReplacingMergeTree(calculated_at)`. Os dados brutos expiram; os
rollups não possuem TTL e preservam o histórico longo.

## Camada de leitura

A API consulta somente `v_latest_block`, `v_mempool_now`,
`v_fee_estimates_now`, `v_eth_fees_1h`, `v_eth_fees_1d`,
`v_fee_estimates_1d` e `v_ingestion_status`.

As views convertem wei para gwei/ETH e executam com `SQL SECURITY DEFINER`. O
usuário da API recebe `SELECT` nas views, não nas tabelas brutas.

## Retenção

- blocos e estimativas: 30 dias;
- mempool: 7 dias;
- preço ETH/USD: 90 dias;
- saúde da ingestão: 14 dias;
- agregados horários e diários: indefinidamente.

## Backfill

O backfill acessa a Alchemy por HTTP, ancora `eth_feeHistory` em blocos explícitos
e gera o mesmo NDJSON do fluxo normal. Ele não escreve diretamente no banco. O
valor adicional de `baseFeePerGas` retornado pelo RPC já é a base fee do próximo
bloco e não deve ser projetado outra vez.
