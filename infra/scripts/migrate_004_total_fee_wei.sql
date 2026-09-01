-- Migracao 004 — taxa efetivamente paga por bloco (total_fee_wei).
--
-- POR QUE: o painel calculava o total de taxas como
--     burned_eth x (base_fee_avg + priority_fee_p50) / base_fee_avg
-- ou seja, tratava a MEDIANA da gorjeta como se fosse a gorjeta media. As
-- transacoes caras (contratos, MEV, liquidacoes) ficam muito acima da mediana e
-- consomem muito gas, entao a conta subestimava o total pela metade: em 27/08
-- deu 87,70 ETH contra 181,45 ETH.
--
-- O alvo esta confirmado contra fonte independente: a metrica FeeTotNtv da Coin
-- Metrics para 2026-08-27 e 181,446106 ETH, o mesmo numero da plataforma de
-- referencia, e a serie inteira da semana coincide. FeeTotNtv e a soma de
-- gasUsed x effectiveGasPrice de todas as transacoes — base fee queimada MAIS
-- gorjeta. Nao ha fator de correcao a aplicar sobre a mediana: o que falta e o
-- valor por transacao, e ele so vem do recibo.
--
-- Os .sql de clickhouse/initdb/ rodam UMA UNICA VEZ, no primeiro start com o
-- volume vazio. Numa instancia que ja existe, so esta migracao aplica a mudanca.
--
-- Aplicar (PowerShell — o '<' e operador reservado, por isso o cmd /c):
--   cmd /c "docker compose exec -T clickhouse clickhouse-client --user alphractal --password alphractal_dev --multiquery < scripts\migrate_004_total_fee_wei.sql"
--
-- Idempotente (IF NOT EXISTS / OR REPLACE). Conteudo identico ao de
-- initdb/002_tables.sql, 003_rollups.sql e 004_views.sql.
--
-- DEPOIS DE APLICAR: as linhas antigas ficam com total_fee_wei = 0. Refaca o
-- backfill do periodo que o painel exibe para preencher a coluna; enquanto ela
-- for zero, o front cai na estimativa antiga em vez de exibir zero.

ALTER TABLE alphractal.eth_blocks
    ADD COLUMN IF NOT EXISTS total_fee_wei UInt128 DEFAULT 0 AFTER burned_wei;

ALTER TABLE alphractal.eth_fees_rollup
    ADD COLUMN IF NOT EXISTS total_fee_wei UInt128 DEFAULT 0 AFTER burned_wei;

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
    toFloat64(total_fee_wei) / 1e18 AS total_fee_eth,
    eth_usd,
    dateDiff('millisecond', block_timestamp, now64(3, 'UTC')) AS age_ms
FROM alphractal.eth_blocks FINAL
ORDER BY block_number DESC
LIMIT 1;

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
    toFloat64(total_fee_wei) / 1e18 AS total_fee_eth,
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
    toFloat64(total_fee_wei) / 1e18 AS total_fee_eth,
    eth_usd_avg
FROM alphractal.eth_fees_rollup FINAL
WHERE granularity = 'day';
