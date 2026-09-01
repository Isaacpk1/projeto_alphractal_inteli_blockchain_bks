-- Serie diaria do rollup. O bucket da view e Date; toDate() no parametro evita
-- depender de como o driver formata DateOnly.
-- Fim INCLUSIVO: com '<' o bucket do dia corrente nunca aparece, porque
-- toDate(agora) e o proprio dia de hoje. O horario usa '<' porque o bucket
-- da hora corrente e menor que agora.
SELECT
    bucket,
    blocks,
    base_fee_gwei_avg,
    base_fee_gwei_min,
    base_fee_gwei_max,
    base_fee_gwei_p50,
    base_fee_gwei_p90,
    base_fee_gwei_p95,
    priority_fee_gwei_avg,
    gas_used_ratio_avg,
    tx_count,
    burned_eth,
    total_fee_eth,
    eth_usd_avg
FROM v_eth_fees_1d
WHERE bucket >= toDate({from:DateTime('UTC')})
  AND bucket <= toDate({to:DateTime('UTC')})
ORDER BY bucket
LIMIT {limit:UInt32}
