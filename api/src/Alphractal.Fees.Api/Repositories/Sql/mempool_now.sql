-- Amostra de mempool mais recente registrada pelo ETL.
SELECT
    sampled_at,
    block_number,
    pending_tx_count,
    base_fee_gwei,
    priority_slow_gwei,
    priority_standard_gwei,
    priority_fast_gwei,
    eth_usd
FROM v_mempool_now
