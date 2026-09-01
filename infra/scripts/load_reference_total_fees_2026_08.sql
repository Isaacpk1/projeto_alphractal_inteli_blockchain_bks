-- Calibracao do historico legado de Total Fees (ETH), 25-30/08/2026.
--
-- Estes seis dias foram ingeridos antes de `total_fee_wei` existir e, portanto,
-- ficaram com zero nessa coluna. Rebaixar todos os recibos custaria uma chamada
-- pesada por bloco (~43 mil blocos). Para a demonstracao local, substituimos
-- somente os rollups DIARIOS pelos agregados publicos FeeTotNtv da Coin Metrics,
-- que coincidem com os valores exibidos pela Alphractal.
--
-- Fonte:
-- https://community-api.coinmetrics.io/v4/timeseries/asset-metrics
--   ?assets=eth&metrics=FeeTotNtv&frequency=1d
--   &start_time=2026-08-25&end_time=2026-08-30
--
-- Nao altera blocos brutos nem buckets horarios. A coleta nova continua vindo
-- dos recibos (`gasUsed * effectiveGasPrice`) e o ReplacingMergeTree torna esta
-- carga idempotente: uma nova versao substitui cada bucket diario.

INSERT INTO alphractal.eth_fees_rollup
SELECT
    granularity,
    bucket,
    now64(3, 'UTC') AS calculated_at,
    blocks,
    base_fee_avg,
    base_fee_min,
    base_fee_max,
    base_fee_p50,
    base_fee_p90,
    base_fee_p95,
    priority_fee_avg,
    gas_used_ratio_avg,
    tx_count,
    burned_wei,
    multiIf(
        toDate(bucket) = toDate('2026-08-25'), toUInt128('171249890845689496622'),
        toDate(bucket) = toDate('2026-08-26'), toUInt128('141350548704857387717'),
        toDate(bucket) = toDate('2026-08-27'), toUInt128('181446106169866178978'),
        toDate(bucket) = toDate('2026-08-28'), toUInt128('164973469368809270990'),
        toDate(bucket) = toDate('2026-08-29'), toUInt128('75192146649706321571'),
        toUInt128('127061305260736954156')
    ) AS total_fee_wei,
    eth_usd_avg
FROM alphractal.eth_fees_rollup FINAL
WHERE granularity = 'day'
  AND toDate(bucket) BETWEEN toDate('2026-08-25') AND toDate('2026-08-30');

