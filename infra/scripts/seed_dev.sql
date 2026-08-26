-- seed_dev.sql
-- Gera ~48 h de dados sintéticos para destravar API e frontend ANTES de a chave
-- RPC do parceiro chegar (dúvida 1 do registro de 18/08/2026).
-- NÃO é dado real: serve para validar contratos, gráficos e custo de query.
--
--   docker compose exec -T clickhouse clickhouse-client \
--     --user alphractal --password alphractal_dev --multiquery < scripts/seed_dev.sql

TRUNCATE TABLE IF EXISTS alphractal.eth_blocks;
TRUNCATE TABLE IF EXISTS alphractal.mempool_samples;
TRUNCATE TABLE IF EXISTS alphractal.fee_estimates;
TRUNCATE TABLE IF EXISTS alphractal.eth_fees_1h;
TRUNCATE TABLE IF EXISTS alphractal.eth_fees_1d;
TRUNCATE TABLE IF EXISTS alphractal.fee_estimates_1d;
TRUNCATE TABLE IF EXISTS alphractal.ingestion_health;

-- ---------------------------------------------------------------------------
-- 14400 blocos = 48 h a 12 s/bloco. Base fee com ciclo diário + ruído.
-- ---------------------------------------------------------------------------
INSERT INTO alphractal.eth_blocks
    (block_number, block_hash, block_timestamp, base_fee_per_gas, next_base_fee,
     gas_used, gas_limit, tx_count, priority_fee_p10, priority_fee_p50,
     priority_fee_p90, burned_wei, eth_usd)
SELECT
    block_number,
    block_hash,
    block_timestamp,
    base_fee,
    toUInt64(base_fee * (0.95 + randCanonical() * 0.10)),
    gas_used,
    gas_limit,
    tx_count,
    p10,
    p50,
    p90,
    toUInt128(base_fee) * toUInt128(gas_used),
    eth_usd
FROM
(
    SELECT
        23000000 + number AS block_number,
        concat('0x', lower(hex(sipHash128(number)))) AS block_hash,
        toDateTime64(now('UTC') - ((14400 - number) * 12), 3, 'UTC') AS block_timestamp,
        toUInt64(greatest(1e9, (8 + (6 * sin(number / 600.)) + (randCanonical() * 4)) * 1e9)) AS base_fee,
        toUInt64(36000000) AS gas_limit,
        toUInt64(36000000 * (0.35 + (randCanonical() * 0.60))) AS gas_used,
        toUInt32(120 + (randCanonical() * 180)) AS tx_count,
        toUInt64(0.05e9 + (randCanonical() * 0.10e9)) AS p10,
        toUInt64(0.50e9 + (randCanonical() * 1.00e9)) AS p50,
        toUInt64(2.00e9 + (randCanonical() * 3.00e9)) AS p90,
        toDecimal64(3200 + (250 * sin(number / 900.)) + (randCanonical() * 40), 6) AS eth_usd
    FROM numbers(14400)
);

-- ---------------------------------------------------------------------------
-- Amostras de mempool: 1 a cada 2 s nas últimas 6 h (10800 linhas)
-- ---------------------------------------------------------------------------
INSERT INTO alphractal.mempool_samples
SELECT
    toDateTime64(now('UTC') - ((10800 - number) * 2), 3, 'UTC') AS sampled_at,
    23000000 + 14400 - intDiv(10800 - number, 6) AS block_number,
    toUInt32(90000 + (randCanonical() * 60000)) AS pending_tx_count,
    toUInt64(greatest(1e9, (8 + (6 * sin(number / 100.)) + (randCanonical() * 4)) * 1e9)) AS base_fee_per_gas,
    toUInt64(0.05e9 + (randCanonical() * 0.10e9)) AS suggested_priority_slow,
    toUInt64(0.50e9 + (randCanonical() * 1.00e9)) AS suggested_priority_standard,
    toUInt64(2.00e9 + (randCanonical() * 3.00e9)) AS suggested_priority_fast,
    toDecimal64(3200 + (randCanonical() * 40), 6) AS eth_usd
FROM numbers(10800);

-- ---------------------------------------------------------------------------
-- Estimativas por operação × velocidade, derivadas dos blocos reais da tabela
-- ---------------------------------------------------------------------------
INSERT INTO alphractal.fee_estimates
    (sampled_at, block_number, operation, speed, gas_units,
     total_fee_wei, total_fee_gwei, total_fee_usd)
SELECT
    sampled_at,
    block_number,
    operation,
    speed,
    gas_units,
    fee_wei,
    toFloat64(fee_wei) / 1e9 AS total_fee_gwei,
    toDecimal64((toFloat64(fee_wei) / 1e18) * toFloat64(eth_usd), 6) AS total_fee_usd
FROM
(
    SELECT
        block_timestamp AS sampled_at,
        block_number,
        eth_usd,
        op.1 AS operation,
        op.2 AS gas_units,
        speed,
        toUInt128(base_fee_per_gas + tip) * toUInt128(op.2) AS fee_wei
    FROM
    (
        SELECT
            block_timestamp,
            block_number,
            eth_usd,
            base_fee_per_gas,
            arrayJoin(['slow', 'standard', 'fast']) AS speed,
            multiIf(speed = 'slow', priority_fee_p10,
                    speed = 'standard', priority_fee_p50,
                    priority_fee_p90) AS tip
        FROM alphractal.eth_blocks
        WHERE block_number > 23000000 + 14400 - 1200   -- últimas ~4 h
    )
    ARRAY JOIN
    [
        ('transfer',        toUInt32(21000)),
        ('erc20_transfer',  toUInt32(65000)),
        ('uniswap_v3_swap', toUInt32(184000)),
        ('nft_mint',        toUInt32(150000))
    ] AS op
);

-- ---------------------------------------------------------------------------
-- Heartbeat dos componentes
-- ---------------------------------------------------------------------------
INSERT INTO alphractal.ingestion_health
    (observed_at, component, status, lag_ms, last_block, detail)
VALUES
    (now64(3, 'UTC'), 'ws_listener', 'ok', 850,  23014399, 'seed'),
    (now64(3, 'UTC'), 'etl',         'ok', 1200, 23014399, 'seed'),
    (now64(3, 'UTC'), 'api',         'ok', 40,   23014399, 'seed');
