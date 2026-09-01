-- Ultimo bloco conhecido pelo CAMINHO FRIO.
-- Nao e o snapshot ao vivo: o painel recebe o bloco atual da janela em memoria
-- de Services/ (RN-14). Esta consulta e diagnostico e fallback de demonstracao.
SELECT
    block_number,
    block_timestamp,
    base_fee_gwei,
    next_base_fee_gwei,
    priority_fee_gwei,
    gas_used,
    gas_limit,
    gas_used_ratio,
    tx_count,
    burned_eth,
    total_fee_eth,
    eth_usd,
    age_ms
FROM v_latest_block
