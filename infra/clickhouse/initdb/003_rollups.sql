-- 003_rollups.sql
-- Caminho frio: agregados horários e diários ("nível diário" pedido pelo parceiro).
-- Estados de agregação (AggregatingMergeTree) em vez de números fechados, para
-- que 24 buckets horários possam ser re-agregados em 1 diário sem erro estatístico.
--
-- ATENÇÃO (reorg): a MV dispara no INSERT. Se o ETL reinserir um bloco corrigido,
-- a linha antiga já contribuiu para o rollup. No MVP isto é ruído desprezível;
-- em produção, recompute o dia anterior com INSERT ... SELECT ... FROM eth_blocks FINAL.

-- --------------------------------- 1 hora ---------------------------------
CREATE TABLE IF NOT EXISTS alphractal.eth_fees_1h
(
    bucket             DateTime('UTC'),
    blocks             AggregateFunction(count),
    base_fee_avg       AggregateFunction(avg, UInt64),
    base_fee_min       AggregateFunction(min, UInt64),
    base_fee_max       AggregateFunction(max, UInt64),
    base_fee_q         AggregateFunction(quantiles(0.5, 0.9, 0.95), UInt64),
    priority_fee_avg   AggregateFunction(avg, UInt64),
    gas_used_ratio_avg AggregateFunction(avg, Float64),
    tx_count_sum       AggregateFunction(sum, UInt32),
    burned_sum         AggregateFunction(sum, UInt128),
    eth_usd_avg        AggregateFunction(avg, Decimal(18, 6))
)
ENGINE = AggregatingMergeTree
PARTITION BY toYYYYMM(bucket)
ORDER BY bucket;

CREATE MATERIALIZED VIEW IF NOT EXISTS alphractal.eth_fees_1h_mv
TO alphractal.eth_fees_1h AS
SELECT
    toStartOfHour(block_timestamp)                       AS bucket,
    countState()                                         AS blocks,
    avgState(base_fee_per_gas)                           AS base_fee_avg,
    minState(base_fee_per_gas)                           AS base_fee_min,
    maxState(base_fee_per_gas)                           AS base_fee_max,
    quantilesState(0.5, 0.9, 0.95)(base_fee_per_gas)     AS base_fee_q,
    avgState(priority_fee_p50)                           AS priority_fee_avg,
    avgState(gas_used / greatest(gas_limit, 1))            AS gas_used_ratio_avg,
    sumState(tx_count)                                   AS tx_count_sum,
    sumState(burned_wei)                                 AS burned_sum,
    avgState(eth_usd)                                    AS eth_usd_avg
FROM alphractal.eth_blocks
GROUP BY bucket;

-- ---------------------------------- 1 dia ----------------------------------
CREATE TABLE IF NOT EXISTS alphractal.eth_fees_1d
(
    bucket             Date,
    blocks             AggregateFunction(count),
    base_fee_avg       AggregateFunction(avg, UInt64),
    base_fee_min       AggregateFunction(min, UInt64),
    base_fee_max       AggregateFunction(max, UInt64),
    base_fee_q         AggregateFunction(quantiles(0.5, 0.9, 0.95), UInt64),
    priority_fee_avg   AggregateFunction(avg, UInt64),
    gas_used_ratio_avg AggregateFunction(avg, Float64),
    tx_count_sum       AggregateFunction(sum, UInt32),
    burned_sum         AggregateFunction(sum, UInt128),
    eth_usd_avg        AggregateFunction(avg, Decimal(18, 6))
)
ENGINE = AggregatingMergeTree
PARTITION BY toYYYYMM(bucket)
ORDER BY bucket;

CREATE MATERIALIZED VIEW IF NOT EXISTS alphractal.eth_fees_1d_mv
TO alphractal.eth_fees_1d AS
SELECT
    toDate(block_timestamp)                              AS bucket,
    countState()                                         AS blocks,
    avgState(base_fee_per_gas)                           AS base_fee_avg,
    minState(base_fee_per_gas)                           AS base_fee_min,
    maxState(base_fee_per_gas)                           AS base_fee_max,
    quantilesState(0.5, 0.9, 0.95)(base_fee_per_gas)     AS base_fee_q,
    avgState(priority_fee_p50)                           AS priority_fee_avg,
    avgState(gas_used / greatest(gas_limit, 1))            AS gas_used_ratio_avg,
    sumState(tx_count)                                   AS tx_count_sum,
    sumState(burned_wei)                                 AS burned_sum,
    avgState(eth_usd)                                    AS eth_usd_avg
FROM alphractal.eth_blocks
GROUP BY bucket;

-- --------------- custo diário por operação (para o heatmap D-04) ---------------
CREATE TABLE IF NOT EXISTS alphractal.fee_estimates_1d
(
    bucket      Date,
    operation   LowCardinality(String),
    speed       LowCardinality(String),
    usd_avg     AggregateFunction(avg, Decimal(18, 6)),
    usd_min     AggregateFunction(min, Decimal(18, 6)),
    usd_max     AggregateFunction(max, Decimal(18, 6)),
    usd_q       AggregateFunction(quantiles(0.5, 0.9), Float64),
    samples     AggregateFunction(count)
)
ENGINE = AggregatingMergeTree
PARTITION BY toYYYYMM(bucket)
ORDER BY (operation, speed, bucket);

CREATE MATERIALIZED VIEW IF NOT EXISTS alphractal.fee_estimates_1d_mv
TO alphractal.fee_estimates_1d AS
SELECT
    toDate(sampled_at)                          AS bucket,
    operation,
    speed,
    avgState(total_fee_usd)                     AS usd_avg,
    minState(total_fee_usd)                     AS usd_min,
    maxState(total_fee_usd)                     AS usd_max,
    quantilesState(0.5, 0.9)(toFloat64(total_fee_usd)) AS usd_q,
    countState()                                AS samples
FROM alphractal.fee_estimates
GROUP BY bucket, operation, speed;
