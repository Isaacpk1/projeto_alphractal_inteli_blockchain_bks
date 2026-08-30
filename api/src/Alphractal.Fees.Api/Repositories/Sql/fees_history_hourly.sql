-- Serie horaria do rollup. Parametro do lado do servidor: nunca concatenar.
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
    eth_usd_avg
FROM v_eth_fees_1h
WHERE bucket >= {from:DateTime('UTC')}
  AND bucket <  {to:DateTime('UTC')}
ORDER BY bucket
LIMIT {limit:UInt32}
