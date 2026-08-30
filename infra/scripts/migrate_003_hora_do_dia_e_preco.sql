-- Migracao 003 — melhor hora para transacionar e variacao 24h do ETH.
--
-- Os .sql de clickhouse/initdb/ rodam UMA UNICA VEZ, no primeiro start com o
-- volume vazio. Numa instancia que ja existe, so esta migracao cria as views.
--
-- Aplicar (PowerShell — o '<' e operador reservado, por isso o cmd /c):
--   cmd /c "docker compose exec -T clickhouse clickhouse-client --user alphractal --password alphractal_dev --multiquery < scripts\migrate_003_hora_do_dia_e_preco.sql"
--
-- Idempotente. Conteudo identico ao de initdb/004_views.sql e 005_users.sql.

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

GRANT SELECT ON alphractal.v_fees_hora_do_dia TO alphractal_api;
GRANT SELECT ON alphractal.v_fees_semana_hora TO alphractal_api;
GRANT SELECT ON alphractal.v_eth_usd_24h TO alphractal_api;
