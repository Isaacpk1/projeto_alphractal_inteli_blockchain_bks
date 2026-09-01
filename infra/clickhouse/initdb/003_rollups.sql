-- Rollups recalculados pelo ETL por bucket.
-- ReplacingMergeTree torna a recomputacao de reorg/reprocessamento idempotente.

CREATE TABLE IF NOT EXISTS alphractal.eth_fees_rollup
(
    granularity       LowCardinality(String),
    bucket            DateTime('UTC'),
    calculated_at     DateTime64(3, 'UTC'),
    blocks            UInt64,
    base_fee_avg      Float64,
    base_fee_min      UInt64,
    base_fee_max      UInt64,
    base_fee_p50      UInt64,
    base_fee_p90      UInt64,
    base_fee_p95      UInt64,
    priority_fee_avg  Float64,
    gas_used_ratio_avg Float64,
    tx_count          UInt64,
    burned_wei        UInt128,
    -- Soma do total efetivamente pago no bucket (base fee + gorjeta).
    total_fee_wei     UInt128 DEFAULT 0,
    eth_usd_avg       Decimal(18, 6)
)
ENGINE = ReplacingMergeTree(calculated_at)
PARTITION BY toYYYYMM(bucket)
ORDER BY (granularity, bucket);

CREATE TABLE IF NOT EXISTS alphractal.fee_estimates_1d
(
    bucket        Date,
    operation     LowCardinality(String),
    speed         LowCardinality(String),
    calculated_at DateTime64(3, 'UTC'),
    samples       UInt64,
    usd_avg       Decimal(18, 6),
    usd_min       Decimal(18, 6),
    usd_max       Decimal(18, 6),
    usd_p50       Decimal(18, 6),
    usd_p90       Decimal(18, 6)
)
ENGINE = ReplacingMergeTree(calculated_at)
PARTITION BY toYYYYMM(bucket)
ORDER BY (operation, speed, bucket);
