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

-- D-02 — distribuicao historica da base fee (30 dias).
--
-- Responde "esta caro em termos historicos?", que a RN-04 NAO responde: aquela
-- regra compara com uma media movel curta e portanto mede VARIACAO, nao NIVEL.
-- Num periodo sustentado de taxas altas a media acompanha a subida e o indicador
-- volta a marcar "Normal". As duas metricas sao complementares.
--
-- Le do rollup horario, nao de eth_blocks: 720 linhas contra ~216 mil, mesma
-- resposta. A janela de retencao dos blocos brutos e de 30 dias (RN-15), entao
-- consultar a tabela bruta daria o mesmo alcance por muito mais trabalho.
--
-- Devolve limiares, nao um percentil calculado: view nao aceita parametro, e o
-- valor atual vive na memoria da API. A comparacao acontece em Services/ (RN-09).
CREATE OR REPLACE VIEW alphractal.v_base_fee_percentiles_30d
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    count()                                        AS buckets,
    min(bucket)                                    AS from_bucket,
    max(bucket)                                    AS to_bucket,
    quantileExact(0.05)(base_fee_avg) / 1e9        AS p05_gwei,
    quantileExact(0.10)(base_fee_avg) / 1e9        AS p10_gwei,
    quantileExact(0.25)(base_fee_avg) / 1e9        AS p25_gwei,
    quantileExact(0.50)(base_fee_avg) / 1e9        AS p50_gwei,
    quantileExact(0.75)(base_fee_avg) / 1e9        AS p75_gwei,
    quantileExact(0.90)(base_fee_avg) / 1e9        AS p90_gwei,
    quantileExact(0.95)(base_fee_avg) / 1e9        AS p95_gwei,
    min(base_fee_avg) / 1e9                        AS min_gwei,
    max(base_fee_avg) / 1e9                        AS max_gwei
FROM alphractal.eth_fees_rollup FINAL
WHERE granularity = 'hour'
  AND bucket >= now() - INTERVAL 30 DAY;

-- Melhor hora para transacionar — media por hora do dia (UTC), 30 dias.
--
-- Responde "QUANDO devo executar?", que nenhuma outra metrica do sistema
-- responde: o congestionamento diz se esta subindo, o percentil diz se esta caro
-- em termos historicos, e nenhum dos dois diz se vale esperar ate as 3h.
--
-- Le do rollup horario: cada linha ja e a media de ~300 blocos, entao 30 dias
-- sao 720 linhas em vez de 216 mil.
CREATE OR REPLACE VIEW alphractal.v_fees_hora_do_dia
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    toHour(bucket)                              AS hora_utc,
    count()                                     AS amostras,
    avg(base_fee_avg) / 1e9                     AS base_fee_gwei_avg,
    quantileExact(0.50)(base_fee_avg) / 1e9     AS base_fee_gwei_p50,
    min(base_fee_avg) / 1e9                     AS base_fee_gwei_min,
    max(base_fee_avg) / 1e9                     AS base_fee_gwei_max
FROM alphractal.eth_fees_rollup FINAL
WHERE granularity = 'hour'
  AND bucket >= now() - INTERVAL 30 DAY
GROUP BY hora_utc
ORDER BY hora_utc;

-- Grade dia-da-semana x hora (7 x 24 = 168 celulas). Alimenta o heatmap.
-- toDayOfWeek: 1 = segunda ... 7 = domingo.
CREATE OR REPLACE VIEW alphractal.v_fees_semana_hora
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    toDayOfWeek(bucket)                         AS dia_semana,
    toHour(bucket)                              AS hora_utc,
    count()                                     AS amostras,
    avg(base_fee_avg) / 1e9                     AS base_fee_gwei_avg
FROM alphractal.eth_fees_rollup FINAL
WHERE granularity = 'hour'
  AND bucket >= now() - INTERVAL 30 DAY
GROUP BY dia_semana, hora_utc
ORDER BY dia_semana, hora_utc;

-- Variacao do ETH/USD em 24 h.
--
-- argMaxIf pega a cotacao mais recente ANTERIOR ao corte de 24 h — nao a mais
-- proxima em valor absoluto. A serie e amostrada a cada ~60 s, entao a cotacao
-- encontrada esta a segundos do corte, nao a horas.
-- A janela de 48 h limita a varredura: sem ela, a consulta leria a retencao
-- inteira de 90 dias para achar duas linhas.
CREATE OR REPLACE VIEW alphractal.v_eth_usd_24h
DEFINER = CURRENT_USER SQL SECURITY DEFINER AS
SELECT
    argMax(price_usd, observed_at)              AS preco_atual,
    max(observed_at)                            AS observado_em,
    argMaxIf(price_usd, observed_at, observed_at <= now64(3, 'UTC') - INTERVAL 24 HOUR)
                                                AS preco_24h,
    maxIf(observed_at, observed_at <= now64(3, 'UTC') - INTERVAL 24 HOUR)
                                                AS observado_em_24h,
    countIf(observed_at <= now64(3, 'UTC') - INTERVAL 24 HOUR) AS amostras_24h
FROM alphractal.eth_usd_prices FINAL
WHERE observed_at >= now64(3, 'UTC') - INTERVAL 48 HOUR;
