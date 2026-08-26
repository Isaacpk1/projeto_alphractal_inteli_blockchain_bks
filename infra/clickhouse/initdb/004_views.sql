-- 004_views.sql
-- Camada de leitura. A API .NET consulta SOMENTE estas views:
-- nenhum *Merge, nenhuma conversão de unidade e nenhum FINAL no código C#.

-- Último bloco consolidado (FINAL resolve o ReplacingMergeTree)
CREATE OR REPLACE VIEW alphractal.v_latest_block AS
SELECT
    block_number,
    block_timestamp,
    base_fee_per_gas / 1e9                       AS base_fee_gwei,
    next_base_fee    / 1e9                       AS next_base_fee_gwei,
    priority_fee_p50 / 1e9                       AS priority_fee_gwei,
    gas_used,
    gas_limit,
    gas_used / greatest(gas_limit, 1)            AS gas_used_ratio,
    tx_count,
    toFloat64(burned_wei) / 1e18                 AS burned_eth,
    eth_usd,
    dateDiff('millisecond', block_timestamp, now64(3, 'UTC')) AS age_ms
FROM alphractal.eth_blocks FINAL
ORDER BY block_number DESC
LIMIT 1;

-- Estado atual da mempool (última amostra)
CREATE OR REPLACE VIEW alphractal.v_mempool_now AS
SELECT
    sampled_at,
    block_number,
    pending_tx_count,
    base_fee_per_gas            / 1e9 AS base_fee_gwei,
    suggested_priority_slow     / 1e9 AS priority_slow_gwei,
    suggested_priority_standard / 1e9 AS priority_standard_gwei,
    suggested_priority_fast     / 1e9 AS priority_fast_gwei,
    eth_usd
FROM alphractal.mempool_samples
ORDER BY sampled_at DESC
LIMIT 1;

-- Custo atual por operação (uma linha por operação/velocidade)
CREATE OR REPLACE VIEW alphractal.v_fee_estimates_now AS
SELECT
    operation,
    speed,
    argMax(gas_units, sampled_at)      AS gas_units,
    argMax(total_fee_gwei, sampled_at) AS total_fee_gwei,
    argMax(total_fee_usd, sampled_at)  AS total_fee_usd,
    max(sampled_at)                    AS last_sampled_at
FROM alphractal.fee_estimates
WHERE sampled_at > now64(3, 'UTC') - INTERVAL 10 MINUTE
GROUP BY operation, speed;

-- Série horária pronta para gráfico
CREATE OR REPLACE VIEW alphractal.v_eth_fees_1h AS
SELECT
    bucket,
    countMerge(blocks)                        AS blocks,
    avgMerge(base_fee_avg)          / 1e9     AS base_fee_gwei_avg,
    minMerge(base_fee_min)          / 1e9     AS base_fee_gwei_min,
    maxMerge(base_fee_max)          / 1e9     AS base_fee_gwei_max,
    quantilesMerge(0.5, 0.9, 0.95)(base_fee_q)[1] / 1e9 AS base_fee_gwei_p50,
    quantilesMerge(0.5, 0.9, 0.95)(base_fee_q)[2] / 1e9 AS base_fee_gwei_p90,
    quantilesMerge(0.5, 0.9, 0.95)(base_fee_q)[3] / 1e9 AS base_fee_gwei_p95,
    avgMerge(priority_fee_avg)      / 1e9     AS priority_fee_gwei_avg,
    avgMerge(gas_used_ratio_avg)              AS gas_used_ratio_avg,
    sumMerge(tx_count_sum)                    AS tx_count,
    toFloat64(sumMerge(burned_sum)) / 1e18    AS burned_eth,
    avgMerge(eth_usd_avg)                     AS eth_usd_avg
FROM alphractal.eth_fees_1h
GROUP BY bucket;

-- Série diária
CREATE OR REPLACE VIEW alphractal.v_eth_fees_1d AS
SELECT
    bucket,
    countMerge(blocks)                        AS blocks,
    avgMerge(base_fee_avg)          / 1e9     AS base_fee_gwei_avg,
    minMerge(base_fee_min)          / 1e9     AS base_fee_gwei_min,
    maxMerge(base_fee_max)          / 1e9     AS base_fee_gwei_max,
    quantilesMerge(0.5, 0.9, 0.95)(base_fee_q)[1] / 1e9 AS base_fee_gwei_p50,
    quantilesMerge(0.5, 0.9, 0.95)(base_fee_q)[2] / 1e9 AS base_fee_gwei_p90,
    quantilesMerge(0.5, 0.9, 0.95)(base_fee_q)[3] / 1e9 AS base_fee_gwei_p95,
    avgMerge(priority_fee_avg)      / 1e9     AS priority_fee_gwei_avg,
    avgMerge(gas_used_ratio_avg)              AS gas_used_ratio_avg,
    sumMerge(tx_count_sum)                    AS tx_count,
    toFloat64(sumMerge(burned_sum)) / 1e18    AS burned_eth,
    avgMerge(eth_usd_avg)                     AS eth_usd_avg
FROM alphractal.eth_fees_1d
GROUP BY bucket;

-- Custo diário por operação (heatmap)
CREATE OR REPLACE VIEW alphractal.v_fee_estimates_1d AS
SELECT
    bucket,
    operation,
    speed,
    countMerge(samples)                AS samples,
    avgMerge(usd_avg)                  AS usd_avg,
    minMerge(usd_min)                  AS usd_min,
    maxMerge(usd_max)                  AS usd_max,
    quantilesMerge(0.5, 0.9)(usd_q)[1] AS usd_p50,
    quantilesMerge(0.5, 0.9)(usd_q)[2] AS usd_p90
FROM alphractal.fee_estimates_1d
GROUP BY bucket, operation, speed;

-- Saúde da ingestão: última linha por componente
CREATE OR REPLACE VIEW alphractal.v_ingestion_status AS
SELECT
    component,
    argMax(status, observed_at)     AS status,
    argMax(lag_ms, observed_at)     AS lag_ms,
    argMax(last_block, observed_at) AS last_block,
    argMax(detail, observed_at)     AS detail,
    max(observed_at)                AS last_seen_at
FROM alphractal.ingestion_health
GROUP BY component;
