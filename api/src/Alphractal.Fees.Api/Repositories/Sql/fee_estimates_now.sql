-- Ultima estimativa por operacao e velocidade (janela de 10 min na view).
SELECT
    operation,
    speed,
    gas_units,
    total_fee_gwei,
    total_fee_usd,
    last_sampled_at
FROM v_fee_estimates_now
ORDER BY operation, speed
