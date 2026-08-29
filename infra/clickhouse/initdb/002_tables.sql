-- Tabelas de entrada do ETL. Todas aceitam entrega at-least-once.

CREATE TABLE IF NOT EXISTS alphractal.eth_blocks
(
    block_number        UInt64,
    block_hash          String,
    block_timestamp     DateTime64(3, 'UTC'),
    ingested_at         DateTime64(3, 'UTC') DEFAULT now64(3, 'UTC'),
    base_fee_per_gas    UInt64,
    next_base_fee       UInt64,
    gas_used            UInt64,
    gas_limit           UInt64,
    tx_count            UInt32,
    priority_fee_p10    UInt64,
    priority_fee_p50    UInt64,
    priority_fee_p90    UInt64,
    burned_wei          UInt128,
    eth_usd             Decimal(18, 6)
)
ENGINE = ReplacingMergeTree(ingested_at)
PARTITION BY toYYYYMM(block_timestamp)
ORDER BY block_number
TTL toDateTime(block_timestamp) + INTERVAL 30 DAY;

CREATE TABLE IF NOT EXISTS alphractal.mempool_samples
(
    sampled_at                  DateTime64(3, 'UTC'),
    block_number                UInt64,
    ingested_at                 DateTime64(3, 'UTC') DEFAULT now64(3, 'UTC'),
    pending_tx_count            UInt32,
    base_fee_per_gas            UInt64,
    suggested_priority_slow     UInt64,
    suggested_priority_standard UInt64,
    suggested_priority_fast     UInt64,
    eth_usd                     Decimal(18, 6)
)
ENGINE = ReplacingMergeTree(ingested_at)
PARTITION BY toYYYYMMDD(sampled_at)
ORDER BY (sampled_at, block_number)
TTL toDateTime(sampled_at) + INTERVAL 7 DAY;

CREATE TABLE IF NOT EXISTS alphractal.fee_estimates
(
    sampled_at      DateTime64(3, 'UTC'),
    block_number    UInt64,
    operation       LowCardinality(String),
    speed           LowCardinality(String),
    ingested_at     DateTime64(3, 'UTC') DEFAULT now64(3, 'UTC'),
    gas_units       UInt32,
    total_fee_wei   UInt128,
    total_fee_gwei  Decimal(24, 9),
    total_fee_usd   Decimal(18, 6)
)
ENGINE = ReplacingMergeTree(ingested_at)
PARTITION BY toYYYYMMDD(sampled_at)
ORDER BY (sampled_at, block_number, operation, speed)
TTL toDateTime(sampled_at) + INTERVAL 30 DAY;

CREATE TABLE IF NOT EXISTS alphractal.eth_usd_prices
(
    observed_at DateTime64(3, 'UTC'),
    source      LowCardinality(String),
    ingested_at DateTime64(3, 'UTC') DEFAULT now64(3, 'UTC'),
    price_usd   Decimal(18, 6)
)
ENGINE = ReplacingMergeTree(ingested_at)
PARTITION BY toYYYYMM(observed_at)
ORDER BY (source, observed_at)
TTL toDateTime(observed_at) + INTERVAL 90 DAY;

CREATE TABLE IF NOT EXISTS alphractal.ingestion_health
(
    observed_at DateTime64(3, 'UTC') DEFAULT now64(3, 'UTC'),
    component   LowCardinality(String),
    status      LowCardinality(String),
    lag_ms      UInt32,
    last_block  UInt64,
    detail      String DEFAULT ''
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(observed_at)
ORDER BY (component, observed_at)
TTL toDateTime(observed_at) + INTERVAL 14 DAY;
