CREATE OR REPLACE VIEW alphractal.v_latest_block
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    block_number,
    block_timestamp,
    base_fee_per_gas / 1e9 AS base_fee_gwei,
    next_base_fee / 1e9 AS next_base_fee_gwei,
    priority_fee_p50 / 1e9 AS priority_fee_gwei,
    gas_used,
    gas_limit,
    gas_used / greatest(gas_limit, 1) AS gas_used_ratio,
    tx_count,
    toFloat64(burned_wei) / 1e18 AS burned_eth,
    eth_usd,
    dateDiff('millisecond', block_timestamp, now64(3, 'UTC')) AS age_ms
FROM alphractal.eth_blocks FINAL
ORDER BY block_number DESC
LIMIT 1;

CREATE OR REPLACE VIEW alphractal.v_mempool_now
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    sampled_at,
    block_number,
    pending_tx_count,
    base_fee_per_gas / 1e9 AS base_fee_gwei,
    suggested_priority_slow / 1e9 AS priority_slow_gwei,
    suggested_priority_standard / 1e9 AS priority_standard_gwei,
    suggested_priority_fast / 1e9 AS priority_fast_gwei,
    eth_usd
FROM alphractal.mempool_samples FINAL
ORDER BY sampled_at DESC
LIMIT 1;

CREATE OR REPLACE VIEW alphractal.v_fee_estimates_now
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    operation,
    speed,
    argMax(gas_units, sampled_at) AS gas_units,
    argMax(total_fee_gwei, sampled_at) AS total_fee_gwei,
    argMax(total_fee_usd, sampled_at) AS total_fee_usd,
    max(sampled_at) AS last_sampled_at
FROM alphractal.fee_estimates FINAL
WHERE sampled_at > now64(3, 'UTC') - INTERVAL 10 MINUTE
GROUP BY operation, speed;

CREATE OR REPLACE VIEW alphractal.v_eth_fees_1h
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    bucket,
    blocks,
    base_fee_avg / 1e9 AS base_fee_gwei_avg,
    base_fee_min / 1e9 AS base_fee_gwei_min,
    base_fee_max / 1e9 AS base_fee_gwei_max,
    base_fee_p50 / 1e9 AS base_fee_gwei_p50,
    base_fee_p90 / 1e9 AS base_fee_gwei_p90,
    base_fee_p95 / 1e9 AS base_fee_gwei_p95,
    priority_fee_avg / 1e9 AS priority_fee_gwei_avg,
    gas_used_ratio_avg,
    tx_count,
    toFloat64(burned_wei) / 1e18 AS burned_eth,
    eth_usd_avg
FROM alphractal.eth_fees_rollup FINAL
WHERE granularity = 'hour';

CREATE OR REPLACE VIEW alphractal.v_eth_fees_1d
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    toDate(bucket) AS bucket,
    blocks,
    base_fee_avg / 1e9 AS base_fee_gwei_avg,
    base_fee_min / 1e9 AS base_fee_gwei_min,
    base_fee_max / 1e9 AS base_fee_gwei_max,
    base_fee_p50 / 1e9 AS base_fee_gwei_p50,
    base_fee_p90 / 1e9 AS base_fee_gwei_p90,
    base_fee_p95 / 1e9 AS base_fee_gwei_p95,
    priority_fee_avg / 1e9 AS priority_fee_gwei_avg,
    gas_used_ratio_avg,
    tx_count,
    toFloat64(burned_wei) / 1e18 AS burned_eth,
    eth_usd_avg
FROM alphractal.eth_fees_rollup FINAL
WHERE granularity = 'day';

CREATE OR REPLACE VIEW alphractal.v_fee_estimates_1d
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT bucket, operation, speed, samples, usd_avg, usd_min, usd_max, usd_p50, usd_p90
FROM alphractal.fee_estimates_1d FINAL;

CREATE OR REPLACE VIEW alphractal.v_ingestion_status
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    component,
    argMax(status, observed_at) AS status,
    argMax(lag_ms, observed_at) AS lag_ms,
    argMax(last_block, observed_at) AS last_block,
    argMax(detail, observed_at) AS detail,
    max(observed_at) AS last_seen_at
FROM alphractal.ingestion_health
GROUP BY component;
