-- Custo diario por operacao e velocidade (D-04).
-- Fim INCLUSIVO pelo mesmo motivo de fees_history_daily.sql.
SELECT
    bucket,
    operation,
    speed,
    samples,
    usd_avg,
    usd_min,
    usd_max,
    usd_p50,
    usd_p90
FROM v_fee_estimates_1d
WHERE bucket >= toDate({from:DateTime('UTC')})
  AND bucket <= toDate({to:DateTime('UTC')})
ORDER BY bucket, operation, speed
LIMIT {limit:UInt32}
