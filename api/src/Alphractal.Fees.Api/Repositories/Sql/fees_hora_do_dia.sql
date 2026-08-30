-- Media da base fee por hora do dia (UTC), 30 dias.
SELECT hora_utc, amostras, base_fee_gwei_avg, base_fee_gwei_p50,
       base_fee_gwei_min, base_fee_gwei_max
FROM v_fees_hora_do_dia
ORDER BY hora_utc
