-- 002_tables.sql
-- Tabelas base (caminho quente). Tudo em wei/UInt para não perder precisão;
-- a conversão para gwei/USD é feita na leitura (views) ou na API.

-- ---------------------------------------------------------------------------
-- 1) Bloco a bloco: a fonte da verdade do painel
--    ReplacingMergeTree por block_number resolve reorg e reprocessamento do ETL:
--    reinserir o mesmo bloco sobrescreve em vez de duplicar.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alphractal.eth_blocks
(
    block_number        UInt64,
    block_hash          String,
    block_timestamp     DateTime64(3, 'UTC'),
    ingested_at         DateTime64(3, 'UTC') DEFAULT now64(3, 'UTC'),

    base_fee_per_gas    UInt64   COMMENT 'wei, EIP-1559',
    next_base_fee       UInt64   COMMENT 'wei, projeção do próximo bloco',
    gas_used            UInt64,
    gas_limit           UInt64,
    tx_count            UInt32,

    priority_fee_p10    UInt64   COMMENT 'wei, tip observado no bloco',
    priority_fee_p50    UInt64,
    priority_fee_p90    UInt64,

    burned_wei          UInt128  COMMENT 'base_fee_per_gas * gas_used',
    eth_usd             Decimal(18, 6) COMMENT 'preço no instante do bloco'
)
ENGINE = ReplacingMergeTree(ingested_at)
PARTITION BY toYYYYMM(block_timestamp)
ORDER BY block_number
TTL toDateTime(block_timestamp) + INTERVAL 180 DAY;

-- ---------------------------------------------------------------------------
-- 2) Amostras de mempool (sub-bloco, cadência de ~1-2 s)
--    Volume alto e valor efêmero: TTL curto, particionado por dia.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alphractal.mempool_samples
(
    sampled_at                  DateTime64(3, 'UTC'),
    block_number                UInt64 COMMENT 'último bloco visto na amostra',
    pending_tx_count            UInt32,
    base_fee_per_gas            UInt64,
    suggested_priority_slow     UInt64,
    suggested_priority_standard UInt64,
    suggested_priority_fast     UInt64,
    eth_usd                     Decimal(18, 6)
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(sampled_at)
ORDER BY sampled_at
TTL toDateTime(sampled_at) + INTERVAL 7 DAY;

-- ---------------------------------------------------------------------------
-- 3) Estimativa financeira por tipo de operação
--    É isto que o painel mostra: "quanto custa, em USD, fazer X agora".
--    operation: transfer | erc20_transfer | uniswap_v3_swap | nft_mint | ...
--    speed:     slow | standard | fast
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alphractal.fee_estimates
(
    sampled_at      DateTime64(3, 'UTC'),
    block_number    UInt64,
    operation       LowCardinality(String),
    speed           LowCardinality(String),
    gas_units       UInt32          COMMENT 'gas estimado da operação',
    total_fee_wei   UInt128,
    total_fee_gwei  Float64,
    total_fee_usd   Decimal(18, 6)
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(sampled_at)
ORDER BY (operation, speed, sampled_at)
TTL toDateTime(sampled_at) + INTERVAL 30 DAY;

-- ---------------------------------------------------------------------------
-- 4) Preço ETH/USD (feed separado, atualizado pelo ETL em Python)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alphractal.eth_usd_prices
(
    observed_at DateTime64(3, 'UTC'),
    source      LowCardinality(String),
    price_usd   Decimal(18, 6)
)
ENGINE = ReplacingMergeTree
PARTITION BY toYYYYMM(observed_at)
ORDER BY (source, observed_at)
TTL toDateTime(observed_at) + INTERVAL 90 DAY;

-- ---------------------------------------------------------------------------
-- 5) Telemetria da própria ingestão (RNF de disponibilidade)
--    Serve para o painel dizer "dados atrasados há N segundos".
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alphractal.ingestion_health
(
    observed_at    DateTime64(3, 'UTC') DEFAULT now64(3, 'UTC'),
    component      LowCardinality(String) COMMENT 'ws_listener | etl | api',
    status         LowCardinality(String) COMMENT 'ok | degraded | down',
    lag_ms         UInt32,
    last_block     UInt64,
    detail         String DEFAULT ''
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(observed_at)
ORDER BY (component, observed_at)
TTL toDateTime(observed_at) + INTERVAL 14 DAY;
