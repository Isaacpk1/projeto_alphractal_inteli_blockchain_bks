-- Migracao 002 — view do D-02 (percentil historico de 30 dias).
--
-- POR QUE ESTE ARQUIVO EXISTE
-- Os .sql de clickhouse/initdb/ rodam UMA UNICA VEZ, no primeiro start com o
-- volume vazio. Adicionar uma view la nao a cria numa instancia que ja existe.
-- Sem esta migracao, o codigo novo consultaria uma view inexistente e o painel
-- responderia 503 sem que ninguem entendesse por que.
--
-- Aplicar:
--   docker compose exec -T clickhouse clickhouse-client \
--     --user alphractal --password alphractal_dev --multiquery < scripts/migrate_002_percentis_30d.sql
--
-- PowerShell (o '<' e operador reservado):
--   cmd /c "docker compose exec -T clickhouse clickhouse-client --user alphractal --password alphractal_dev --multiquery < scripts\migrate_002_percentis_30d.sql"
--
-- Idempotente: CREATE OR REPLACE e GRANT podem rodar quantas vezes for preciso.
-- O conteudo e identico ao de initdb/004_views.sql e 005_users.sql — se um mudar,
-- o outro muda junto.

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

GRANT SELECT ON alphractal.v_base_fee_percentiles_30d TO alphractal_api;
