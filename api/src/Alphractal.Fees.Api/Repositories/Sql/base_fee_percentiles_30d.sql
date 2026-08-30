-- D-02: limiares da distribuicao de base fee nos ultimos 30 dias.
-- A comparacao com o valor atual acontece em Services/ (RN-09) — view nao aceita
-- parametro e o valor atual vive na memoria da API, nao no banco.
SELECT
    buckets,
    from_bucket,
    to_bucket,
    p05_gwei,
    p10_gwei,
    p25_gwei,
    p50_gwei,
    p75_gwei,
    p90_gwei,
    p95_gwei,
    min_gwei,
    max_gwei
FROM v_base_fee_percentiles_30d
